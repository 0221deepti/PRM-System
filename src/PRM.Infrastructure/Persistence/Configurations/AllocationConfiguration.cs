using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class AllocationConfiguration : IEntityTypeConfiguration<Allocation>
{
    public void Configure(EntityTypeBuilder<Allocation> builder)
    {
        builder.Property(a => a.UtilisationPercent).IsRequired();

        builder.HasOne(a => a.Employee)
               .WithMany(e => e.Allocations)
               .HasForeignKey(a => a.EmployeeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Project)
               .WithMany(p => p.Allocations)
               .HasForeignKey(a => a.ProjectId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
