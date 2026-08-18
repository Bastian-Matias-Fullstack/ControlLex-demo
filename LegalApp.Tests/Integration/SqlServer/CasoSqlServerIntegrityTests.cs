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
