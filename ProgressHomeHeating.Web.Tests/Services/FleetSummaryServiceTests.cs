using ProgressHomeHeating.Contracts;
using ProgressHomeHeating.Web.Services;

namespace ProgressHomeHeating.Web.Tests.Services;

public class FleetSummaryServiceTests
{
    private static DeliveryOrderDto MakeOrder(DeliveryStatus status, DateOnly scheduledDate) =>
        new(
            Id: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            CustomerName: "Test Customer",
            TankId: Guid.NewGuid(),
            DriverId: null,
            DriverName: null,
            TruckId: null,
            TruckLabel: null,
            ScheduledDate: scheduledDate,
            Status: status,
            GallonsRequested: 100,
            GallonsDelivered: status == DeliveryStatus.Delivered ? 100 : null);

    [Theory]
    [InlineData(DeliveryStatus.Requested)]
    [InlineData(DeliveryStatus.Scheduled)]
    [InlineData(DeliveryStatus.EnRoute)]
    public void GetUpcomingDeliveries_IncludesActiveStatuses(DeliveryStatus status)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var orders = new List<DeliveryOrderDto> { MakeOrder(status, today) };

        var result = FleetSummaryService.GetUpcomingDeliveries(orders);

        Assert.Single(result);
    }

    [Theory]
    [InlineData(DeliveryStatus.Delivered)]
    [InlineData(DeliveryStatus.Cancelled)]
    public void GetUpcomingDeliveries_ExcludesCompletedOrCancelledOrders(DeliveryStatus status)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        // Even though the scheduled date is still today/future, an order that has
        // already been delivered or cancelled should no longer count as "upcoming".
        var orders = new List<DeliveryOrderDto>
        {
            MakeOrder(status, today),
            MakeOrder(status, today.AddDays(3))
        };

        var result = FleetSummaryService.GetUpcomingDeliveries(orders);

        Assert.Empty(result);
    }

    [Fact]
    public void GetUpcomingDeliveries_ReturnsMixOfActiveOrdersOnlySortedByDate()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var requestedLater = MakeOrder(DeliveryStatus.Requested, today.AddDays(5));
        var scheduledSoon = MakeOrder(DeliveryStatus.Scheduled, today);
        var enRoute = MakeOrder(DeliveryStatus.EnRoute, today.AddDays(1));
        var delivered = MakeOrder(DeliveryStatus.Delivered, today.AddDays(2));
        var cancelled = MakeOrder(DeliveryStatus.Cancelled, today.AddDays(1));

        var orders = new List<DeliveryOrderDto> { requestedLater, scheduledSoon, enRoute, delivered, cancelled };

        var result = FleetSummaryService.GetUpcomingDeliveries(orders);

        Assert.Equal([scheduledSoon, enRoute, requestedLater], result);
    }

    [Fact]
    public void GetUpcomingDeliveries_ReturnsEmptyListForNoOrders()
    {
        var result = FleetSummaryService.GetUpcomingDeliveries([]);

        Assert.Empty(result);
    }
}
