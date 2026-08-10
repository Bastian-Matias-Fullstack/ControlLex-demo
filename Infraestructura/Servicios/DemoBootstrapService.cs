using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aplicacion.Servicios.Auth;
using Aplicacion.Servicios.Demo;
using Dominio.Entidades;
using Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infraestructura.Servicios
{
    public sealed class DemoBootstrapService
    {
        private readonly AppDbContext _context;
        private readonly IHashService _hashService;
        private readonly ILogger<DemoBootstrapService> _logger;

        public DemoBootstrapService(
            AppDbContext context,
            IHashService hashService,
            ILogger<DemoBootstrapService> logger)
        {
            _context = context;
            _hashService = hashService;
            _logger = logger;
        }

        public async Task<bool> BootstrapAsync(
            string demoPassword,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(demoPassword))
            {
                throw new InvalidOperationException(
                    "DemoBootstrap:Password no está configurada.");
            }

            var clientesCount =
                await _context.Clientes.CountAsync(cancellationToken);

            var usuariosCount =
                await _context.Usuarios.CountAsync(cancellationToken);

            var usuarioRolesCount =
                await _context.UsuarioRoles.CountAsync(cancellationToken);

            var casosCount =
                await _context.Casos.CountAsync(cancellationToken);

            var operationalDataIsEmpty =
                clientesCount == 0 &&
                usuariosCount == 0 &&
                usuarioRolesCount == 0 &&
                casosCount == 0;

            if (!operationalDataIsEmpty)
            {
                throw new InvalidOperationException(
                    "El seed demo requiere una base recién migrada y sin datos operativos. " +
                    $"Estado actual: Clientes={clientesCount}, " +
                    $"Usuarios={usuariosCount}, " +
                    $"UsuarioRoles={usuarioRolesCount}, " +
                    $"Casos={casosCount}. No se modificó información.");
            }

            var requiredRoleNames = new[]
            {
                "Admin",
                "Abogado",
                "Soporte"
            };

            var roles = await _context.Roles
                .Where(r => requiredRoleNames.Contains(r.Nombre))
                .ToListAsync(cancellationToken);

            if (roles.Count != requiredRoleNames.Length)
            {
                throw new InvalidOperationException(
                    "No están disponibles todos los roles requeridos por la demo. " +
                    "Verifica que las migrations se hayan aplicado.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var clientes = DemoBootstrapBaseline
                    .GetClientes()
                    .Select(seed => new Cliente(seed.Rut, seed.Nombre))
                    .ToList();

                await _context.Clientes.AddRangeAsync(
                    clientes,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                var clienteIdsPorNombre = clientes.ToDictionary(
                    c => c.Nombre,
                    c => c.Id,
                    StringComparer.Ordinal);

                var roleIdsPorNombre = roles.ToDictionary(
                    r => r.Nombre,
                    r => r.Id,
                    StringComparer.OrdinalIgnoreCase);

                var usuarioSeeds = DemoBootstrapBaseline.GetUsuarios();
                var usuarios = new List<Usuario>();

                foreach (var seed in usuarioSeeds)
                {
                    if (!roleIdsPorNombre.TryGetValue(
                            seed.Rol,
                            out var rolId))
                    {
                        throw new InvalidOperationException(
                            $"Rol demo no encontrado: {seed.Rol}");
                    }

                    var usuario = new Usuario(
                        seed.Nombre,
                        seed.Email,
                        _hashService.Hash(demoPassword))
                    {
                        EsDemoProtegido = true
                    };

                    usuario.UsuarioRoles.Add(new UsuarioRol
                    {
                        RolId = rolId
                    });

                    usuarios.Add(usuario);
                }

                await _context.Usuarios.AddRangeAsync(
                    usuarios,
                    cancellationToken);

                var casos = DemoResetBaseline
                    .GetCasosBase()
                    .Select(seed =>
                    {
                        if (!clienteIdsPorNombre.TryGetValue(
                                seed.NombreCliente,
                                out var clienteId))
                        {
                            throw new InvalidOperationException(
                                $"Cliente demo no encontrado para el caso: {seed.NombreCliente}");
                        }

                        return new Caso
                        {
                            Titulo = seed.Titulo,
                            Descripcion = seed.Descripcion,
                            NombreCliente = seed.NombreCliente,
                            TipoCaso = (TipoCaso)seed.TipoCaso,
                            FechaCreacion =
                                new DateTimeOffset(seed.FechaCreacion),
                            Estado = MapEstadoCaso(seed.Estado),
                            ClienteId = clienteId,
                            FechaCambioEstado = seed.FechaCambioEstado,
                            FechaCierre = seed.FechaCierre,
                            MotivoCierre = seed.MotivoCierre,
                            CreatedBy = seed.CreatedBy,
                            ModifiedBy = seed.ModifiedBy,
                            UpdatedAt = seed.UpdatedAt.HasValue
                                ? new DateTimeOffset(seed.UpdatedAt.Value)
                                : null
                        };
                    })
                    .ToList();

                await _context.Casos.AddRangeAsync(
                    casos,
                    cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "DEMO_BOOTSTRAP_OK clientes={Clientes} usuarios={Usuarios} casos={Casos}",
                    clientes.Count,
                    usuarios.Count,
                    casos.Count);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static EstadoCaso MapEstadoCaso(string estado)
        {
            return estado switch
            {
                "Pendiente" => EstadoCaso.Pendiente,
                "EnProceso" => EstadoCaso.EnProceso,
                "Cerrado" => EstadoCaso.Cerrado,
                _ => throw new InvalidOperationException(
                    $"Estado demo no soportado: {estado}")
            };
        }
    }
}