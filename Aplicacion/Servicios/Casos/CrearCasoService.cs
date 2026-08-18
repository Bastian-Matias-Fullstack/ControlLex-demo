using Aplicacion.DTO;
using Aplicacion.DTOs;
using Aplicacion.Excepciones;
using Aplicacion.Repositorio;
using Aplicacion.Servicios;
using Aplicacion.Servicios.Casos;
using Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Aplicacion.Casos
{

    /*Este método sigue un flujo claro: validar, formatear, evitar duplicados, 
     * crear cliente si no existe y guardar el caso. Es limpio,
     * mantenible y respeta las reglas del negocio.*/
    public class CrearCasoService
    {
        private readonly ICasoRepository _casoRepository;
        private readonly FormateadorNombreService _formateador;
        private readonly IClienteRepository _clienteRepository;

        public CrearCasoService(ICasoRepository casoRepository, IClienteRepository clienteRepository, FormateadorNombreService formateador)
        {
            _casoRepository = casoRepository;
            _formateador = formateador;
            _clienteRepository = clienteRepository; 
        }
        public async Task<CasoDto> EjecutarAsync(
            CrearCasoRequest request,
            string? usuarioActual = null)
        {
                var actor = string.IsNullOrWhiteSpace(usuarioActual)
                    ? "Sistema"
                    : usuarioActual.Trim();

                // 🔹 0. Normalizar input (AQUÍ VA EL CAMBIO)
                request.Titulo = request.Titulo?.Trim() ?? string.Empty;
                request.Descripcion = request.Descripcion?.Trim() ?? string.Empty;
                // 1. Validaciones
                if (string.IsNullOrWhiteSpace(request.Titulo))
                    throw new InvalidRequestException("El título del caso es obligatorio.");
                if (request.ClienteId <= 0)
                    throw new InvalidRequestException("Debe seleccionar un cliente válido.");
                var cliente = await _clienteRepository.ObtenerPorIdAsync(request.ClienteId);
                if (cliente is null)
                    throw new NotFoundException("El cliente no existe.");

                var clienteTieneCasoActivo =
                    await _casoRepository.ExisteCasoActivoParaClienteAsync(
                        cliente.Id,
                        0);

                if (clienteTieneCasoActivo)
                    throw new BusinessConflictException(
                        "El cliente ya tiene otro caso activo."
                    );

                //6. Crear caso
                var nuevoCaso = new Caso
                {
                    Titulo = request.Titulo,
                    Descripcion = request.Descripcion,
                    TipoCaso = request.TipoCaso,
                    ClienteId = cliente.Id,
                    Cliente = cliente,
                    NombreCliente = cliente.Nombre,
                    FechaCreacion = DateTimeOffset.UtcNow,
                    Estado = EstadoCaso.Pendiente,
                    CreatedBy = actor
                };
                await _casoRepository.CrearAsync(nuevoCaso);
                // 6. Retornar DTO
                return new CasoDto
                {
            Id = nuevoCaso.Id,
            Titulo = nuevoCaso.Titulo,
            Estado = nuevoCaso.Estado,
            FechaCreacion = nuevoCaso.FechaCreacion,
            NombreCliente = cliente.Nombre,
            TipoCaso = nuevoCaso.TipoCaso,
            Descripcion = nuevoCaso.Descripcion,
            Version = CasoVersionToken.Codificar(nuevoCaso.Version)
                };
        }
    }
}

