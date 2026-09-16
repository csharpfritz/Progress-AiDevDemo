using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.BillingApi.Billing;

public sealed class BillingAccountRecord
{
    public required string Id { get; init; }
    public required Guid CustomerId { get; init; }
    public long BalanceCents { get; set; }
    public DateOnly? DueDate { get; set; }
    public List<BillingTransactionRecord> Transactions { get; } = [];
    public Dictionary<string, string> IdempotencyKeys { get; } = [];
    public object Gate { get; } = new();
}

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

public sealed record PaymentApplication(PaymentIntentRecord Intent, bool Replayed);

public sealed record PaymentOutcome(bool Succeeded, string? FailureCode, string? FailureMessage);
