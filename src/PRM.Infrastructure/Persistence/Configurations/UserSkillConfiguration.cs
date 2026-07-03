using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> builder)
    {
        builder.HasIndex(us => new { us.UserId, us.SkillId }).IsUnique();

        builder.HasOne(us => us.User)
               .WithMany(u => u.Skills)
               .HasForeignKey(us => us.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(us => us.Skill)
               .WithMany(s => s.UserSkills)
               .HasForeignKey(us => us.SkillId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
