using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class EmployeeSkill : BaseEntity
{
    public int EmployeeId { get; set; }
    public int SkillId { get; set; }
    public SkillProficiency Proficiency { get; set; }

    public Employee Employee { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
