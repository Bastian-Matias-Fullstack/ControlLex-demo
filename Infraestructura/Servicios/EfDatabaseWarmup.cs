using Aplicacion.Servicios.Operacional;
using Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Infraestructura.Servicios
{
    public sealed class EfDatabaseWarmup : IDatabaseWarmup
    {
        private readonly AppDbContext _context;

        public EfDatabaseWarmup(AppDbContext context)
        {
            _context = context;
        }

        public async Task EjecutarAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _context.Database.OpenConnectionAsync(cancellationToken);
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                await _context.Usuarios
                    .AsNoTracking()
                    .Select(u => u.Id)
                    .Take(1)
                    .ToListAsync(cancellationToken);
            }
            finally
            {
                try
                {
                    await _context.Database.CloseConnectionAsync();
                }
                catch
                {
                    // Preserve the original warmup result when cleanup cannot close the connection.
                }
            }
        }
    }
}
