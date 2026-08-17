using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


/*helper ApiError necesita el HttpContext para rellenar Instance = context.Request.Path.

Entonces… ¡todos tus controladores actuales pueden usar esta clase sin hacer nada extra! /*/
/* Centralizas los errores.

Evitas repetir código.

Estás aplicando DRY y buenas prácticas realistas.

Compatible con cualquier ControllerBase.

 */
namespace API.Helpers
{
    public static class ApiError
    {
        public static ProblemDetails BadRequest(string message, HttpContext httpContext) =>
            Create(StatusCodes.Status400BadRequest, "Solicitud inválida", message, httpContext);

        public static ProblemDetails Unauthorized(string message, HttpContext httpContext) =>
            Create(StatusCodes.Status401Unauthorized, "No autenticado", message, httpContext);

        public static ProblemDetails Forbidden(string message, HttpContext httpContext) =>
            Create(StatusCodes.Status403Forbidden, "Acceso denegado", message, httpContext);

        public static ProblemDetails NotFound(string message, HttpContext httpContext) =>
            Create(StatusCodes.Status404NotFound, "No encontrado", message, httpContext);

        public static ProblemDetails Conflict(string message, HttpContext httpContext) =>
            Create(StatusCodes.Status409Conflict, "Conflicto de negocio", message, httpContext);

        public static ProblemDetails TooManyRequests(string message, HttpContext httpContext) =>
            Create(StatusCodes.Status429TooManyRequests, "Demasiadas solicitudes", message, httpContext);

        public static ProblemDetails InternalError(string message, HttpContext httpContext) =>
            Create(StatusCodes.Status500InternalServerError, "Error interno del servidor", message, httpContext);

        public static ProblemDetails ServiceUnavailable(string message, HttpContext httpContext) =>
            Create(StatusCodes.Status503ServiceUnavailable, "Servicio no disponible", message, httpContext);

        public static ProblemDetails Create(
            int statusCode,
            string title,
            string detail,
            HttpContext context)
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Type = statusCode switch
                {
                    StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                    StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                    StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                    StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                    StatusCodes.Status429TooManyRequests => "https://tools.ietf.org/html/rfc6585#section-4",
                    StatusCodes.Status503ServiceUnavailable => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
                    _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                }
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;
            if (context.Items.TryGetValue("X-Correlation-ID", out var correlationId) &&
                correlationId is not null)
            {
                problem.Extensions["correlationId"] = correlationId.ToString();
            }

            return problem;
        }

        public static async Task WriteAsync(
            HttpContext context,
            ProblemDetails problem,
            CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json",
                cancellationToken: cancellationToken);
        }
    }
}
