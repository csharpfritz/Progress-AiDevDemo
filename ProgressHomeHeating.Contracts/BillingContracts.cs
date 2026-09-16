namespace ProgressHomeHeating.Contracts;

public enum BillingAccountStatus
{
    PaidInFull,
    Current,
    DueSoon,
    PastDue
}

public enum BillingTransactionType
{
    Invoice,
    Payment,
    Adjustment
}

public enum PaymentIntentStatus
{
    Succeeded,
    Failed
}

public record BillingAccountDto(
    string Id,
    Guid CustomerId,
    string Currency,
    long BalanceCents,
    BillingAccountStatus Status,
    DateOnly? DueDate,
    DateTimeOffset AsOf);

public record BillingTransactionDto(
    string Id,
    Guid CustomerId,
    BillingTransactionType Type,
    string Description,
    long AmountCents,
    string Currency,
    DateTimeOffset CreatedAt,
    string Status);

public record BillingTransactionListDto(
    IReadOnlyList<BillingTransactionDto> Data,
    bool HasMore);

public record CreatePaymentIntentRequest(
    Guid CustomerId,
    long AmountCents,
    string Currency,
    string PaymentMethod);

public record PaymentIntentDto(
    string Id,
    Guid CustomerId,
    long AmountCents,
    string Currency,
    PaymentIntentStatus Status,
    DateTimeOffset CreatedAt,
    long BalanceAfterCents,
    string? FailureCode,
    string? FailureMessage);

public record BillingErrorDto(
    string Code,
    string Message,
    string? Param = null);

public static class BillingErrorCodes
{
    public const string AmountInvalid = "amount_invalid";
    public const string AmountBelowMinimum = "amount_below_minimum";
    public const string AmountExceedsBalance = "amount_exceeds_balance";
    public const string CurrencyUnsupported = "currency_unsupported";
    public const string PaymentMethodUnsupported = "payment_method_unsupported";
    public const string LimitInvalid = "limit_invalid";
    public const string CardDeclined = "card_declined";
    public const string ProcessingError = "processing_error";
}

public static class BillingPaymentMethods
{
    public const string Card = "card";
    public const string BankAccount = "bank_account";

    public static readonly IReadOnlyList<string> All = [Card, BankAccount];
}

public static class BillingConstants
{
    public const string Currency = "usd";
    public const long MinimumPaymentCents = 100;
    public const int DefaultTransactionLimit = 20;
    public const int MaxTransactionLimit = 100;
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string IdempotencyReplayedHeader = "Idempotency-Replayed";
}
