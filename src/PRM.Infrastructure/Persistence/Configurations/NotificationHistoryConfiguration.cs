using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PRM.Domain.Entities;

namespace PRM.Infrastructure.Persistence.Configurations;

public class NotificationHistoryConfiguration : IEntityTypeConfiguration<NotificationHistory>
{
    public void Configure(EntityTypeBuilder<NotificationHistory> builder)
    {
        builder.Property(x => x.SentTo).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.ProjectId, x.NotificationType, x.SentTo });

        builder.HasOne(x => x.Project)
            .WithMany(p => p.NotificationHistories)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}