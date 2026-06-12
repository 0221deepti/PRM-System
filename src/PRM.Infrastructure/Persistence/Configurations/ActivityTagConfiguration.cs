using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class ActivityTagConfiguration : IEntityTypeConfiguration<ActivityTag>
{
    public void Configure(EntityTypeBuilder<ActivityTag> builder)
    {
        builder.HasIndex(at => at.Name).IsUnique();
        builder.Property(at => at.Name).HasMaxLength(100).IsRequired();
        builder.Property(at => at.Description).HasMaxLength(300);
        builder.Property(at => at.Color).HasMaxLength(7);

        builder.HasMany(at => at.EntryTags)
               .WithOne(tet => tet.ActivityTag)
               .HasForeignKey(tet => tet.ActivityTagId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
