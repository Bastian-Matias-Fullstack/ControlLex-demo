
using Aplicacion.Servicios.Operacional;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace API.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemController : ControllerBase
    {
        private readonly IDatabaseWarmup _databaseWarmup;
        private readonly ILogger<SystemController> _logger;

        private static readonly object _stateLock = new();
        private static Task<WarmupSnapshot>? _inflightWarmupTask;
        private static DateTime _lastWarmupSuccessUtc = DateTime.MinValue;
        private static readonly TimeSpan _successCacheTtl = TimeSpan.FromSeconds(45);

        public SystemController(IDatabaseWarmup databaseWarmup, ILogger<SystemController> logger)
        {
            _databaseWarmup = databaseWarmup;
            _logger = logger;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            var sw = Stopwatch.StartNew();

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();
            var origin = Request.Headers["Origin"].ToString();

            sw.Stop();

            _logger.LogInformation(
                "PING_OK ip={IP} origin={Origin} userAgent={UserAgent} totalMs={TotalMs}",
                ip, origin, userAgent, sw.ElapsedMilliseconds);

            return Ok(new
            {
                ok = true,
                totalMs = sw.ElapsedMilliseconds
            });
        }

        [HttpGet("warmup")]
        public async Task<IActionResult> Warmup(
            [FromQuery] int timeoutMs = 25000,
            CancellationToken cancellationToken = default)
        {
            timeoutMs = Math.Clamp(timeoutMs, 1000, 30000);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();
            var origin = Request.Headers["Origin"].ToString();

            Task<WarmupSnapshot> task;
            var now = DateTime.UtcNow;

            lock (_stateLock)
            {
                if (now - _lastWarmupSuccessUtc < _successCacheTtl)
                {
                    return Ok(new
                    {
                        ok = true,
                        cached = true,
                        message = "Warmup reciente reutilizado."
                    });
                }

                if (_inflightWarmupTask is { IsCompleted: false })
                {
                    task = _inflightWarmupTask;
                }
                else
                {
                    task = RunWarmupInternalAsync();
                    _inflightWarmupTask = task;
                }
            }

            try
            {
                var result = await task.WaitAsync(
                    TimeSpan.FromMilliseconds(timeoutMs),
                    cancellationToken);

                if (!result.Ok)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        ok = false,
                        cached = false,
                        dbMs = result.DbMs,
                        totalMs = result.TotalMs,
                        message = "La demo aún se está preparando."
                    });
                }

                return Ok(new
                {
                    ok = true,
                    cached = false,
                    dbMs = result.DbMs,
                    totalMs = result.TotalMs
                });
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "WARMUP_TIMEOUT ip={IP} origin={Origin} userAgent={UserAgent} timeoutMs={TimeoutMs}",
                    ip, origin, userAgent, timeoutMs);

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    ok = false,
                    warming = true,
                    message = "La demo se está preparando. Intenta nuevamente en unos segundos."
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    ok = false,
                    message = "Warmup cancelado."
                });
            }
            finally
            {
                lock (_stateLock)
                {
                    if (_inflightWarmupTask?.IsCompleted == true)
                    {
                        _inflightWarmupTask = null;
                    }
                }
            }
        }

        private async Task<WarmupSnapshot> RunWarmupInternalAsync()
        {
            var totalSw = Stopwatch.StartNew();
            var dbSw = Stopwatch.StartNew();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                await _databaseWarmup.EjecutarAsync(cts.Token);

                dbSw.Stop();
                totalSw.Stop();

                lock (_stateLock)
                {
                    _lastWarmupSuccessUtc = DateTime.UtcNow;
                }

                _logger.LogInformation(
                    "WARMUP_OK dbMs={DbMs} totalMs={TotalMs}",
                    dbSw.ElapsedMilliseconds, totalSw.ElapsedMilliseconds);

                return new WarmupSnapshot(
                    Ok: true,
                    DbMs: dbSw.ElapsedMilliseconds,
                    TotalMs: totalSw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                dbSw.Stop();
                totalSw.Stop();

                _logger.LogWarning(
                    ex,
                    "WARMUP_FAIL dbMs={DbMs} totalMs={TotalMs}",
                    dbSw.ElapsedMilliseconds, totalSw.ElapsedMilliseconds);

                return new WarmupSnapshot(
                    Ok: false,
                    DbMs: dbSw.ElapsedMilliseconds,
                    TotalMs: totalSw.ElapsedMilliseconds);
            }
        }

        private sealed record WarmupSnapshot(
            bool Ok,
            long DbMs,
            long TotalMs);
    }
}
