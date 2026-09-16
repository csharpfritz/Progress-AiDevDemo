using ProgressHomeHeating.BillingApi.Billing;
using ProgressHomeHeating.Contracts;
using Xunit;

namespace ProgressHomeHeating.BillingApi.Tests;

public class DeterministicSeedingTests
{
    private static readonly DateOnly Anchor = new(2026, 3, 1);

    [Fact]
    public void Seeding_the_same_customer_twice_produces_identical_data()
    {
        var customerId = TestCustomers.WithHistory();

        var first = DeterministicBillingSeeder.Create(customerId, Anchor);
        var second = DeterministicBillingSeeder.Create(customerId, Anchor);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.BalanceCents, second.BalanceCents);
        Assert.Equal(first.DueDate, second.DueDate);
        Assert.Equal(first.Transactions.Count, second.Transactions.Count);
        Assert.Equal(
            first.Transactions.Select(t => (t.Id, t.AmountCents, t.CreatedAt, t.Description)),
            second.Transactions.Select(t => (t.Id, t.AmountCents, t.CreatedAt, t.Description)));
    }

    [Fact]
    public void Different_customers_produce_different_data()
    {
        var a = DeterministicBillingSeeder.Create(TestCustomers.WithHistory(), Anchor);
        var b = DeterministicBillingSeeder.Create(TestCustomers.WithHistory(skip: 1), Anchor);

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Empty_accounts_have_no_transactions_and_zero_balance()
    {
        var customerId = TestCustomers.WithoutHistory();

        var account = DeterministicBillingSeeder.Create(customerId, Anchor);

        Assert.Empty(account.Transactions);
        Assert.Equal(0, account.BalanceCents);
        Assert.Null(account.DueDate);
    }

    [Fact]
    public void Transactions_are_ordered_newest_first()
    {
        var account = DeterministicBillingSeeder.Create(TestCustomers.WithHistory(), Anchor);

        var dates = account.Transactions.Select(t => t.CreatedAt).ToList();
        Assert.Equal(dates.OrderByDescending(d => d), dates);
    }

    [Fact]
    public void Open_balance_matches_the_single_open_invoice()
    {
        var account = DeterministicBillingSeeder.Create(TestCustomers.WithHistory(), Anchor);

        var open = account.Transactions
            .Where(t => t.Type == BillingTransactionType.Invoice && t.Status == "open")
            .ToList();

        Assert.Single(open);
        Assert.Equal(open[0].AmountCents, account.BalanceCents);
    }
}
