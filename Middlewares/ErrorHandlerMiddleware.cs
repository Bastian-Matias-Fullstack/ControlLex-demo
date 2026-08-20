using Aplicacion.Excepciones;
using API.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace API.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _env;
        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var isDbError = IsDatabaseException(ex);
                var statusCode = ex switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    BusinessConflictException => StatusCodes.Status409Conflict,
                    DomainException => StatusCodes.Status400BadRequest,
                    _ when isDbError => StatusCodes.Status503ServiceUnavailable,
                    _ => StatusCodes.Status500InternalServerError
                };
  
                var title = statusCode switch
                {
                    404 => "Recurso no encontrado",
                    409 => "Conflicto de negocio",
                    400 => "Solicitud inválida",
                    503 => "Servicio no disponible",
                    _ => "Error interno del servidor"
                };

                // 🔒 En prod NO filtramos detalle interno
                var safeDetail = statusCode switch
                {
                    500 when !_env.IsDevelopment() =>
                        "Ocurrió un error interno. Intenta nuevamente.",
                    503 when !_env.IsDevelopment() =>
                        "Servicio temporalmente no disponible. Intenta nuevamente.",
                    _ => ex.Message
                };

                if (statusCode >= StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(ex, "Error no controlado. TraceId={TraceId}", context.TraceIdentifier);
                }
                else
                {
                    _logger.LogWarning(
                        ex,
                        "Solicitud rechazada. StatusCode={StatusCode} TraceId={TraceId}",
                        statusCode,
                        context.TraceIdentifier);
                }

                var problem = ApiError.Create(statusCode, title, safeDetail, context);
                await ApiError.WriteAsync(context, problem, context.RequestAborted);
            }
        }

        private static bool IsDatabaseException(Exception ex)
        {
            if (ex is null) return false;

            // 1) Recorrer cadena normal
            for (var cur = ex; cur != null; cur = cur.InnerException)
            {
                if (cur is SqlException) return true;
                if (cur is DbUpdateException) return true;

                // timeouts típicos de conexión / comando
                if (cur is TimeoutException) return true;
                if (cur is TaskCanceledException) return true;
                if (cur is OperationCanceledException) return true;
            }

            // 2) AggregateException (muy común con async/EF)
            if (ex is AggregateException agg)
            {
                foreach (var inner in agg.Flatten().InnerExceptions)
                {
                    if (IsDatabaseException(inner)) return true;
                }
            }

            return false;
        }
    }
}
