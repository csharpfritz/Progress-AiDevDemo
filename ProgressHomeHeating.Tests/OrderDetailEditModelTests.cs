using ProgressHomeHeating.Contracts;
using ProgressHomeHeating.Web.Models;

namespace ProgressHomeHeating.Tests;

public class OrderDetailEditModelTests
{
    private static readonly Guid DriverA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DriverB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TruckA = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static DeliveryOrderDto Order(
        DeliveryStatus status = DeliveryStatus.Scheduled,
        Guid? driverId = null,
        Guid? truckId = null,
        int? gallonsDelivered = null,
        string? createdBy = null) => new(
            Id: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            CustomerId: Guid.NewGuid(),
            CustomerName: "Test Customer",
            TankId: Guid.NewGuid(),
            DriverId: driverId,
            DriverName: driverId is null ? null : "Driver",
            TruckId: truckId,
            TruckLabel: truckId is null ? null : "Truck",
            ScheduledDate: new DateOnly(2026, 1, 15),
            Status: status,
            GallonsRequested: 150,
            GallonsDelivered: gallonsDelivered,
            CreatedBy: createdBy);

    [Fact]
    public void Constructor_seeds_editable_fields_from_order()
    {
        var model = new OrderDetailEditModel(Order(DeliveryStatus.EnRoute, DriverA, TruckA, 120));

        Assert.Equal(new DateTime(2026, 1, 15), model.ScheduledDate);
        Assert.Equal(DeliveryStatus.EnRoute, model.Status);
        Assert.Equal(DriverA, model.DriverId);
        Assert.Equal(TruckA, model.TruckId);
        Assert.Equal(120, model.GallonsDelivered);
    }

    [Fact]
    public void BuildUpdateRequest_is_all_null_when_nothing_changed()
    {
        var request = new OrderDetailEditModel(Order(DeliveryStatus.Scheduled, DriverA, TruckA, 120))
            .BuildUpdateRequest();

        Assert.Null(request.ScheduledDate);
        Assert.Null(request.Status);
        Assert.Null(request.DriverId);
        Assert.Null(request.TruckId);
        Assert.Null(request.GallonsDelivered);
    }

    [Fact]
    public void HasChanges_is_false_when_nothing_changed() =>
        Assert.False(new OrderDetailEditModel(Order()).HasChanges);

    [Fact]
    public void BuildUpdateRequest_sends_only_the_changed_date()
    {
        var model = new OrderDetailEditModel(Order()) { ScheduledDate = new DateTime(2026, 2, 1) };

        var request = model.BuildUpdateRequest();

        Assert.Equal(new DateOnly(2026, 2, 1), request.ScheduledDate);
        Assert.Null(request.Status);
        Assert.Null(request.DriverId);
        Assert.Null(request.TruckId);
        Assert.Null(request.GallonsDelivered);
        Assert.True(model.HasChanges);
    }

    [Fact]
    public void BuildUpdateRequest_sends_only_the_changed_driver()
    {
        var model = new OrderDetailEditModel(Order(driverId: DriverA)) { DriverId = DriverB };

        var request = model.BuildUpdateRequest();

        Assert.Equal(DriverB, request.DriverId);
        Assert.Null(request.ScheduledDate);
        Assert.Null(request.Status);
        Assert.Null(request.TruckId);
        Assert.Null(request.GallonsDelivered);
    }

    [Fact]
    public void BuildUpdateRequest_ignores_attempt_to_clear_assigned_driver()
    {
        // FU-01: the API contract cannot express "unassign"; a null selection is a no-op.
        var model = new OrderDetailEditModel(Order(driverId: DriverA)) { DriverId = null };

        Assert.Null(model.BuildUpdateRequest().DriverId);
        Assert.False(model.HasChanges);
    }

    [Fact]
    public void BuildUpdateRequest_sends_status_and_gallons_together_when_both_change()
    {
        var model = new OrderDetailEditModel(Order(DeliveryStatus.EnRoute))
        {
            Status = DeliveryStatus.Delivered,
            GallonsDelivered = 175
        };

        var request = model.BuildUpdateRequest();

        Assert.Equal(DeliveryStatus.Delivered, request.Status);
        Assert.Equal(175, request.GallonsDelivered);
        Assert.Null(request.ScheduledDate);
        Assert.Null(request.DriverId);
        Assert.Null(request.TruckId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("dispatch-agent")]
    public void Constructor_and_delta_building_are_agnostic_to_CreatedBy(string? createdBy)
    {
        // VAL-6.6: the AI dispatch agent creates orders with CreatedBy set, but the
        // detail modal's edit model keys purely on order data (Id-based lookup in
        // Scheduler.razor), so agent-created orders must behave identically to
        // manually scheduled ones.
        var model = new OrderDetailEditModel(Order(DeliveryStatus.EnRoute, DriverA, TruckA, 120, createdBy))
        {
            Status = DeliveryStatus.Delivered,
            GallonsDelivered = 200
        };

        var request = model.BuildUpdateRequest();

        Assert.True(model.HasChanges);
        Assert.Equal(DeliveryStatus.Delivered, request.Status);
        Assert.Equal(200, request.GallonsDelivered);
        Assert.Null(request.ScheduledDate);
        Assert.Null(request.DriverId);
        Assert.Null(request.TruckId);
    }

    [Fact]
    public void BuildCancelRequest_sets_only_the_cancelled_status()
    {
        var request = OrderDetailEditModel.BuildCancelRequest();

        Assert.Equal(DeliveryStatus.Cancelled, request.Status);
        Assert.Null(request.ScheduledDate);
        Assert.Null(request.DriverId);
        Assert.Null(request.TruckId);
        Assert.Null(request.GallonsDelivered);
    }
}
