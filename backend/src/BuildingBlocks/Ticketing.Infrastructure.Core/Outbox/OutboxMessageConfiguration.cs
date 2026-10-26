using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ticketing.Infrastructure.Core.Outbox;

/// <summary>
/// Cấu hình EF Core cho OutboxMessages.
/// Index cho dòng chưa xử lý để worker quét nhanh.
/// </summary>
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.Error).HasMaxLength(2000);

        // Index cho worker: chỉ quét dòng chưa xử lý, sắp theo thời gian tạo
        builder.HasIndex(x => new { x.ProcessedAt, x.CreatedAt })
            .HasFilter("\"ProcessedAt\" IS NULL")
            .HasDatabaseName("IX_OutboxMessages_Unprocessed");
    }
}
