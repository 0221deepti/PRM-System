using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string Department { get; set; } = string.Empty;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Bench;
    public int? ManagerId { get; set; }  // FK → another User (self-referencing)
    public bool IsActive { get; set; } = true;
    public bool ForcePasswordChange { get; set; } = true;

    // Navigation
    public Role Role { get; set; } = null!;
    public User? Manager { get; set; }
    public ICollection<User> DirectReports { get; set; } = new List<User>();
    public ICollection<UserSkill> Skills { get; set; } = new List<UserSkill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
    public ICollection<EmployeeAccessStatus> AccessStatuses { get; set; } = new List<EmployeeAccessStatus>();
}
