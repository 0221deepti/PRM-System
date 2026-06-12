using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class TimesheetEntryTagConfiguration : IEntityTypeConfiguration<TimesheetEntryTag>
{
    public void Configure(EntityTypeBuilder<TimesheetEntryTag> builder)
    {
        builder.HasIndex(tet => new { tet.TimesheetEntryId, tet.ActivityTagId }).IsUnique();

        builder.HasOne(tet => tet.TimesheetEntry)
               .WithMany(te => te.Tags)
               .HasForeignKey(tet => tet.TimesheetEntryId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tet => tet.ActivityTag)
               .WithMany(at => at.EntryTags)
               .HasForeignKey(tet => tet.ActivityTagId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
