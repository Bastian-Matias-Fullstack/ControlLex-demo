using Aplicacion.DTO;
using Aplicacion.DTOs;
using Aplicacion.Excepciones;
using Aplicacion.Repositorio;
using Dominio.Entidades;

namespace Aplicacion.Servicios.Casos
{
    public class ListarCasosService
    {
        public const int TamanioMaximo = 100;

        private static readonly HashSet<string> OrdenesValidos =
        [
            "fecha_desc",
            "fecha_asc",
            "titulo_asc",
            "titulo_desc"
        ];

        private readonly ICasoRepository _casoRepository;

        public ListarCasosService(ICasoRepository casoRepository)
        {
            _casoRepository = casoRepository;
        }

        public async Task<ResultadoPaginadoConResumen<CasoDto>> EjecutarAsync(
            FiltroCasosRequest filtro)
        {
            ValidarYNormalizar(filtro);
            return await _casoRepository.ObtenerPaginaAsync(filtro);
        }

        private static void ValidarYNormalizar(FiltroCasosRequest filtro)
        {
            if (filtro.Pagina < 1)
            {
                throw new InvalidRequestException(
                    "La página debe ser mayor o igual a 1."
                );
            }

            if (filtro.Tamanio < 1 || filtro.Tamanio > TamanioMaximo)
            {
                throw new InvalidRequestException(
                    $"El tamaño de página debe estar entre 1 y {TamanioMaximo}."
                );
            }

            filtro.Buscar = Normalizar(filtro.Buscar);
            filtro.Estado = Normalizar(filtro.Estado);
            filtro.Orden = Normalizar(filtro.Orden);

            if (filtro.Estado is not null)
            {
                var estadoValido = Enum
                    .GetNames<EstadoCaso>()
                    .FirstOrDefault(nombre => string.Equals(
                        nombre,
                        filtro.Estado,
                        StringComparison.OrdinalIgnoreCase));

                if (estadoValido is null)
                {
                    throw new InvalidRequestException(
                        $"El estado '{filtro.Estado}' no es válido."
                    );
                }

                filtro.Estado = estadoValido;
            }

            if (filtro.Orden is not null && !OrdenesValidos.Contains(filtro.Orden))
            {
                throw new InvalidRequestException(
                    $"El orden '{filtro.Orden}' no es válido."
                );
            }

            if (filtro.Desde > filtro.Hasta)
            {
                throw new InvalidRequestException(
                    "La fecha Desde no puede ser posterior a Hasta."
                );
            }
        }

        private static string? Normalizar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}
