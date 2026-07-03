using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class TimesheetConfiguration : IEntityTypeConfiguration<Timesheet>
{
    public void Configure(EntityTypeBuilder<Timesheet> builder)
    {
        builder.HasIndex(t => new { t.UserId, t.ProjectId, t.WeekStartDate }).IsUnique();

        builder.Property(t => t.TotalHoursWorked).HasPrecision(5, 2);

        builder.HasOne(t => t.User)
               .WithMany(u => u.Timesheets)
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Project)
               .WithMany()
               .HasForeignKey(t => t.ProjectId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Entries)
               .WithOne(e => e.Timesheet)
               .HasForeignKey(e => e.TimesheetId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
