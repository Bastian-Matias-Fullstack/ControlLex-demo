using API.Middlewares;
using API.Helpers;
using Aplicacion.Casos;
using Aplicacion.Repositorio;
using Aplicacion.Servicios;
using Aplicacion.Servicios.Auth;
using Aplicacion.Servicios.Casos;
using Aplicacion.Servicios.Operacional;
using Aplicacion.Validaciones;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infraestructura.Persistencia;
using Infraestructura.Repositorios;
using Infraestructura.Servicios;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Aplicacion.Servicios.Demo;
//Configuración de Servicios (DI)
var builder = WebApplication.CreateBuilder(args);
var isRenderWebService =
    string.Equals(
        builder.Configuration["RENDER"],
        "true",
        StringComparison.OrdinalIgnoreCase) &&
    string.Equals(
        builder.Configuration["RENDER_SERVICE_TYPE"],
        "web",
        StringComparison.OrdinalIgnoreCase);

//aqui permitimos 
//var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

//Conexion a la base de datos 
builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
sql => sql.MigrationsAssembly("API")
    ));
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddDbContextCheck<AppDbContext>("db");
builder.Services.AddScoped<ICasoRepository, CasoRepository>();
builder.Services.AddScoped<ListarCasosService>();
builder.Services.AddScoped<ActualizarCasoService>();
builder.Services.AddScoped<FormateadorNombreService>();
builder.Services.AddScoped<CrearCasoService>();
builder.Services.AddScoped<CerrarCasoService>();
builder.Services.AddScoped<EliminarCasoService>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IRolRepositorio, RolRepositorio>();
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<IDemoResetService, DemoResetService>();
builder.Services.AddScoped<IDatabaseWarmup, EfDatabaseWarmup>();
builder.Services.AddScoped<DemoBootstrapService>();
builder.Services.AddSingleton<ILoginLockoutService, LoginLockoutService>();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<Aplicacion.Usuarios.Handlers.CrearUsuarioCommandHandler>());
//🔹 Validaciones (FluentValidation)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<CrearCasoRequestValidator>();
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>)
);
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value is not null && e.Value.Errors.Count > 0)
            .Select(e => new
            {
                campo = e.Key,
                errores = e.Value!.Errors.Select(x =>
                    string.IsNullOrWhiteSpace(x.ErrorMessage)
                        ? "Valor inválido."
                        : x.ErrorMessage
                ).ToList()
            })
            .ToList();
        var problemDetails = ApiError.BadRequest(
            "Uno o más parámetros no cumplen el formato esperado.",
            context.HttpContext);
        problemDetails.Extensions["errors"] = errors;
        return new BadRequestObjectResult(problemDetails);
    };
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations(); //  Esto es clave

    c.SwaggerDoc("v1", new OpenApiInfo
    { 
        Title = "API Jurídica",
        Version = "v1",
        Description = "Documentación oficial de la API"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ejemplo: Bearer {tu_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.UseInlineDefinitionsForEnums(); //Esto activa los enums como dropdown en Swagger
});
// 1) CORS (por configuración)
var corsOrigins = builder.Configuration
    .GetSection("Cors:Origins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin.Trim().TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins)
                  .WithMethods("GET", "HEAD")
                  .AllowAnyHeader();
        }
    });
});
// Autenticación con JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
    "JWT Key no configurada. Configura Jwt:Key (Development) o la variable de entorno Jwt__Key (Production).");
}
    builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await ApiError.WriteAsync(
                    context.HttpContext,
                    ApiError.Unauthorized("Se requiere autenticación válida.", context.HttpContext),
                    context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                await ApiError.WriteAsync(
                    context.HttpContext,
                    ApiError.Forbidden("No tienes permisos para acceder a este recurso.", context.HttpContext),
                    context.HttpContext.RequestAborted);
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});
//   Rate Limiting (estricto pero usable para demo pública)
// - Global API: 20 req/min por IP
// - Login: 3 req/min por IP
// - Writes (POST/PUT/DELETE): 8 req/min por IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        // Retry-After si está disponible
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }
        await ApiError.WriteAsync(
            context.HttpContext,
            ApiError.TooManyRequests(
                "Demasiadas solicitudes. Intenta nuevamente en unos segundos.",
                context.HttpContext),
            token);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Effective client IP is resolved before the rate limiter middleware.
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = httpContext.Request.Method.ToUpperInvariant();

        // 1) LOGIN (ultra estricto)
        if (path == "/api/auth/login")
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"login:{ip}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }
        // 1.5) WARMUP (estricto y barato)
        // 1.5) SYSTEM (ping + warmup) - estricto y barato
        if (path == "/api/system/warmup" || path == "/api/system/ping")
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"system:{path}:{ip}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }
        // 2) WRITES (POST/PUT/DELETE) - estricto
        if (method is "POST" or "PUT" or "DELETE" or "PATCH")
        {
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"write:{ip}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        }
        // 3) GLOBAL API (GET/listados y navegación)
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"global:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});
// HSTS (solo tiene efecto cuando se llama app.UseHsts() en Production)
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;

    // 👇 Para poder VER el header en localhost cuando simulas Production local:
    // en el dominio real también funcionará.
    options.ExcludedHosts.Clear();
});
var app = builder.Build();

var seedDemoRequested = System.Array.Exists(
    args,
    arg => string.Equals(
        arg,
        "--seed-demo",
        System.StringComparison.OrdinalIgnoreCase));

if (seedDemoRequested)
{
    if (!app.Environment.IsDevelopment())
    {
        throw new System.InvalidOperationException(
            "El seed demo solo está permitido en Development.");
    }

    using var scope = app.Services.CreateScope();

    var demoBootstrap =
        scope.ServiceProvider.GetRequiredService<DemoBootstrapService>();

    var demoPassword =
        app.Configuration["DemoBootstrap:Password"];

    await demoBootstrap.BootstrapAsync(
        demoPassword ?? string.Empty);

    System.Console.WriteLine("DEMO_BOOTSTRAP_COMPLETED");
    return;
}

app.Use(async (context, next) =>
{
    context.Connection.RemoteIpAddress =
        ClientIpResolver.Resolve(context, isRenderWebService);

    if (isRenderWebService)
    {
        context.Request.Scheme = Uri.UriSchemeHttps;
    }

    await next();
});
app.Use(async (context, next) =>
{
    const string headerName = "X-Correlation-ID";
    var correlationId = context.Request.Headers.TryGetValue(headerName, out var incomingCorrelationId)
        && !string.IsNullOrWhiteSpace(incomingCorrelationId)
        ? incomingCorrelationId.ToString()
        : Guid.NewGuid().ToString("N");
    context.Items[headerName] = correlationId;
    context.Response.Headers[headerName] = correlationId;

    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        await next();
    }
});
// Swagger controlado por configuración
var swaggerEnabled = builder.Configuration.GetValue<bool>("Swagger:Enabled");
if (swaggerEnabled)
{

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // 1) Proteger rutas de swagger (UI + JSON) con Auth + Rol Admin
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var isSwagger =
                path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
            if (!isSwagger)
            {
                await next();
                return;
            }
            // Importante: Authentication/Authorization aún no han corrido aquí,
            // así que forzamos AuthenticateAsync.
            var authResult = await context.AuthenticateAsync("Bearer");
            if (!authResult.Succeeded || authResult.Principal is null)
            {
                await ApiError.WriteAsync(
                    context,
                    ApiError.Unauthorized("No autenticado.", context),
                    context.RequestAborted);
                return;
            }
            context.User = authResult.Principal;
            var isAdmin = context.User.IsInRole("Admin"); // ajusta si tu rol se llama distinto
            if (!isAdmin)
            {
                await ApiError.WriteAsync(
                    context,
                    ApiError.Forbidden("Acceso denegado.", context),
                    context.RequestAborted);
                return;
            }
            await next();
        });
        // 2) Swagger UI y JSON
        app.UseSwagger();
        app.UseSwaggerUI();
    }

}
if (app.Environment.IsProduction())
{
    app.UseHsts();

    if (!isRenderWebService)
    {
        app.UseHttpsRedirection();
    }
}

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
    context.Response.Headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");

    if (app.Environment.IsProduction() &&
        builder.Configuration.GetValue<bool>("SecurityHeaders:EnableCsp"))
    {
        var frameAncestors = builder.Configuration
            .GetSection("SecurityHeaders:FrameAncestors")
            .Get<string[]>() ?? new[] { "'self'", "http://localhost:4200" };
        var frameAncestorsValue = string.Join(" ", frameAncestors);

        var csp = string.Join(" ",
            "default-src 'self';",
            "base-uri 'self';",
            "object-src 'none';",
            "frame-ancestors " + frameAncestorsValue + ";",
            "img-src 'self' data: https:;",
            "font-src 'self' https: data:;",
            "style-src 'self' 'unsafe-inline' https:;",
            "script-src 'self' 'unsafe-inline' https:;",
            "connect-src 'self' https:;",
            "form-action 'self';"
        );

        context.Response.Headers["Content-Security-Policy"] = csp;
    }

    await next();
});
//  Servir login.html como default en /
var defaultFilesOptions = new DefaultFilesOptions();
defaultFilesOptions.DefaultFileNames.Clear();
defaultFilesOptions.DefaultFileNames.Add("login.html");
app.UseDefaultFiles(defaultFilesOptions);

//  Servir archivos de wwwroot (css, js, img, videos, etc.)
app.UseStaticFiles();

app.UseRouting();
app.UseCors("PermitirFrontend"); // ESTO ACTIVA CORS
app.UseRateLimiter(); //aquí (antes de tu ErrorHandlerMiddleware)
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseAuthentication(); // JWT primero
app.UseAuthorization();
app.MapControllers();
static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var payload = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString()
        })
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
}
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Name == "self",
    ResponseWriter = WriteHealthResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Name == "db",
    ResponseWriter = WriteHealthResponse
});
app.Run();

public partial class Program { }
