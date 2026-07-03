namespace PRM.Domain.Exceptions;

public class OverAllocationException : DomainException
{
    public OverAllocationException(string message) : base(message) { }
}
