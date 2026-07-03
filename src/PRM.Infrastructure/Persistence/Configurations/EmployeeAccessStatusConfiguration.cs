using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class EmployeeAccessStatusConfiguration : IEntityTypeConfiguration<EmployeeAccessStatus>
{
    public void Configure(EntityTypeBuilder<EmployeeAccessStatus> builder)
    {
        builder.Property(x => x.TrackedWeekStartDate).IsRequired();

        builder.HasIndex(x => new { x.EmployeeId, x.TrackedWeekStartDate }).IsUnique();

        builder.HasOne(x => x.Employee)
            .WithMany(u => u.AccessStatuses)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RestoredByUser)
            .WithMany()
            .HasForeignKey(x => x.RestoredBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}