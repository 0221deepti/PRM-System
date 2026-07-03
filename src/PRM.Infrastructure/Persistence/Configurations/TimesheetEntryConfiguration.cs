using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class TimesheetEntryConfiguration : IEntityTypeConfiguration<TimesheetEntry>
{
    public void Configure(EntityTypeBuilder<TimesheetEntry> builder)
    {
        builder.Property(te => te.HoursWorked).HasPrecision(5, 2).IsRequired();
        builder.Property(te => te.Description).HasMaxLength(500);

        builder.HasOne(te => te.Timesheet)
               .WithMany(t => t.Entries)
               .HasForeignKey(te => te.TimesheetId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(te => te.Tags)
               .WithOne(tet => tet.TimesheetEntry)
               .HasForeignKey(tet => tet.TimesheetEntryId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
