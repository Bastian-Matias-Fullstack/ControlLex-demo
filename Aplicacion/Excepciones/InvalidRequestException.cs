namespace Aplicacion.Excepciones
{
    public sealed class InvalidRequestException : DomainException
    {
        public InvalidRequestException(string message) : base(message)
        {
        }
    }
}
