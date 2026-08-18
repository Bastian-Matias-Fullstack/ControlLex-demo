using Aplicacion.Servicios.Demo;

namespace LegalApp.Tests.Application.Demo;

public class DemoResetBaselineTests
{
    [Fact]
    public void GetCasosBase_MantieneConteosEsperados()
    {
        var casos = DemoResetBaseline.GetCasosBase();

        Assert.Equal(15, casos.Count);
        Assert.Equal(5, casos.Count(c => c.Estado == "Pendiente"));
        Assert.Equal(5, casos.Count(c => c.Estado == "EnProceso"));
        Assert.Equal(5, casos.Count(c => c.Estado == "Cerrado"));
    }

    [Fact]
    public void GetCasosBase_DistribuyeUnCasoActivoPorClienteDemo()
    {
        var clientesActivos = DemoResetBaseline
            .GetCasosBase()
            .Where(c => c.Estado != "Cerrado")
            .Select(c => c.ClienteId)
            .ToList();

        Assert.Equal(10, clientesActivos.Count);
        Assert.Equal(10, clientesActivos.Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 10), clientesActivos.OrderBy(id => id));
    }

    [Fact]
    public void GetCasosBase_MantieneClienteIdYNombreConsistentes()
    {
        var clientesPorId = DemoBootstrapBaseline
            .GetClientes()
            .ToDictionary(c => c.Id, c => c.Nombre);

        foreach (var caso in DemoResetBaseline.GetCasosBase())
        {
            Assert.Equal(clientesPorId[caso.ClienteId], caso.NombreCliente);
        }
    }

    [Fact]
    public void GetCasosBase_UsaActorSeedEnTodosLosCasos()
    {
        var casos = DemoResetBaseline.GetCasosBase();

        Assert.All(casos, caso => Assert.Equal("seed", caso.CreatedBy));
    }
}
