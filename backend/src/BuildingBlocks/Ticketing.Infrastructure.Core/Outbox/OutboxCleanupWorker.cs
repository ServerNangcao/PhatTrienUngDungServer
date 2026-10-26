using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ticketing.Infrastructure.Core.Outbox;

/// <summary>
/// Background job dọn dữ liệu Outbox và Inbox đã xử lý sau N ngày.
/// Chạy mỗi giờ, xóa batch nhỏ để không block.
/// </summary>
public sealed class OutboxCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxCleanupWorker> _logger;

    public OutboxCleanupWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxOptions> options,
        ILogger<OutboxCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxCleanupWorker started. Cleanup after {Days} days",
            _options.CleanupAfterDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxCleanupWorker encountered an error");
            }

            // Chạy mỗi giờ
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var markers = scope.ServiceProvider.GetServices<BaseDbContextMarker>();
        var cutoff = DateTime.UtcNow.AddDays(-_options.CleanupAfterDays);

        foreach (var marker in markers)
        {
            if (scope.ServiceProvider.GetService(marker.DbContextType) is not BaseDbContext dbContext)
            {
                continue;
            }

            // Xóa OutboxMessages đã xử lý
            var outboxDeleted = await dbContext.OutboxMessages
                .Where(x => x.ProcessedAt != null && x.ProcessedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            // Xóa InboxMessages cũ
            var inboxDeleted = await dbContext.InboxMessages
                .Where(x => x.ProcessedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (outboxDeleted > 0 || inboxDeleted > 0)
            {
                _logger.LogInformation(
                    "Cleanup schema '{Schema}': deleted {OutboxCount} outbox, {InboxCount} inbox messages",
                    dbContext.Schema, outboxDeleted, inboxDeleted);
            }
        }
    }
}
