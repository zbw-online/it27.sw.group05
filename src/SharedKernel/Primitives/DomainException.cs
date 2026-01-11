namespace SharedKernel.Primitives
{
    public sealed class DomainException(string message) : Exception(message)
    {
    }
}
