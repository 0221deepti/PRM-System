namespace PRM.Domain.Exceptions;

public class PrmAuthenticationException : DomainException
{
    public PrmAuthenticationException(string message) : base(message) { }
}
