using ProgressHomeHeating.Contracts;
using ProgressHomeHeating.Web.Models;

namespace ProgressHomeHeating.Tests;

public class OrderEditPolicyTests
{
    private static DeliveryOrderDto Order(
        DeliveryStatus status = DeliveryStatus.Scheduled,
        int? gallonsDelivered = null) => new(
            Id: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            CustomerName: "Test Customer",
            TankId: Guid.NewGuid(),
            DriverId: null,
            DriverName: null,
            TruckId: null,
            TruckLabel: null,
            ScheduledDate: new DateOnly(2026, 1, 15),
            Status: status,
            GallonsRequested: 150,
            GallonsDelivered: gallonsDelivered);

    [Theory]
    [InlineData(DeliveryStatus.Requested, true)]
    [InlineData(DeliveryStatus.Scheduled, true)]
    [InlineData(DeliveryStatus.EnRoute, true)]
    [InlineData(DeliveryStatus.Delivered, false)]
    [InlineData(DeliveryStatus.Cancelled, false)]
    public void CanCancel_matches_AC07(DeliveryStatus status, bool expected) =>
        Assert.Equal(expected, OrderEditPolicy.CanCancel(status));

    [Fact]
    public void AllowedTransitions_always_include_current_status()
    {
        foreach (var status in Enum.GetValues<DeliveryStatus>())
        {
            Assert.Contains(status, OrderEditPolicy.AllowedTransitions(status));
        }
    }

    [Fact]
    public void AllowedTransitions_for_Delivered_is_terminal() =>
        Assert.Equal(
            new[] { DeliveryStatus.Delivered },
            OrderEditPolicy.AllowedTransitions(DeliveryStatus.Delivered));

    [Fact]
    public void AllowedTransitions_for_Cancelled_is_terminal() =>
        Assert.Equal(
            new[] { DeliveryStatus.Cancelled },
            OrderEditPolicy.AllowedTransitions(DeliveryStatus.Cancelled));

    [Fact]
    public void AllowedTransitions_for_EnRoute_excludes_backwards_moves()
    {
        var allowed = OrderEditPolicy.AllowedTransitions(DeliveryStatus.EnRoute);
        Assert.DoesNotContain(DeliveryStatus.Requested, allowed);
        Assert.DoesNotContain(DeliveryStatus.Scheduled, allowed);
    }

    [Fact]
    public void CancelConfirmationMessage_escalates_for_EnRoute() =>
        Assert.Equal(
            OrderEditPolicy.EnRouteCancelMessage,
            OrderEditPolicy.CancelConfirmationMessage(DeliveryStatus.EnRoute));

    [Fact]
    public void CancelConfirmationMessage_is_standard_for_Scheduled() =>
        Assert.Equal(
            OrderEditPolicy.StandardCancelMessage,
            OrderEditPolicy.CancelConfirmationMessage(DeliveryStatus.Scheduled));

    [Theory]
    [InlineData(DeliveryStatus.Requested, false)]
    [InlineData(DeliveryStatus.Scheduled, false)]
    [InlineData(DeliveryStatus.EnRoute, false)]
    [InlineData(DeliveryStatus.Delivered, true)]
    [InlineData(DeliveryStatus.Cancelled, false)]
    public void IsGallonsDeliveredEditable_only_for_Delivered(DeliveryStatus status, bool expected) =>
        Assert.Equal(expected, OrderEditPolicy.IsGallonsDeliveredEditable(status));

    [Fact]
    public void Validate_returns_null_for_untouched_model() =>
        Assert.Null(OrderEditPolicy.Validate(new OrderDetailEditModel(Order())));

    [Fact]
    public void Validate_rejects_missing_scheduled_date()
    {
        var model = new OrderDetailEditModel(Order()) { ScheduledDate = null };
        Assert.Equal("Scheduled date is required.", OrderEditPolicy.Validate(model));
    }

    [Fact]
    public void Validate_rejects_missing_status()
    {
        var model = new OrderDetailEditModel(Order()) { Status = null };
        Assert.Equal("Status is required.", OrderEditPolicy.Validate(model));
    }

    [Fact]
    public void Validate_rejects_disallowed_transition()
    {
        var model = new OrderDetailEditModel(Order(DeliveryStatus.Delivered, 150))
        {
            Status = DeliveryStatus.Requested
        };
        Assert.Equal(
            "An order with status Delivered cannot be changed to Requested.",
            OrderEditPolicy.Validate(model));
    }

    [Fact]
    public void Validate_requires_gallons_delivered_when_marking_delivered()
    {
        var model = new OrderDetailEditModel(Order(DeliveryStatus.Scheduled))
        {
            Status = DeliveryStatus.Delivered,
            GallonsDelivered = null
        };
        Assert.Equal(
            "Gallons delivered is required when the status is Delivered.",
            OrderEditPolicy.Validate(model));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_rejects_non_positive_gallons_delivered(int gallons)
    {
        var model = new OrderDetailEditModel(Order(DeliveryStatus.Scheduled))
        {
            Status = DeliveryStatus.Delivered,
            GallonsDelivered = gallons
        };
        Assert.Equal("Gallons delivered must be greater than zero.", OrderEditPolicy.Validate(model));
    }

    [Fact]
    public void Validate_accepts_positive_gallons_delivered()
    {
        var model = new OrderDetailEditModel(Order(DeliveryStatus.Scheduled))
        {
            Status = DeliveryStatus.Delivered,
            GallonsDelivered = 180
        };
        Assert.Null(OrderEditPolicy.Validate(model));
    }
}
