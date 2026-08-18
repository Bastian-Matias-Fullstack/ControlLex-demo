using Aplicacion.DTO;
using Aplicacion.DTOs;
using Aplicacion.Repositorio;

namespace Aplicacion.Servicios.Casos
{
    public class ListarCasosService
    {
        private readonly ICasoRepository _casoRepository;

        public ListarCasosService(ICasoRepository casoRepository)
        {
            _casoRepository = casoRepository;
        }

        public async Task<ResultadoPaginadoConResumen<CasoDto>> EjecutarAsync(
            FiltroCasosRequest filtro)
        {
            return await _casoRepository.ObtenerPaginaAsync(filtro);
        }
    }
}
