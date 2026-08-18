using Aplicacion.Excepciones;
using Aplicacion.Servicios.Casos;
using FluentAssertions;

namespace LegalApp.Tests.Application.Casos;

public class CasoVersionTokenTests
{
    [Fact]
    public void CodificarYDecodificar_RowVersionValida_PreservaBytes()
    {
        byte[] version = [1, 2, 3, 4, 5, 6, 7, 8];

        var token = CasoVersionToken.Codificar(version);
        var decoded = CasoVersionToken.Decodificar(token);

        decoded.Should().Equal(version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-base64")]
    [InlineData("AQID")]
    public void Decodificar_TokenAusenteOInvalido_LanzaInvalidRequest(
        string? token)
    {
        var action = () => CasoVersionToken.Decodificar(token);

        action.Should().Throw<InvalidRequestException>();
    }
}
