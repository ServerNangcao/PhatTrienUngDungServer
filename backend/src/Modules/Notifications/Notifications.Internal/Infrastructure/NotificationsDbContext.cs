using Microsoft.EntityFrameworkCore;
using Notifications.Internal.Domain;
using Ticketing.Infrastructure.Core;

namespace Notifications.Internal.Infrastructure;

internal sealed class NotificationsDbContext : BaseDbContext
{
    public const string SchemaName = "notifications";

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
        : base(options, SchemaName)
    {
    }

    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.ToTable("EmailLogs", SchemaName);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecipientEmail).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.Property(x => x.SentAt).IsRequired();

            entity.HasIndex(x => x.RecipientEmail);
            entity.HasIndex(x => x.SentAt);
        });
    }
}
