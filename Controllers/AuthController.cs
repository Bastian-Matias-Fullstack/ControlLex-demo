using Microsoft.AspNetCore.Mvc;
using API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Aplicacion.Servicios.Auth;
using Aplicacion.DTO;
using Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _context;
    private readonly IHashService _hashService;
    private readonly ILogger<AuthController> _logger;
    private readonly ILoginLockoutService _lockoutService;

    public AuthController(AppDbContext context, IJwtService jwtService, IHashService hashService, ILogger<AuthController> logger, ILoginLockoutService lockoutService)
    {
        _jwtService = jwtService;
        _context = context;
        _hashService = hashService;
        _logger = logger;
        _lockoutService = lockoutService;
    }
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        const string credencialesInvalidas = "Credenciales inválidas";

        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        // Información del cliente para auditoría de seguridad
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        if (_lockoutService.IsLocked(email, out var lockedUntil))
        {
            _logger.LogWarning(
                "LOGIN_LOCKED email={Email} ip={IP} userAgent={UserAgent} lockedUntilUtc={LockedUntilUtc}",
                email, ip, userAgent, lockedUntil);
            var problem = ApiError.TooManyRequests(
                "Cuenta temporalmente bloqueada por múltiples intentos fallidos.",
                HttpContext);
            problem.Extensions["lockedUntilUtc"] = lockedUntil;
            return StatusCode(StatusCodes.Status429TooManyRequests, problem);
        }
        /* aqui usamos EF CORE YA QUE ES CONSULTA SIMPLE */
        var usuario = await _context.Usuarios
           .Include(u => u.UsuarioRoles)
           .ThenInclude(ur => ur.Rol)
           .FirstOrDefaultAsync(u => u.Email == email);
        if (usuario == null)
        {
            var failedCount = _lockoutService.RegisterFailure(email, out var lockedAfterFailure);
            _logger.LogWarning(
            "LOGIN_FAIL email={Email} ip={IP} userAgent={UserAgent} reason=USER_NOT_FOUND failedCount={FailedCount} lockedUntilUtc={LockedUntilUtc}",
            email, ip, userAgent, failedCount, lockedAfterFailure);

            if (lockedAfterFailure is not null)
            {
                var problem = ApiError.TooManyRequests(
                    "Cuenta temporalmente bloqueada por múltiples intentos fallidos.",
                    HttpContext);
                problem.Extensions["lockedUntilUtc"] = lockedAfterFailure;
                return StatusCode(StatusCodes.Status429TooManyRequests, problem);
            }
            return Unauthorized(ApiError.Unauthorized(credencialesInvalidas, HttpContext));
        }
        // Verificar contraseña con IHashService
        var esValida = _hashService.Verificar(dto.Password, usuario.PasswordHash);
        if (!esValida)
        {
            var failedCount = _lockoutService.RegisterFailure(email, out var lockedAfterFailure);

            _logger.LogWarning(
            "LOGIN_FAIL email={Email} ip={IP} userAgent={UserAgent} reason=BAD_PASSWORD failedCount={FailedCount} lockedUntilUtc={LockedUntilUtc}",
             email, ip, userAgent, failedCount, lockedAfterFailure);

            if (lockedAfterFailure is not null)
            {
                var problem = ApiError.TooManyRequests(
                    "Cuenta temporalmente bloqueada por múltiples intentos fallidos.",
                    HttpContext);
                problem.Extensions["lockedUntilUtc"] = lockedAfterFailure;
                return StatusCode(StatusCodes.Status429TooManyRequests, problem);
            }
            return Unauthorized(ApiError.Unauthorized(credencialesInvalidas, HttpContext));

        }
        _lockoutService.Reset(email);
        // Extraer los nombres de los roles
        var roles = usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList();
        // Generar token JWT
        var token = _jwtService.GenerarToken(usuario.Email, usuario.Id, roles);
        _logger.LogInformation(
    "LOGIN_OK email={Email} ip={IP} userAgent={UserAgent} userId={UserId} roles={Roles}",
    email, ip, userAgent, usuario.Id, string.Join(",", roles));
        return Ok(new { token });

    }
}
