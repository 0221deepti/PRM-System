using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class Employee : BaseEntity
{
    public int UserId { get; set; }
    public string Department { get; set; } = string.Empty;
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Bench;
    public int? ManagerId { get; set; }  // FK → another Employee

    // Navigation
    public User User { get; set; } = null!;
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
    public ICollection<EmployeeSkill> Skills { get; set; } = new List<EmployeeSkill>();
    public ICollection<Allocation> Allocations { get; set; } = new List<Allocation>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
