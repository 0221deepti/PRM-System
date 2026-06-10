using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class Skill : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public SkillCategory Category { get; set; }

    // Navigation
    public ICollection<EmployeeSkill> EmployeeSkills { get; set; } = new List<EmployeeSkill>();
}
