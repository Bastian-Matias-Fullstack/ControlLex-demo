namespace Aplicacion.Servicios.Operacional
{
    public interface IDatabaseWarmup
    {
        Task EjecutarAsync(CancellationToken cancellationToken);
    }
}
