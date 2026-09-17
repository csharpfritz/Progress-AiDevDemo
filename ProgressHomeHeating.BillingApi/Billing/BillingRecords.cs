using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.BillingApi.Billing;

public sealed class BillingAccountRecord
{
    public required string Id { get; init; }
    public required Guid CustomerId { get; init; }
    public long BalanceCents { get; set; }
    public DateOnly? DueDate { get; set; }
    public List<BillingTransactionRecord> Transactions { get; } = [];
    public Dictionary<string, IdempotencyRecord> IdempotencyKeys { get; } = [];
    public object Gate { get; } = new();
}

// Captures the original request payload alongside the resulting intent id so a replayed
// Idempotency-Key can be validated against the same parameters (amount/currency/method)
// rather than blindly returning whatever intent was recorded first under that key.
public sealed record IdempotencyRecord(
    string IntentId,
    long AmountCents,
    string Currency,
    string PaymentMethod);

public sealed class BillingTransactionRecord
{
    public required string Id { get; init; }
    public required BillingTransactionType Type { get; init; }
    public required string Description { get; init; }
    public required long AmountCents { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Status { get; init; }
}

public sealed class PaymentIntentRecord
{
    public required string Id { get; init; }
    public required Guid CustomerId { get; init; }
    public required long AmountCents { get; init; }
    public required PaymentIntentStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required long BalanceAfterCents { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureMessage { get; init; }
}

// Intent is null only when Mismatched is true (the key was reused with different parameters).
public sealed record PaymentApplication(PaymentIntentRecord? Intent, bool Replayed, bool Mismatched)
{
    public static PaymentApplication Succeeded(PaymentIntentRecord intent, bool replayed) =>
        new(intent, replayed, Mismatched: false);

    public static readonly PaymentApplication KeyMismatch = new(null, Replayed: false, Mismatched: true);
}

public sealed record PaymentOutcome(bool Succeeded, string? FailureCode, string? FailureMessage);

public enum PaymentReplayKind
{
    NotFound,
    Matched,
    Mismatched
}

// Result of looking up an Idempotency-Key before validation/application: NotFound means the key
// hasn't been seen for this account, Matched means it was seen with the same request parameters
// (safe to replay), and Mismatched means it was seen with different parameters (must be rejected).
public sealed record PaymentReplayResult(PaymentReplayKind Kind, PaymentIntentRecord? Intent)
{
    public static readonly PaymentReplayResult NotFound = new(PaymentReplayKind.NotFound, null);
    public static readonly PaymentReplayResult Mismatched = new(PaymentReplayKind.Mismatched, null);

    public static PaymentReplayResult Matched(PaymentIntentRecord intent) =>
        new(PaymentReplayKind.Matched, intent);
}
