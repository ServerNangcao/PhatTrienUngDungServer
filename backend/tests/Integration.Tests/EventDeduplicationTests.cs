using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ticketing.Infrastructure.Core;
using Ticketing.Infrastructure.Core.Inbox;
using Ticketing.Infrastructure.Core.Outbox;
using Ticketing.SharedKernel;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// DbContext thử nghiệm cho test tích hợp kế thừa từ BaseDbContext.
/// </summary>
public sealed class TestModuleDbContext : BaseDbContext
{
    public TestModuleDbContext(DbContextOptions<TestModuleDbContext> options)
        : base(options, "test_module")
    {
    }
}

/// <summary>
/// Sample Integration Event cho test.
/// </summary>
public sealed record TestIntegrationEvent : IntegrationEvent
{
    public string Message { get; init; } = string.Empty;
}

public class EventDeduplicationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestModuleDbContext> _options;

    public EventDeduplicationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TestModuleDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new TestModuleDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tiêu chí Giai đoạn 2 & Điểm 4: Giao một event hai lần, Inbox chỉ cho phép xử lý một lần.
    /// Lần 1: TryAddInboxMessageAsync trả về true -> Handler thực thi.
    /// Lần 2: TryAddInboxMessageAsync trả về false -> Bỏ qua.
    /// </summary>
    [Fact]
    public async Task Same_Event_Delivered_Twice_Is_Handled_Only_Once()
    {
        var eventId = Guid.NewGuid();
        const string handlerName = "OrderPaidNotificationHandler";
        var executionCount = 0;

        // Lần 1: Event được giao lần đầu
        using (var context = new TestModuleDbContext(_options))
        {
            var isNew = await context.TryAddInboxMessageAsync(eventId, handlerName);
            if (isNew)
            {
                executionCount++;
                await context.SaveChangesAsync();
            }
        }

        // Lần 2: Event bị giao lặp lại (retry hoặc mạng chập chờn)
        using (var context = new TestModuleDbContext(_options))
        {
            var isNew = await context.TryAddInboxMessageAsync(eventId, handlerName);
            if (isNew)
            {
                executionCount++;
                await context.SaveChangesAsync();
            }
        }

        // Khẳng định: Handler chỉ được gọi đúng 1 lần
        Assert.Equal(1, executionCount);

        // Khẳng định: Bảng InboxMessages chỉ có đúng 1 dòng
        using (var context = new TestModuleDbContext(_options))
        {
            var count = await context.InboxMessages.CountAsync(x => x.EventId == eventId);
            Assert.Equal(1, count);
        }
    }

    /// <summary>
    /// Tiêu chí Giai đoạn 2 & Điểm 4: Hai worker chạy đồng thời tranh chấp xử lý cùng một event.
    /// Unique constraint (EventId, Handler) đảm bảo chỉ đúng 1 worker thành công.
    /// </summary>
    [Fact]
    public async Task Concurrent_Workers_Handling_Same_Event_Only_One_Succeeds()
    {
        var eventId = Guid.NewGuid();
        const string handlerName = "SendTicketEmailHandler";
        var successfulInserts = 0;
        var duplicateCatches = 0;

        var tasks = Enumerable.Range(0, 2).Select(async _ =>
        {
            using var context = new TestModuleDbContext(_options);
            try
            {
                var isNew = await context.TryAddInboxMessageAsync(eventId, handlerName);
                if (isNew)
                {
                    await context.SaveChangesAsync();
                    Interlocked.Increment(ref successfulInserts);
                }
                else
                {
                    Interlocked.Increment(ref duplicateCatches);
                }
            }
            catch (DbUpdateException)
            {
                // Unique index violation khi race condition chèn đồng thời
                Interlocked.Increment(ref duplicateCatches);
            }
        });

        await Task.WhenAll(tasks);

        Assert.Equal(1, successfulInserts);
        Assert.Equal(1, duplicateCatches);

        using (var context = new TestModuleDbContext(_options))
        {
            var inboxCount = await context.InboxMessages.CountAsync(x => x.EventId == eventId && x.Handler == handlerName);
            Assert.Equal(1, inboxCount);
        }
    }

    /// <summary>
    /// Tiêu chí Điểm 4: Outbox chỉ lưu tên class ngắn (EventType = short name, không chứa namespace)
    /// để đổi namespace sau này không làm vỡ việc deserialize event.
    /// </summary>
    [Fact]
    public async Task Outbox_Message_Uses_Short_Class_Name_Not_Full_Name()
    {
        var integrationEvent = new TestIntegrationEvent
        {
            Message = "Test message"
        };

        using (var context = new TestModuleDbContext(_options))
        {
            context.AddOutboxMessage(integrationEvent);
            await context.SaveChangesAsync();
        }

        using (var context = new TestModuleDbContext(_options))
        {
            var message = await context.OutboxMessages.FirstOrDefaultAsync(x => x.EventId == integrationEvent.EventId);
            Assert.NotNull(message);
            Assert.Equal(nameof(TestIntegrationEvent), message.EventType);
            Assert.DoesNotContain(".", message.EventType);
            Assert.Equal(integrationEvent.EventId, message.EventId);
        }
    }

    /// <summary>
    /// Tiêu chí Điểm 4: Job dọn dẹp Outbox/Inbox xóa các bản ghi đã xử lý cũ hơn N ngày.
    /// </summary>
    [Fact]
    public async Task Outbox_Cleanup_Query_Identifies_Old_Processed_Messages()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);

        using (var context = new TestModuleDbContext(_options))
        {
            // Bản ghi cũ đã xử lý (cần xóa)
            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "OldProcessedEvent",
                EventId = Guid.NewGuid(),
                Payload = "{}",
                CreatedAt = cutoff.AddDays(-2),
                ProcessedAt = cutoff.AddDays(-1)
            });

            // Bản ghi mới đã xử lý (chưa xóa)
            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "RecentProcessedEvent",
                EventId = Guid.NewGuid(),
                Payload = "{}",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ProcessedAt = DateTime.UtcNow.AddMinutes(-30)
            });

            // Bản ghi chưa xử lý (tuyệt đối không xóa)
            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "UnprocessedEvent",
                EventId = Guid.NewGuid(),
                Payload = "{}",
                CreatedAt = cutoff.AddDays(-5),
                ProcessedAt = null
            });

            await context.SaveChangesAsync();
        }

        using (var context = new TestModuleDbContext(_options))
        {
            var toDelete = await context.OutboxMessages
                .Where(x => x.ProcessedAt != null && x.ProcessedAt < cutoff)
                .ToListAsync();

            Assert.Single(toDelete);
            Assert.Equal("OldProcessedEvent", toDelete[0].EventType);
        }
    }
}
