namespace PRM.Domain.Exceptions;

public class DuplicateTimesheetException : DomainException
{
    public DuplicateTimesheetException(string message) : base(message) { }
}
