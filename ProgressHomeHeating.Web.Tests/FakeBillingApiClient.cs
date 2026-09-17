using ProgressHomeHeating.Contracts;
using ProgressHomeHeating.Web.Services;

namespace ProgressHomeHeating.Web.Tests;

public sealed class FakeBillingApiClient : IBillingApiClient
{
    public BillingAccountDto Account { get; set; } = NewAccount(50_00);
    public List<BillingTransactionDto> Transactions { get; set; } = [];
    public PaymentSubmissionResult PaymentResult { get; set; } =
        new(NewIntent(PaymentIntentStatus.Succeeded), null);

    public bool ThrowUnavailable { get; set; }
    public TaskCompletionSource? LoadGate { get; set; }
    public List<string> IdempotencyKeys { get; } = [];
    public int AccountLoadCount { get; private set; }

    public static readonly Guid CustomerId = new("11111111-1111-1111-1111-111111111111");

    public async Task<BillingAccountDto> GetAccountAsync(Guid customerId, CancellationToken ct = default)
    {
        if (LoadGate is not null) await LoadGate.Task;
        if (ThrowUnavailable) throw new BillingUnavailableException("down");
        AccountLoadCount++;
        return Account;
    }

    public async Task<IReadOnlyList<BillingTransactionDto>> GetTransactionsAsync(
        Guid customerId, int limit = BillingConstants.DefaultTransactionLimit, CancellationToken ct = default)
    {
        if (LoadGate is not null) await LoadGate.Task;
        if (ThrowUnavailable) throw new BillingUnavailableException("down");
        return Transactions;
    }

    public Task<PaymentSubmissionResult> SubmitPaymentAsync(
        CreatePaymentIntentRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        IdempotencyKeys.Add(idempotencyKey);
        if (ThrowUnavailable) throw new BillingUnavailableException("down");
        return Task.FromResult(PaymentResult);
    }

    public static BillingAccountDto NewAccount(long balanceCents) => new(
        $"acct_{CustomerId:N}", CustomerId, "usd", balanceCents,
        balanceCents > 0 ? BillingAccountStatus.DueSoon : BillingAccountStatus.PaidInFull,
        balanceCents > 0 ? new DateOnly(2026, 4, 1) : null,
        new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero));

    public static BillingTransactionDto NewTransaction(long amountCents, BillingTransactionType type) => new(
        $"txn_{Guid.NewGuid():N}", CustomerId, type, "Heating oil delivery — 150 gal",
        amountCents, "usd", new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
        type == BillingTransactionType.Payment ? "succeeded" : "open");

    public static PaymentIntentDto NewIntent(PaymentIntentStatus status, string? failureCode = null) => new(
        $"pi_{Guid.NewGuid():N}", CustomerId, 2_500, "usd", status,
        new DateTimeOffset(2026, 3, 20, 12, 0, 0, TimeSpan.Zero),
        status == PaymentIntentStatus.Succeeded ? 2_500 : 5_000,
        failureCode,
        failureCode is null ? null : "Your payment method was declined by the issuing bank.");
}
