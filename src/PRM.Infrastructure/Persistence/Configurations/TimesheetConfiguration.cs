using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.HasIndex(t => new { t.EmployeeId, t.ProjectId, t.WeekStartDate }).IsUnique();

        builder.Property(t => t.HoursWorked).HasPrecision(5, 2);
        builder.Property(t => t.ActivityTags).HasMaxLength(500);

        builder.HasOne(t => t.Employee)
               .WithMany(e => e.Timesheets)
               .HasForeignKey(t => t.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Project)
               .WithMany()
               .HasForeignKey(t => t.ProjectId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
