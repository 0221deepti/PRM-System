using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.Property(c => c.LlmProvider).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LlmApiKey).HasMaxLength(256);
        builder.Property(c => c.LlmApiUrl).HasMaxLength(500);
        builder.Property(c => c.LlmModelName).HasMaxLength(100);
        builder.Property(c => c.SchedulerIntervalHours).IsRequired();
        builder.Property(c => c.MaxWeeklyHours).IsRequired();
    }
}
