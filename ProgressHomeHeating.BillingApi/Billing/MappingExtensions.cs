using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.BillingApi.Billing;

public static class MappingExtensions
{
    public static BillingAccountDto ToDto(this BillingAccountRecord account, DateOnly today, DateTimeOffset asOf) =>
        new(account.Id,
            account.CustomerId,
            BillingConstants.Currency,
            account.BalanceCents,
            ResolveStatus(account, today),
            account.DueDate,
            asOf);

    public static BillingTransactionDto ToDto(this BillingTransactionRecord transaction, Guid customerId) =>
        new(transaction.Id,
            customerId,
            transaction.Type,
            transaction.Description,
            transaction.AmountCents,
            BillingConstants.Currency,
            transaction.CreatedAt,
            transaction.Status);

    public static PaymentIntentDto ToDto(this PaymentIntentRecord intent) =>
        new(intent.Id,
            intent.CustomerId,
            intent.AmountCents,
            BillingConstants.Currency,
            intent.Status,
            intent.CreatedAt,
            intent.BalanceAfterCents,
            intent.FailureCode,
            intent.FailureMessage);

    private static BillingAccountStatus ResolveStatus(BillingAccountRecord account, DateOnly today)
    {
        if (account.BalanceCents <= 0) return BillingAccountStatus.PaidInFull;
        if (account.DueDate is null) return BillingAccountStatus.Current;
        if (account.DueDate < today) return BillingAccountStatus.PastDue;
        if (account.DueDate.Value.DayNumber - today.DayNumber <= 7) return BillingAccountStatus.DueSoon;
        return BillingAccountStatus.Current;
    }
}
