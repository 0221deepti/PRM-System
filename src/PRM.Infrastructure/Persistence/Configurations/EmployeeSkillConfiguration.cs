using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class EmployeeSkillConfiguration : IEntityTypeConfiguration<EmployeeSkill>
{
    public void Configure(EntityTypeBuilder<EmployeeSkill> builder)
    {
        builder.HasIndex(es => new { es.EmployeeId, es.SkillId }).IsUnique();

        builder.HasOne(es => es.Employee)
               .WithMany(e => e.Skills)
               .HasForeignKey(es => es.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(es => es.Skill)
               .WithMany(s => s.EmployeeSkills)
               .HasForeignKey(es => es.SkillId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
