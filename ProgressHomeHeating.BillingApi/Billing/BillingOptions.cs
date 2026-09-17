namespace ProgressHomeHeating.BillingApi.Billing;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    // Payments whose cent remainder equals this value are deterministically declined.
    public int DeclineCents { get; set; } = 13;

    // Payments whose cent remainder equals this value deterministically fail with a processing error.
    public int ProcessingErrorCents { get; set; } = 42;

    // When set, seeded dates are generated relative to this date instead of today (test determinism).
    public DateOnly? SeedAnchorDate { get; set; }
}
