using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.Web.Services;

/// <summary>
/// Computes the figures shown in the dashboard's "Fleet Summary" box.
/// Extracted from <c>Home.razor</c> so the filtering logic can be unit tested
/// independently of the Blazor component lifecycle.
/// </summary>
public static class FleetSummaryService
{
    /// <summary>
    /// Statuses that still represent work to be done. Orders that have already
    /// been <see cref="DeliveryStatus.Delivered"/> or <see cref="DeliveryStatus.Cancelled"/>
    /// are no longer "upcoming", even if their scheduled date is today or later.
    /// </summary>
    private static readonly HashSet<DeliveryStatus> ActiveStatuses =
    [
        DeliveryStatus.Requested,
        DeliveryStatus.Scheduled,
        DeliveryStatus.EnRoute
    ];

    /// <summary>
    /// Filters a set of orders down to those that are genuinely upcoming: scheduled
    /// today or later and not yet delivered or cancelled.
    /// </summary>
    public static List<DeliveryOrderDto> GetUpcomingDeliveries(IEnumerable<DeliveryOrderDto> orders) =>
        orders
            .Where(o => ActiveStatuses.Contains(o.Status))
            .OrderBy(o => o.ScheduledDate)
            .ToList();
}
