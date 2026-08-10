using System.Collections.Generic;

namespace Aplicacion.Servicios.Demo
{
    public static class DemoBootstrapBaseline
    {
        public static IReadOnlyList<DemoClienteSeedItem> GetClientes()
        {
            return new List<DemoClienteSeedItem>
            {
                new(1, "Constructora Andina SpA", "76.123.456-7"),
                new(2, "Inversiones Los Robles Ltda.", "77.234.567-8"),
                new(3, "Comercializadora Sur Global SpA", "78.345.678-9"),
                new(4, "Servicios Jurídicos Altamira Ltda.", "79.456.789-1"),
                new(5, "Transporte y Logística Patagonia SpA", "76.567.890-2"),
                new(6, "Agroexportadora Valle Verde SpA", "77.111.222-3"),
                new(7, "Tecnología Nimbus Chile Ltda.", "78.222.333-4"),
                new(8, "Inmobiliaria Costa Norte SpA", "79.333.444-5"),
                new(9, "Retail Central Plaza S.A.", "76.444.555-6"),
                new(10, "Fundación Apoyo Social Horizonte", "65.555.666-7")
            };
        }

        public static IReadOnlyList<DemoUsuarioSeedItem> GetUsuarios()
        {
            return new List<DemoUsuarioSeedItem>
            {
                new("admin", "admin@legal.cl", "Admin"),
                new("soporte", "soporte@legal.cl", "Soporte"),
                new("abogado", "abogado@legal.cl", "Abogado")
            };
        }
    }

    public sealed record DemoClienteSeedItem(
        int Id,
        string Nombre,
        string Rut);

    public sealed record DemoUsuarioSeedItem(
        string Nombre,
        string Email,
        string Rol);
}
