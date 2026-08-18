using Aplicacion.Repositorio;
using Aplicacion.Excepciones;
using Dominio.Entidades;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;

namespace LegalApp.Tests.Integration;

internal enum CasoRepositoryFailure
{
    None,
    Dependency,
    Unexpected,
    Concurrency
}

internal sealed class ControlLexApiFactory : WebApplicationFactory<Program>
{
    internal const string JwtKey = "controllex-integration-tests-signing-key-2026";
    internal const string JwtIssuer = "ControlLex.IntegrationTests";
    internal const string JwtAudience = "ControlLex.IntegrationTests.Client";
    internal const string SensitiveMarker = "INTERNAL-SENSITIVE-MARKER";

    private readonly CasoRepositoryFailure _failure;

    public ControlLexApiFactory(CasoRepositoryFailure failure = CasoRepositoryFailure.None)
    {
        _failure = failure;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", JwtIssuer);
        builder.UseSetting("Jwt:Audience", JwtAudience);
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Server=(localdb)\\mssqllocaldb;Database=ControlLexIntegrationTests;Trusted_Connection=True;");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICasoRepository>();
            services.RemoveAll<IUsuarioRepositorio>();

            var casoRepository = new Mock<ICasoRepository>();
            switch (_failure)
            {
                case CasoRepositoryFailure.Dependency:
                    casoRepository
                        .Setup(repository => repository.ObtenerPorIdAsync(It.IsAny<int>()))
                        .ThrowsAsync(new TimeoutException(SensitiveMarker));
                    break;
                case CasoRepositoryFailure.Unexpected:
                    casoRepository
                        .Setup(repository => repository.ObtenerPorIdAsync(It.IsAny<int>()))
                        .ThrowsAsync(new Exception(SensitiveMarker));
                    break;
                case CasoRepositoryFailure.Concurrency:
                    casoRepository
                        .Setup(repository => repository.ObtenerPorIdAsync(It.IsAny<int>()))
                        .ReturnsAsync(new Caso
                        {
                            Id = 1,
                            Titulo = "Caso concurrente",
                            Descripcion = "Descripción original",
                            Estado = EstadoCaso.Pendiente,
                            ClienteId = 1,
                            Version = new byte[8]
                        });
                    casoRepository
                        .Setup(repository => repository.ActualizarAsync(
                            It.IsAny<Caso>(),
                            It.IsAny<byte[]>()))
                        .ThrowsAsync(new BusinessConflictException(
                            "El caso fue modificado por otro usuario. " +
                            "Recarga los datos e inténtalo nuevamente."));
                    break;
                default:
                    casoRepository
                        .Setup(repository => repository.ObtenerPorIdAsync(It.IsAny<int>()))
                        .ReturnsAsync((Caso?)null);
                    break;
            }

            var usuarioRepository = new Mock<IUsuarioRepositorio>();
            usuarioRepository
                .Setup(repository => repository.ObtenerPorIdAsync(2))
                .ReturnsAsync(new Usuario
                {
                    Id = 2,
                    Nombre = "Usuario Demo",
                    Email = "demo@controllex.test",
                    PasswordHash = "not-used",
                    EsDemoProtegido = true
                });

            services.AddSingleton(casoRepository.Object);
            services.AddSingleton(usuarioRepository.Object);
        });
    }
}
