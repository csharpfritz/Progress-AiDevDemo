using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.Web.Models;

/// <summary>
/// Mutable edit state for a single delivery order shown in the scheduler detail modal.
/// Built from the loaded <see cref="DeliveryOrderDto"/>; produces a delta-only
/// <see cref="UpdateDeliveryOrderRequest"/> so unchanged fields are never overwritten.
/// </summary>
public sealed class OrderDetailEditModel
{
    public OrderDetailEditModel(DeliveryOrderDto order)
    {
        ArgumentNullException.ThrowIfNull(order);

        OrderId = order.Id;
        OriginalScheduledDate = order.ScheduledDate;
        OriginalStatus = order.Status;
        OriginalDriverId = order.DriverId;
        OriginalTruckId = order.TruckId;
        OriginalGallonsDelivered = order.GallonsDelivered;

        ScheduledDate = order.ScheduledDate.ToDateTime(TimeOnly.MinValue);
        Status = order.Status;
        DriverId = order.DriverId;
        TruckId = order.TruckId;
        GallonsDelivered = order.GallonsDelivered;
    }

    public Guid OrderId { get; }

    public DateOnly OriginalScheduledDate { get; }
    public DeliveryStatus OriginalStatus { get; }
    public Guid? OriginalDriverId { get; }
    public Guid? OriginalTruckId { get; }
    public int? OriginalGallonsDelivered { get; }

    public DateTime? ScheduledDate { get; set; }
    public DeliveryStatus? Status { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? TruckId { get; set; }
    public int? GallonsDelivered { get; set; }

    /// <summary>True when at least one editable field differs from the loaded order.</summary>
    public bool HasChanges
    {
        get
        {
            var request = BuildUpdateRequest();
            return request.ScheduledDate is not null
                || request.Status is not null
                || request.DriverId is not null
                || request.TruckId is not null
                || request.GallonsDelivered is not null;
        }
    }

    /// <summary>
    /// Builds a partial update containing only changed fields. Every unchanged field is
    /// <c>null</c>, which OrderEndpoints.MapPut interprets as "leave unchanged".
    /// Clearing an already-assigned driver or truck is not representable by the current
    /// contract and is therefore treated as "unchanged" (see research.md OQ-3 / FU-01).
    /// </summary>
    public UpdateDeliveryOrderRequest BuildUpdateRequest()
    {
        DateOnly? scheduledDate = null;
        if (ScheduledDate is not null)
        {
            var candidate = DateOnly.FromDateTime(ScheduledDate.Value);
            if (candidate != OriginalScheduledDate)
            {
                scheduledDate = candidate;
            }
        }

        var status = Status is not null && Status.Value != OriginalStatus ? Status : null;

        var driverId = DriverId is not null && DriverId != OriginalDriverId ? DriverId : null;
        var truckId = TruckId is not null && TruckId != OriginalTruckId ? TruckId : null;

        var gallonsDelivered =
            GallonsDelivered is not null && GallonsDelivered != OriginalGallonsDelivered
                ? GallonsDelivered
                : null;

        return new UpdateDeliveryOrderRequest(
            ScheduledDate: scheduledDate,
            Status: status,
            DriverId: driverId,
            TruckId: truckId,
            GallonsDelivered: gallonsDelivered);
    }

    /// <summary>The request used by the "Cancel Delivery" action: status change only.</summary>
    public static UpdateDeliveryOrderRequest BuildCancelRequest() => new(
        ScheduledDate: null,
        Status: DeliveryStatus.Cancelled,
        DriverId: null,
        TruckId: null,
        GallonsDelivered: null);
}
