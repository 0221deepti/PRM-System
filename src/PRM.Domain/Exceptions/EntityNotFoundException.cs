namespace PRM.Domain.Exceptions;

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string message) : base(message) { }
}
