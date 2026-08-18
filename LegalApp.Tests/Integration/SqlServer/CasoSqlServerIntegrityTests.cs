using Aplicacion.Excepciones;
using Dominio.Entidades;
using FluentAssertions;
using Infraestructura.Persistencia;
using Infraestructura.Repositorios;
using Microsoft.EntityFrameworkCore;

namespace LegalApp.Tests.Integration.SqlServer;

public class CasoSqlServerIntegrityTests
{
    private const string ConnectionVariable =
        "CONTROLLEX_SQL_TEST_CONNECTION";

    [Fact]
    public async Task CrearAsync_SegundoCasoActivo_TraduceSoloIndiceConocidoAConflicto()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var context = CreateContext(connectionString);
        var casoExistente = await context.Casos
            .AsNoTracking()
            .FirstAsync(caso => caso.Estado != EstadoCaso.Cerrado);
        var repository = new CasoRepository(context);
        var duplicado = CreateCase(casoExistente.ClienteId);

        Func<Task> action = async () => await repository.CrearAsync(duplicado);

        await action.Should()
            .ThrowAsync<BusinessConflictException>()
            .WithMessage("*otro caso activo*");
    }

    [Fact]
    public async Task CrearAsync_OtraViolacionSql_NoSeMapeaAConflictoDeCasoActivo()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var context = CreateContext(connectionString);
        var repository = new CasoRepository(context);
        var casoSinCliente = CreateCase(int.MaxValue);

        Func<Task> action = async () =>
            await repository.CrearAsync(casoSinCliente);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ActualizarAsync_DosLecturasConMismaVersion_RechazaStaleWrite()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var casoId = 0;

        try
        {
            await using (var setupContext = CreateContext(connectionString))
            {
                var repository = new CasoRepository(setupContext);
                var caso = CreateCase(clienteId: 1);
                caso.Estado = EstadoCaso.Cerrado;
                caso.FechaCierre = DateTime.UtcNow;

                await repository.CrearAsync(caso);
                casoId = caso.Id;
            }

            await using var firstContext = CreateContext(connectionString);
            await using var secondContext = CreateContext(connectionString);
            var firstRepository = new CasoRepository(firstContext);
            var secondRepository = new CasoRepository(secondContext);
            var firstCase = await firstContext.Casos.SingleAsync(c => c.Id == casoId);
            var secondCase = await secondContext.Casos.SingleAsync(c => c.Id == casoId);
            var versionN = firstCase.Version.ToArray();

            secondCase.Version.Should().Equal(versionN);

            firstCase.Descripcion = "Primera modificación persistida";
            await firstRepository.ActualizarAsync(firstCase, versionN);

            firstCase.Version.Should().NotEqual(versionN);

            secondCase.Descripcion = "Segunda modificación obsoleta";
            Func<Task> staleWrite = async () =>
                await secondRepository.ActualizarAsync(secondCase, versionN);

            await staleWrite.Should()
                .ThrowAsync<BusinessConflictException>()
                .WithMessage("*modificado por otro usuario*");

            await using var verifyContext = CreateContext(connectionString);
            var persistedDescription = await verifyContext.Casos
                .Where(c => c.Id == casoId)
                .Select(c => c.Descripcion)
                .SingleAsync();

            persistedDescription.Should().Be("Primera modificación persistida");
        }
        finally
        {
            if (casoId > 0)
            {
                await using var cleanupContext = CreateContext(connectionString);
                await cleanupContext.Casos
                    .Where(c => c.Id == casoId)
                    .ExecuteDeleteAsync();
            }
        }
    }

    private static AppDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static Caso CreateCase(int clienteId)
    {
        return new Caso
        {
            Titulo = "Prueba de integridad SQL",
            Descripcion = "Caso no persistido por la prueba",
            NombreCliente = "Cliente de prueba",
            TipoCaso = TipoCaso.Civil,
            FechaCreacion = DateTimeOffset.UtcNow,
            Estado = EstadoCaso.Pendiente,
            ClienteId = clienteId,
            CreatedBy = "sql-test"
        };
    }
}
