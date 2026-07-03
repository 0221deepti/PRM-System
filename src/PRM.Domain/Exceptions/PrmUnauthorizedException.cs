namespace PRM.Domain.Exceptions;

public class PrmUnauthorizedException : DomainException
{
    public PrmUnauthorizedException(string message) : base(message) { }
}
