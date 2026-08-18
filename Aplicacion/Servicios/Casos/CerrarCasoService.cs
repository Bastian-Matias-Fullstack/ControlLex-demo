using Aplicacion.DTO;
using Aplicacion.Excepciones;
using Aplicacion.Repositorio;
using Dominio.Entidades;

namespace Aplicacion.Servicios.Casos
{
    public class CerrarCasoService
    {
        private readonly ICasoRepository _casoRepository;
        public CerrarCasoService(ICasoRepository casoRepository)
        {
            _casoRepository = casoRepository;
        }

        public async Task EjecutarAsync(
            int casoId,
            CerrarCasoRequest request,
            string? usuarioActual = null)
        {
            // Validación de request
            if (request is null)
                throw new InvalidEstadoCasoException(
                    "La solicitud no contiene datos válidos para el cierre."
                );

            request.MotivoCierre = (request.MotivoCierre ?? string.Empty).Trim();

            // Auditoría
            var actor = string.IsNullOrWhiteSpace(usuarioActual)
                ? "Sistema"
                : usuarioActual.Trim();

            // Obtener caso
            var caso = await _casoRepository.ObtenerPorIdAsync(casoId);

            if (caso is null)
                throw new NotFoundException("El caso no existe.");

            // Reglas de negocio
            if (caso.EstaCerrado())
                throw new BusinessConflictException("El caso ya está cerrado.");

            if (caso.Estado == EstadoCaso.EnProceso)
            {
                if (string.IsNullOrWhiteSpace(caso.Descripcion))
                    throw new InvalidEstadoCasoException(
                        "No se puede cerrar un caso sin descripción."
                    );

            }
            else if (caso.Estado == EstadoCaso.Pendiente)
            {
                if (string.IsNullOrWhiteSpace(request.MotivoCierre))
                    throw new InvalidEstadoCasoException(
                        "Debe ingresar un motivo para cerrar un caso pendiente."
                    );
            }
            else
            {
                throw new InvalidEstadoCasoException(
                    "No se puede cerrar este caso en su estado actual."
                );
            }
            // Auditoría y persistencia
            var fechaCierre = DateTime.UtcNow;
            caso.Cerrar(fechaCierre, request.MotivoCierre);
            caso.UpdatedAt = fechaCierre;
            caso.ModifiedBy = actor;

            await _casoRepository.ActualizarAsync(caso);
        }
    }
}
