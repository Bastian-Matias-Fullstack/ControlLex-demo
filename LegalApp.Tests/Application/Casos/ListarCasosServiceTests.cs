using Aplicacion.DTOs;
using Aplicacion.Excepciones;
using Aplicacion.Repositorio;
using Aplicacion.Servicios.Casos;
using Dominio.Entidades;
using Infraestructura.Persistencia;
using Infraestructura.Repositorios;
using Microsoft.EntityFrameworkCore;

public class ListarCasosServiceTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static void SeedCasos(AppDbContext context)
    {
        var clienteJuan = new Cliente("1-9", "Juan Perez");
        var clienteMaria = new Cliente("2-7", "Maria Lopez");

        context.Clientes.AddRange(clienteJuan, clienteMaria);

        context.Casos.AddRange(
            new Caso
            {
                Titulo = "Caso Pendiente Juan",
                Estado = EstadoCaso.Pendiente,
                Cliente = clienteJuan,
                TipoCaso = TipoCaso.Civil
            },
            new Caso
            {
                Titulo = "Caso Cerrado Juan",
                Estado = EstadoCaso.Cerrado,
                Cliente = clienteJuan,
                TipoCaso = TipoCaso.Civil
            },
            new Caso
            {
                Titulo = "Caso Pendiente Maria",
                Estado = EstadoCaso.Pendiente,
                Cliente = clienteMaria,
                TipoCaso = TipoCaso.Penal
            }
        );

        context.SaveChanges();
    }

    [Fact]
    public async Task EjecutarAsync_AplicaFiltroPorEstadoYBusqueda()
    {
        // Arrange
        using var context = CrearContexto();
        SeedCasos(context);

        ICasoRepository repository = new CasoRepository(context);
        var service = new ListarCasosService(repository);

        var filtro = new FiltroCasosRequest
        {
            Estado = "Pendiente",
            Buscar = "Juan"
        };

        // Act
        var resultado = await service.EjecutarAsync(filtro);

        // Assert
        Assert.Single(resultado.Items);
        Assert.Equal("Caso Pendiente Juan", resultado.Items[0].Titulo);
    }

    [Fact]
    public async Task EjecutarAsync_AplicaPaginacionCorrectamente()
    {
        // Arrange
        using var context = CrearContexto();

        var cliente = new Cliente("3-3", "Cliente Test");
        context.Clientes.Add(cliente);

        for (int i = 1; i <= 25; i++)
        {
            context.Casos.Add(new Caso
            {
                Titulo = $"Caso {i}",
                Estado = EstadoCaso.Pendiente,
                Cliente = cliente,
                TipoCaso = TipoCaso.Civil
            });
        }

        context.SaveChanges();

        ICasoRepository repository = new CasoRepository(context);
        var service = new ListarCasosService(repository);

        var filtro = new FiltroCasosRequest
        {
            Pagina = 2,
            Tamanio = 10
        };

        // Act
        var resultado = await service.EjecutarAsync(filtro);

        // Assert
        Assert.Equal(10, resultado.Items.Count);
        Assert.Equal(25, resultado.TotalRegistros);
        Assert.Equal(2, resultado.Pagina);
        Assert.Equal(3, resultado.TotalPaginas);
    }

    [Fact]
    public async Task EjecutarAsync_CalculaResumenCorrectamente()
    {
        // Arrange
        using var context = CrearContexto();

        var cliente = new Cliente("4-4", "Cliente Resumen");
        context.Clientes.Add(cliente);

        context.Casos.AddRange(
            new Caso { Estado = EstadoCaso.Pendiente, Cliente = cliente, TipoCaso = TipoCaso.Civil },
            new Caso { Estado = EstadoCaso.Pendiente, Cliente = cliente, TipoCaso = TipoCaso.Civil },
            new Caso { Estado = EstadoCaso.Cerrado, Cliente = cliente, TipoCaso = TipoCaso.Penal }
        );

        context.SaveChanges();

        ICasoRepository repository = new CasoRepository(context);
        var service = new ListarCasosService(repository);

        var filtro = new FiltroCasosRequest();

        // Act
        var resultado = await service.EjecutarAsync(filtro);

        // Assert
        // Assert
        Assert.NotNull(resultado.Resumen);
        Assert.Equal(3, resultado.Resumen.Total);
        Assert.Equal(2, resultado.Resumen.Pendientes);
        Assert.Equal(1, resultado.Resumen.Resueltos);

        Assert.Equal(3, resultado.TotalRegistros);
        Assert.Equal(1, resultado.TotalPaginas);
    }
    [Fact]
    public async Task EjecutarAsync_OrdenaPorTituloAsc()
    {
        using var context = CrearContexto();

        var cliente = new Cliente("5-5", "Cliente Orden");
        context.Clientes.Add(cliente);

        context.Casos.AddRange(
            new Caso { Titulo = "Zeta", Estado = EstadoCaso.Pendiente, Cliente = cliente, TipoCaso = TipoCaso.Civil },
            new Caso { Titulo = "Alfa", Estado = EstadoCaso.Pendiente, Cliente = cliente, TipoCaso = TipoCaso.Civil },
            new Caso { Titulo = "Beta", Estado = EstadoCaso.Pendiente, Cliente = cliente, TipoCaso = TipoCaso.Civil }
        );
        context.SaveChanges();

        ICasoRepository repository = new CasoRepository(context);
        var service = new ListarCasosService(repository);

        var filtro = new FiltroCasosRequest { Orden = "titulo_asc", Pagina = 1, Tamanio = 10 };

        var resultado = await service.EjecutarAsync(filtro);

        Assert.Equal(new[] { "Alfa", "Beta", "Zeta" }, resultado.Items.Select(i => i.Titulo).ToArray());
    }

    [Fact]
    public async Task EjecutarAsync_SoloDesde_FiltraInclusivamente()
    {
        using var context = CrearContexto();
        SeedCasosConFechas(context);
        var service = CrearServicio(context);

        var resultado = await service.EjecutarAsync(new FiltroCasosRequest
        {
            Desde = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero)
        });

        Assert.Equal(
            ["Caso nuevo", "Caso intermedio"],
            resultado.Items.Select(item => item.Titulo).ToArray());
    }

    [Fact]
    public async Task EjecutarAsync_SoloHasta_FiltraInclusivamente()
    {
        using var context = CrearContexto();
        SeedCasosConFechas(context);
        var service = CrearServicio(context);

        var resultado = await service.EjecutarAsync(new FiltroCasosRequest
        {
            Hasta = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero)
        });

        Assert.Equal(
            ["Caso intermedio", "Caso antiguo"],
            resultado.Items.Select(item => item.Titulo).ToArray());
    }

    [Fact]
    public async Task EjecutarAsync_DesdeYHasta_FiltraRangoInclusivo()
    {
        using var context = CrearContexto();
        SeedCasosConFechas(context);
        var service = CrearServicio(context);

        var resultado = await service.EjecutarAsync(new FiltroCasosRequest
        {
            Desde = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
            Hasta = new DateTimeOffset(2026, 1, 25, 0, 0, 0, TimeSpan.Zero)
        });

        Assert.Equal("Caso intermedio", Assert.Single(resultado.Items).Titulo);
    }

    [Fact]
    public async Task EjecutarAsync_DesdePosteriorAHasta_LanzaInvalidRequest()
    {
        using var context = CrearContexto();
        var service = CrearServicio(context);
        var filtro = new FiltroCasosRequest
        {
            Desde = new DateTimeOffset(2026, 1, 21, 0, 0, 0, TimeSpan.Zero),
            Hasta = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero)
        };

        var action = () => service.EjecutarAsync(filtro);

        await Assert.ThrowsAsync<InvalidRequestException>(action);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task EjecutarAsync_PaginacionFueraDeRango_LanzaInvalidRequest(
        int pagina,
        int tamanio)
    {
        using var context = CrearContexto();
        var service = CrearServicio(context);

        var action = () => service.EjecutarAsync(new FiltroCasosRequest
        {
            Pagina = pagina,
            Tamanio = tamanio
        });

        await Assert.ThrowsAsync<InvalidRequestException>(action);
    }

    [Theory]
    [InlineData("0", null)]
    [InlineData("Archivado", null)]
    [InlineData(null, "fecha")]
    [InlineData(null, "titulo asc")]
    public async Task EjecutarAsync_EstadoUOrdenInvalido_LanzaInvalidRequest(
        string? estado,
        string? orden)
    {
        using var context = CrearContexto();
        var service = CrearServicio(context);

        var action = () => service.EjecutarAsync(new FiltroCasosRequest
        {
            Estado = estado,
            Orden = orden
        });

        await Assert.ThrowsAsync<InvalidRequestException>(action);
    }

    private static ListarCasosService CrearServicio(AppDbContext context)
    {
        return new ListarCasosService(new CasoRepository(context));
    }

    private static void SeedCasosConFechas(AppDbContext context)
    {
        var cliente = new Cliente("6-6", "Cliente Fechas");
        context.Clientes.Add(cliente);
        context.Casos.AddRange(
            CrearCasoConFecha(
                "Caso antiguo",
                new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
                cliente),
            CrearCasoConFecha(
                "Caso intermedio",
                new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero),
                cliente),
            CrearCasoConFecha(
                "Caso nuevo",
                new DateTimeOffset(2026, 1, 30, 0, 0, 0, TimeSpan.Zero),
                cliente));
        context.SaveChanges();
    }

    private static Caso CrearCasoConFecha(
        string titulo,
        DateTimeOffset fecha,
        Cliente cliente)
    {
        return new Caso
        {
            Titulo = titulo,
            Estado = EstadoCaso.Cerrado,
            Cliente = cliente,
            TipoCaso = TipoCaso.Civil,
            FechaCreacion = fecha
        };
    }
}
