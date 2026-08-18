using Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Infraestructura.Persistencia;
using Aplicacion.Repositorio;
using Aplicacion.DTO;
using Aplicacion.DTOs;
using Aplicacion.Excepciones;
using Infraestructura.Persistencia.Configuraciones;
using Microsoft.Data.SqlClient;
using Aplicacion.Servicios.Casos;

namespace Infraestructura.Repositorios
{
    public class CasoRepository : ICasoRepository
    {
        private readonly AppDbContext _context;
        public CasoRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Caso>> ObtenerTodosAsync()
        {
            return await _context.Casos.ToListAsync();
        }
        public async Task<Caso?> ObtenerPorIdAsync(int casoId)
        {
            return await _context.Casos
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.Id == casoId);
        }
        public async Task CrearAsync(Caso nuevoCaso)
        {
            _context.Casos.Add(nuevoCaso);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (EsConflictoDeCasoActivo(ex))
            {
                throw new BusinessConflictException(
                    "El cliente ya tiene otro caso activo."
                );
            }
        }
        public async Task ActualizarAsync(Caso caso, byte[] versionEsperada)
        {
            _context.Casos.Update(caso);
            _context.Entry(caso)
                .Property(c => c.Version)
                .OriginalValue = versionEsperada;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new BusinessConflictException(
                    "El caso fue modificado por otro usuario. " +
                    "Recarga los datos e inténtalo nuevamente."
                );
            }
            catch (DbUpdateException ex) when (EsConflictoDeCasoActivo(ex))
            {
                throw new BusinessConflictException(
                    "El cliente ya tiene otro caso activo."
                );
            }
        }
        public async Task EliminarAsync(Caso caso)
        {
            _context.Casos.Remove(caso);
            await _context.SaveChangesAsync();
        }
        public async Task<Caso?> ObtenerPorId(int id)
        {
            return await _context.Casos.FindAsync(id);
        }
        public async Task<List<ConteoPorClienteDto>> ObtenerConteoCasosPorClienteAsync()
        {
            return await _context.Casos
             .Include(c => c.Cliente) // 👈 esto trae el nombre del cliente
             .GroupBy(c => new { c.ClienteId, c.Cliente.Nombre })
             .Select(g => new ConteoPorClienteDto
             {
                 ClienteId = g.Key.ClienteId,
                 NombreCliente = g.Key.Nombre,
                 CantidadCasos = g.Count()
             })
             .ToListAsync();
        }
        public async Task<ResultadoPaginadoConResumen<CasoDto>> ObtenerPaginaAsync(
            FiltroCasosRequest filtro)
        {
            var query = _context.Casos
                .Include(c => c.Cliente)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filtro.Estado) &&
                Enum.TryParse<EstadoCaso>(filtro.Estado, true, out var estadoEnum))
            {
                query = query.Where(c => c.Estado == estadoEnum);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Buscar))
            {
                query = query.Where(c =>
                    c.Titulo.Contains(filtro.Buscar) ||
                    c.Cliente.Nombre.Contains(filtro.Buscar));
            }

            if (filtro.Desde.HasValue)
            {
                query = query.Where(c => c.FechaCreacion >= filtro.Desde.Value);
            }

            if (filtro.Hasta.HasValue)
            {
                query = query.Where(c => c.FechaCreacion <= filtro.Hasta.Value);
            }

            query = filtro.Orden switch
            {
                "fecha_desc" => query
                    .OrderByDescending(c => c.FechaCreacion)
                    .ThenByDescending(c => c.Id),
                "fecha_asc" => query
                    .OrderBy(c => c.FechaCreacion)
                    .ThenBy(c => c.Id),
                "titulo_asc" => query
                    .OrderBy(c => c.Titulo)
                    .ThenBy(c => c.Id),
                "titulo_desc" => query
                    .OrderByDescending(c => c.Titulo)
                    .ThenByDescending(c => c.Id),
                _ => query.OrderByDescending(c => c.Id)
            };

            var total = await query.CountAsync();
            var skip = (filtro.Pagina - 1) * filtro.Tamanio;
            var rows = await query
                .Skip(skip)
                .Take(filtro.Tamanio)
                .Select(c => new
                {
                    c.Id,
                    c.Titulo,
                    c.Estado,
                    c.FechaCreacion,
                    c.ClienteId,
                    NombreCliente = c.Cliente.Nombre,
                    c.Descripcion,
                    c.MotivoCierre,
                    c.TipoCaso,
                    c.Version
                })
                .ToListAsync();

            var items = rows
                .Select(c => new CasoDto
                {
                    Id = c.Id,
                    Titulo = c.Titulo,
                    Estado = c.Estado,
                    FechaCreacion = c.FechaCreacion,
                    ClienteId = c.ClienteId,
                    NombreCliente = c.NombreCliente,
                    Descripcion = c.Descripcion ?? "",
                    MotivoCierre = c.MotivoCierre ?? "",
                    TipoCaso = c.TipoCaso,
                    Version = CasoVersionToken.Codificar(c.Version)
                })
                .ToList();
            var resumen = new ResumenCasosDto
            {
                Total = total,
                Pendientes = await query.CountAsync(c => c.Estado == EstadoCaso.Pendiente),
                Resueltos = await query.CountAsync(c => c.Estado == EstadoCaso.Cerrado)
            };

            return new ResultadoPaginadoConResumen<CasoDto>
            {
                Items = items,
                TotalRegistros = total,
                Pagina = filtro.Pagina,
                Tamanio = filtro.Tamanio,
                TotalPaginas = (int)Math.Ceiling((double)total / filtro.Tamanio),
                Resumen = resumen
            };
        }
        public async Task<List<Caso>> ObtenerPorEstadoAsync(EstadoCaso estado)
        {
            return await _context.Casos
                .Where(c => c.Estado == estado)
                .ToListAsync();
        }
        public async Task<bool> ExistenCasosCreadosPorUsuarioAsync(string email)
        {
            return await _context.Casos
                .AnyAsync(c => c.CreatedBy == email);
        }
        public async Task<bool> ExisteCasoActivoParaClienteAsync(int clienteId, int casoId)
        {
            return await _context.Casos.AnyAsync(c =>
        c.ClienteId == clienteId &&
        c.Id != casoId &&
        c.Estado != EstadoCaso.Cerrado
    );
        }

        private static bool EsConflictoDeCasoActivo(DbUpdateException exception)
        {
            for (Exception? current = exception;
                 current is not null;
                 current = current.InnerException)
            {
                if (current is SqlException sqlException &&
                    sqlException.Number is 2601 or 2627 &&
                    sqlException.Message.Contains(
                        CasoConfiguration.ActiveCaseUniqueIndexName,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
