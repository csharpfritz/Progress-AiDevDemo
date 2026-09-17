using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.Web.Models;

/// <summary>
/// Pure rules that govern what a dispatcher may change on an existing delivery order
/// from the scheduler detail modal.
/// </summary>
public static class OrderEditPolicy
{
    public const string StandardCancelMessage =
        "Cancel this delivery? The order will be kept for reporting with a status of Cancelled.";

    public const string EnRouteCancelMessage =
        "This delivery is already EN ROUTE. Cancelling now will not recall the truck. " +
        "Are you sure you want to cancel it?";

    public const string CancelDialogTitle = "Confirm Cancellation";
    public const string CancelOkText = "Yes, cancel delivery";
    public const string CancelDeclineText = "No, keep delivery";

    public const string AlreadyClosedMessage =
        "This delivery is already Delivered or Cancelled and can no longer be cancelled.";

    /// <summary>
    /// Returns the statuses a dispatcher may select for an order whose stored status is
    /// <paramref name="current"/>. The current status is always included so the editor can
    /// render an unchanged selection.
    /// </summary>
    public static IReadOnlyList<DeliveryStatus> AllowedTransitions(DeliveryStatus current) => current switch
    {
        DeliveryStatus.Requested =>
        [
            DeliveryStatus.Requested,
            DeliveryStatus.Scheduled,
            DeliveryStatus.EnRoute,
            DeliveryStatus.Cancelled
        ],
        DeliveryStatus.Scheduled =>
        [
            DeliveryStatus.Scheduled,
            DeliveryStatus.EnRoute,
            DeliveryStatus.Delivered,
            DeliveryStatus.Cancelled
        ],
        DeliveryStatus.EnRoute =>
        [
            DeliveryStatus.EnRoute,
            DeliveryStatus.Delivered,
            DeliveryStatus.Cancelled
        ],
        DeliveryStatus.Delivered => [DeliveryStatus.Delivered],
        DeliveryStatus.Cancelled => [DeliveryStatus.Cancelled],
        _ => [current]
    };

    /// <summary>True when the order may still be cancelled by a dispatcher.</summary>
    public static bool CanCancel(DeliveryStatus current) =>
        current is not (DeliveryStatus.Delivered or DeliveryStatus.Cancelled);

    /// <summary>True when GallonsDelivered may be edited for the given in-editor status.</summary>
    public static bool IsGallonsDeliveredEditable(DeliveryStatus? selectedStatus) =>
        selectedStatus == DeliveryStatus.Delivered;

    public static string CancelConfirmationMessage(DeliveryStatus current) =>
        current == DeliveryStatus.EnRoute ? EnRouteCancelMessage : StandardCancelMessage;

    /// <summary>
    /// Validates the dispatcher's in-progress edits. Returns <c>null</c> when the edit is
    /// valid, otherwise the single message to render in the modal's error area.
    /// </summary>
    public static string? Validate(OrderDetailEditModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.ScheduledDate is null)
        {
            return "Scheduled date is required.";
        }

        if (model.Status is null)
        {
            return "Status is required.";
        }

        if (!AllowedTransitions(model.OriginalStatus).Contains(model.Status.Value))
        {
            return $"An order with status {model.OriginalStatus} cannot be changed to {model.Status.Value}.";
        }

        if (model.Status.Value == DeliveryStatus.Delivered)
        {
            if (model.GallonsDelivered is null)
            {
                return "Gallons delivered is required when the status is Delivered.";
            }

            if (model.GallonsDelivered.Value < 1)
            {
                return "Gallons delivered must be greater than zero.";
            }
        }

        return null;
    }
}
