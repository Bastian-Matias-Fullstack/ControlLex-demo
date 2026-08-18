using Dominio.Entidades;

namespace LegalApp.Tests.Domain;

public class CasoTests
{
    [Fact]
    public void Cerrar_CasoActivo_RegistraEstadoYFechaTerminal()
    {
        var caso = new Caso { Estado = EstadoCaso.EnProceso };
        var fecha = new DateTime(2026, 8, 18, 12, 0, 0, DateTimeKind.Utc);

        caso.Cerrar(fecha, "Acuerdo aprobado");

        Assert.Equal(EstadoCaso.Cerrado, caso.Estado);
        Assert.Equal(fecha, caso.FechaCierre);
        Assert.Equal(fecha, caso.FechaCambioEstado);
        Assert.Equal("Acuerdo aprobado", caso.MotivoCierre);
    }

    [Fact]
    public void Cerrar_CasoYaCerrado_FallaSinMutacionParcial()
    {
        var fechaOriginal = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        var caso = new Caso
        {
            Estado = EstadoCaso.Cerrado,
            FechaCierre = fechaOriginal,
            FechaCambioEstado = fechaOriginal,
            MotivoCierre = "Motivo original"
        };

        var action = () => caso.Cerrar(fechaOriginal.AddDays(1), "Motivo nuevo");

        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal(EstadoCaso.Cerrado, caso.Estado);
        Assert.Equal(fechaOriginal, caso.FechaCierre);
        Assert.Equal(fechaOriginal, caso.FechaCambioEstado);
        Assert.Equal("Motivo original", caso.MotivoCierre);
    }
}
