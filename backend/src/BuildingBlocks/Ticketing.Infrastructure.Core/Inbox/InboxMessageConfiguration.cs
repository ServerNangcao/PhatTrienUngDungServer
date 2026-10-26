using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ticketing.Infrastructure.Core.Inbox;

/// <summary>
/// Cấu hình EF Core cho InboxMessages.
/// Unique (EventId, Handler) là cơ chế chống xử lý lặp chính.
/// </summary>
internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Handler).HasMaxLength(256).IsRequired();

        // Chống xử lý lặp: cùng EventId + Handler = đã xử lý rồi
        builder.HasIndex(x => new { x.EventId, x.Handler })
            .IsUnique()
            .HasDatabaseName("IX_InboxMessages_EventId_Handler");
    }
}
