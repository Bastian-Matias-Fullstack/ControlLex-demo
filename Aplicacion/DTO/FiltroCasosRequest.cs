namespace Aplicacion.DTOs
{
    public class FiltroCasosRequest
    {
        public int Pagina { get; set; } = 1;

        public int Tamanio { get; set; } = 10;

        public string? Estado { get; set; }

        public string? Buscar { get; set; }

        public string? Orden { get; set; }

        public DateTime? Desde { get; set; }

        public DateTime? Hasta { get; set; }
    }
}
