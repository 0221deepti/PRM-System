using PRM.Domain.Enums;

namespace PRM.Domain.Entities;

public class UserSkill : BaseEntity
{
    public int UserId { get; set; }
    public int SkillId { get; set; }
    public SkillProficiency Proficiency { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
