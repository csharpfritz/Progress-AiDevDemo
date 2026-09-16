# PHASE-1 — Shared Billing Contracts

## Goal

Add every billing DTO, enum, and error-code constant to `ProgressHomeHeating.Contracts` so that
`ProgressHomeHeating.BillingApi` and `ProgressHomeHeating.Web` compile against one source of truth.

## Dependencies

- **Depends on:** none. This phase may start immediately.
- **Blocks:** PHASE-2, PHASE-4.

## Tasks

### TASK-1.1 — Create `BillingContracts.cs`

Create the new file `ProgressHomeHeating.Contracts/BillingContracts.cs` with exactly the following content:

```csharp
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
```

### TASK-1.2 — Confirm no project file change is required

Open `ProgressHomeHeating.Contracts/ProgressHomeHeating.Contracts.csproj` and confirm it contains
`<ImplicitUsings>enable</ImplicitUsings>` and `<Nullable>enable</Nullable>`.
Do **not** add any `PackageReference` — `BillingContracts.cs` uses only BCL types.

### TASK-1.3 — Build the contracts project

Run from the repository root:

```bash
dotnet build ProgressHomeHeating.Contracts/ProgressHomeHeating.Contracts.csproj
```

## Acceptance criteria for PHASE-1

- [ ] `ProgressHomeHeating.Contracts/BillingContracts.cs` exists with the content above.
- [ ] `dotnet build ProgressHomeHeating.Contracts/ProgressHomeHeating.Contracts.csproj` exits with code `0` and reports `0 Error(s)`.
- [ ] No other file in the repository was modified by this phase.

## Validation command

```bash
dotnet build ProgressHomeHeating.Contracts/ProgressHomeHeating.Contracts.csproj -warnaserror
```
