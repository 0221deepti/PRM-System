using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.Property(e => e.Department).HasMaxLength(100).IsRequired();

        builder.HasOne(e => e.User)
               .WithOne(u => u.Employee)
               .HasForeignKey<Employee>(e => e.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Manager)
               .WithMany(e => e.DirectReports)
               .HasForeignKey(e => e.ManagerId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
