using System.Reflection;
using NetArchTest.Rules;
using Xunit;

// Module class imports (public duy nhất trong mỗi Internal)
using Identity.Internal.Api;
using Events.Internal.Api;
using Ticketing.Internal.Api;
using Orders.Internal.Api;
using CheckIn.Internal.Api;
using Notifications.Internal.Api;

// Module Contracts imports
using Identity.Contracts;
using Events.Contracts;
using Ticketing.Contracts;
using Orders.Contracts;
using CheckIn.Contracts;
using Notifications.Contracts;

namespace Architecture.Tests;

/// <summary>
/// Bốn luật kiến trúc kiểm tra ranh giới module.
/// Chạy trong CI, vi phạm thì build đỏ.
/// </summary>
public class ModuleBoundaryTests
{
    private static readonly string[] Modules =
        ["Identity", "Events", "Ticketing", "Orders", "CheckIn", "Notifications"];

    // Mỗi Internal chỉ có một class public (XxxModule) nên dùng nó để lấy assembly
    private static readonly Dictionary<string, Assembly> Internals = new()
    {
        ["Identity"] = typeof(IdentityModule).Assembly,
        ["Events"] = typeof(EventsModule).Assembly,
        ["Ticketing"] = typeof(TicketingModule).Assembly,
        ["Orders"] = typeof(OrdersModule).Assembly,
        ["CheckIn"] = typeof(CheckInModule).Assembly,
        ["Notifications"] = typeof(NotificationsModule).Assembly,
    };

    private static readonly Dictionary<string, Assembly> Contracts = new()
    {
        ["Identity"] = typeof(IIdentityModuleApi).Assembly,
        ["Events"] = typeof(IEventsModuleApi).Assembly,
        ["Ticketing"] = typeof(ITicketingModuleApi).Assembly,
        ["Orders"] = typeof(IOrdersModuleApi).Assembly,
        ["CheckIn"] = typeof(ICheckInModuleApi).Assembly,
        ["Notifications"] = typeof(INotificationsModuleApi).Assembly,
    };

    public static IEnumerable<object[]> Names() => Modules.Select(m => new object[] { m });

    public static IEnumerable<object[]> Pairs() =>
        from a in Modules from b in Modules where a != b select new object[] { a, b };

    /// <summary>
    /// Luật 1: Internal của module A không tham chiếu Internal của module B.
    /// </summary>
    [Theory, MemberData(nameof(Pairs))]
    public void Internal_Does_Not_Reference_Other_Internal(string module, string other)
    {
        var referenced = Internals[module].GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain($"{other}.Internal", referenced);
    }

    /// <summary>
    /// Luật 2: Contracts của A không tham chiếu Contracts của B.
    /// </summary>
    [Theory, MemberData(nameof(Pairs))]
    public void Contracts_Does_Not_Reference_Other_Contracts(string module, string other)
    {
        var referenced = Contracts[module].GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain($"{other}.Contracts", referenced);
    }

    /// <summary>
    /// Luật 3: Thư mục Domain không phụ thuộc Application, Infrastructure, EF Core.
    /// </summary>
    [Theory, MemberData(nameof(Names))]
    public void Domain_Has_No_Outward_Dependencies(string module)
    {
        var types = Types.InAssembly(Internals[module])
            .That().ResideInNamespace($"{module}.Internal.Domain");

        // Nếu namespace chưa có type nào thì bỏ qua (module mới tạo)
        if (!types.GetTypes().Any())
        {
            return;
        }

        var result = types
            .ShouldNot().HaveDependencyOnAny(
                $"{module}.Internal.Application",
                $"{module}.Internal.Infrastructure",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// Luật 4: Internal chỉ lộ ra đúng một class public: XxxModule.
    /// Class Migration do EF sinh ra cũng là public nên được loại trừ.
    /// </summary>
    [Theory, MemberData(nameof(Names))]
    public void Internal_Exposes_Only_Module_Class(string module)
    {
        var leaked = Types.InAssembly(Internals[module])
            .That().ArePublic()
            .And().DoNotHaveName($"{module}Module")
            .And().DoNotResideInNamespaceContaining("Migrations")
            .GetTypes();

        Assert.Empty(leaked);
    }
}
