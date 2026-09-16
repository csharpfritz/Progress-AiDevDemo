using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ProgressHomeHeating.Contracts;
using ProgressHomeHeating.Web.Components.Billing;
using ProgressHomeHeating.Web.Services;
using Xunit;

namespace ProgressHomeHeating.Web.Tests;

public class BillingPanelTests : BunitContext
{
    private FakeBillingApiClient Arrange(Action<FakeBillingApiClient>? configure = null)
    {
        var fake = new FakeBillingApiClient();
        configure?.Invoke(fake);
        Services.AddSingleton<IBillingApiClient>(fake);
        return fake;
    }

    private IRenderedComponent<BillingPanel> RenderPanel() =>
        Render<BillingPanel>(ps => ps.Add(p => p.CustomerId, FakeBillingApiClient.CustomerId));

    // AC-001
    [Fact]
    public void Renders_balance_status_due_date_and_transactions()
    {
        Arrange(f =>
        {
            f.Account = FakeBillingApiClient.NewAccount(12_345);
            f.Transactions =
            [
                FakeBillingApiClient.NewTransaction(12_345, BillingTransactionType.Invoice),
                FakeBillingApiClient.NewTransaction(-5_000, BillingTransactionType.Payment)
            ];
        });

        var cut = RenderPanel();

        Assert.Contains("123.45", cut.Find("[data-testid=billing-balance]").TextContent);
        Assert.Contains("Due soon", cut.Find("[data-testid=billing-status]").TextContent);
        Assert.Contains("April 1, 2026", cut.Find("[data-testid=billing-due-date]").TextContent);
        Assert.Equal(2, cut.FindAll("[data-testid=billing-transaction-row]").Count);
    }

    // AC-002
    [Fact]
    public void Shows_loading_indicator_while_data_is_pending()
    {
        var gate = new TaskCompletionSource();
        var fake = Arrange(f => f.LoadGate = gate);

        var cut = RenderPanel();

        Assert.NotNull(cut.Find("[data-testid=billing-loading]"));
        Assert.Empty(cut.FindAll("[data-testid=billing-summary]"));

        gate.SetResult();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=billing-summary]")));
        Assert.Empty(cut.FindAll("[data-testid=billing-loading]"));
        Assert.True(fake.AccountLoadCount > 0);
    }

    // AC-003
    [Fact]
    public void Shows_empty_state_when_there_is_no_history()
    {
        Arrange(f =>
        {
            f.Account = FakeBillingApiClient.NewAccount(0);
            f.Transactions = [];
        });

        var cut = RenderPanel();

        Assert.NotNull(cut.Find("[data-testid=billing-empty-history]"));
        Assert.Empty(cut.FindAll("[data-testid=billing-transactions-table]"));
    }

    // AC-004
    [Fact]
    public void Shows_service_unavailable_message_when_billing_is_down()
    {
        Arrange(f => f.ThrowUnavailable = true);

        var cut = RenderPanel();

        Assert.NotNull(cut.Find("[data-testid=billing-unavailable]"));
        Assert.Empty(cut.FindAll("[data-testid=billing-summary]"));
    }

    // AC-005 + AC-008
    [Fact]
    public void Successful_payment_shows_confirmation_and_refreshes_balance()
    {
        var fake = Arrange(f =>
        {
            f.Account = FakeBillingApiClient.NewAccount(5_000);
            f.Transactions = [FakeBillingApiClient.NewTransaction(5_000, BillingTransactionType.Invoice)];
        });

        var cut = RenderPanel();

        fake.PaymentResult = new PaymentSubmissionResult(
            FakeBillingApiClient.NewIntent(PaymentIntentStatus.Succeeded), null);
        fake.Account = FakeBillingApiClient.NewAccount(2_500);
        fake.Transactions =
        [
            FakeBillingApiClient.NewTransaction(-2_500, BillingTransactionType.Payment),
            FakeBillingApiClient.NewTransaction(5_000, BillingTransactionType.Invoice)
        ];

        cut.Find("[data-testid=billing-pay-button]").Click();

        cut.WaitForAssertion(() =>
        {
            var success = cut.Find("[data-testid=billing-payment-success]").TextContent;
            Assert.Contains("pi_", success);
            Assert.Contains("succeeded", success, StringComparison.OrdinalIgnoreCase);
        });

        Assert.Contains("25.00", cut.Find("[data-testid=billing-balance]").TextContent);
        Assert.Equal(2, cut.FindAll("[data-testid=billing-transaction-row]").Count);
        Assert.Single(fake.IdempotencyKeys);
    }

    // AC-006
    [Fact]
    public void Validation_error_from_the_api_is_shown_and_balance_is_unchanged()
    {
        var fake = Arrange(f => f.Account = FakeBillingApiClient.NewAccount(5_000));
        fake.PaymentResult = new PaymentSubmissionResult(null,
            new BillingErrorDto(BillingErrorCodes.AmountExceedsBalance,
                "Payment amount cannot exceed the outstanding balance.", "amountCents"));

        var cut = RenderPanel();
        cut.Find("[data-testid=billing-pay-button]").Click();

        cut.WaitForAssertion(() => Assert.Contains("cannot exceed",
            cut.Find("[data-testid=billing-validation-error]").TextContent));
        Assert.Contains("50.00", cut.Find("[data-testid=billing-balance]").TextContent);
        Assert.Empty(cut.FindAll("[data-testid=billing-payment-success]"));
    }

    // AC-006 — local guard for empty/zero input
    [Fact]
    public void Empty_amount_is_rejected_without_calling_the_api()
    {
        var fake = Arrange(f => f.Account = FakeBillingApiClient.NewAccount(5_000));

        var cut = RenderPanel();
        cut.Find("[data-testid=billing-amount-input]").Input(string.Empty);
        cut.Find("[data-testid=billing-pay-button]").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=billing-validation-error]")));
        Assert.Empty(fake.IdempotencyKeys);
    }

    // AC-007
    [Fact]
    public void Simulated_decline_shows_failure_message_and_leaves_state_intact()
    {
        var fake = Arrange(f => f.Account = FakeBillingApiClient.NewAccount(5_000));
        fake.PaymentResult = new PaymentSubmissionResult(
            FakeBillingApiClient.NewIntent(PaymentIntentStatus.Failed, BillingErrorCodes.CardDeclined), null);

        var cut = RenderPanel();
        cut.Find("[data-testid=billing-pay-button]").Click();

        cut.WaitForAssertion(() =>
        {
            var failure = cut.Find("[data-testid=billing-payment-failure]").TextContent;
            Assert.Contains(BillingErrorCodes.CardDeclined, failure);
            Assert.Contains("balance is unchanged", failure);
        });

        Assert.Contains("50.00", cut.Find("[data-testid=billing-balance]").TextContent);
        Assert.Empty(cut.FindAll("[data-testid=billing-payment-success]"));
    }
}
