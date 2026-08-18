using Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Infraestructura.Persistencia;
using Aplicacion.Repositorio;
using Aplicacion.DTO;
using Aplicacion.DTOs;

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
            await _context.SaveChangesAsync();
        }
        public async Task ActualizarAsync(Caso caso)
        {
            _context.Casos.Update(caso);
            await _context.SaveChangesAsync();
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

            query = filtro.Orden switch
            {
                "fecha_desc" => query.OrderByDescending(c => c.FechaCreacion),
                "fecha_asc" => query.OrderBy(c => c.FechaCreacion),
                "titulo_asc" => query.OrderBy(c => c.Titulo),
                "titulo_desc" => query.OrderByDescending(c => c.Titulo),
                _ => query.OrderByDescending(c => c.Id)
            };

            var total = await query.CountAsync();
            var skip = (filtro.Pagina - 1) * filtro.Tamanio;
            var items = await query
                .Skip(skip)
                .Take(filtro.Tamanio)
                .Select(c => new CasoDto
                {
                    Id = c.Id,
                    Titulo = c.Titulo,
                    Estado = c.Estado,
                    FechaCreacion = c.FechaCreacion,
                    ClienteId = c.ClienteId,
                    NombreCliente = c.Cliente.Nombre,
                    Descripcion = c.Descripcion ?? "",
                    MotivoCierre = c.MotivoCierre ?? "",
                    TipoCaso = c.TipoCaso
                })
                .ToListAsync();
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

    }
}
