using PRM.Domain.Enums;

namespace PRM.Client.Session;

/// <summary>
/// Holds the current session state after login.
/// Single Responsibility: only stores authentication state.
/// </summary>
public class SessionContext
{
    public string? Token { get; set; }
    public string? UserFullName { get; set; }
    public UserRole Role { get; set; }
    public int UserId { get; set; }
    public int EmployeeId { get; set; }
    public bool IsLoggedIn => Token != null;

    public void Clear()
    {
        Token = null;
        UserFullName = null;
        UserId = 0;
        EmployeeId = 0;
    }
}
