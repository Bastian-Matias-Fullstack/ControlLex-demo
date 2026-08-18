using Aplicacion.Excepciones;

namespace Aplicacion.Servicios.Casos;

public static class CasoVersionToken
{
    private const int RowVersionLength = 8;

    public static string Codificar(byte[] version)
    {
        return Convert.ToBase64String(version);
    }

    public static byte[] Decodificar(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidRequestException(
                "La versión del caso es obligatoria."
            );
        }

        try
        {
            var version = Convert.FromBase64String(token);

            if (version.Length != RowVersionLength)
            {
                throw new InvalidRequestException(
                    "La versión del caso no es válida."
                );
            }

            return version;
        }
        catch (FormatException)
        {
            throw new InvalidRequestException(
                "La versión del caso no es válida."
            );
        }
    }
}
