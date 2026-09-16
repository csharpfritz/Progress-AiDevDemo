using System.Net;
using System.Net.Http.Json;
using ProgressHomeHeating.Contracts;
using Xunit;

namespace ProgressHomeHeating.BillingApi.Tests;

public class BillingEndpointsTests : IClassFixture<BillingApiFactory>
{
    private readonly BillingApiFactory _factory;

    public BillingEndpointsTests(BillingApiFactory factory) => _factory = factory;

    private static long PayableCents(long balance) =>
        Math.Max(BillingConstants.MinimumPaymentCents, balance - (balance % 100) - 500);

    [Fact]
    public async Task Get_account_returns_balance_status_and_due_date()
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory();

        var account = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");

        Assert.NotNull(account);
        Assert.Equal(customerId, account!.CustomerId);
        Assert.Equal("usd", account.Currency);
        Assert.True(account.BalanceCents > 0);
        Assert.NotNull(account.DueDate);
    }

    [Fact]
    public async Task Get_transactions_returns_seeded_history()
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory();

        var list = await client.GetFromJsonAsync<BillingTransactionListDto>(
            $"/v1/billing_accounts/{customerId}/transactions?limit=50");

        Assert.NotNull(list);
        Assert.NotEmpty(list!.Data);
    }

    [Fact]
    public async Task Get_transactions_returns_empty_list_for_empty_account()
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithoutHistory();

        var list = await client.GetFromJsonAsync<BillingTransactionListDto>(
            $"/v1/billing_accounts/{customerId}/transactions");

        Assert.NotNull(list);
        Assert.Empty(list!.Data);
    }

    [Fact]
    public async Task Get_transactions_rejects_invalid_limit()
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory();

        var response = await client.GetAsync($"/v1/billing_accounts/{customerId}/transactions?limit=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<BillingErrorDto>();
        Assert.Equal(BillingErrorCodes.LimitInvalid, error!.Code);
    }

    [Fact]
    public async Task Successful_payment_reduces_balance_and_records_a_transaction()
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory(skip: 1);

        var before = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");
        var amount = PayableCents(before!.BalanceCents);

        var response = await client.PostAsJsonAsync("/v1/payment_intents",
            new CreatePaymentIntentRequest(customerId, amount, "usd", "card"));
        response.EnsureSuccessStatusCode();
        var intent = await response.Content.ReadFromJsonAsync<PaymentIntentDto>();

        Assert.Equal(PaymentIntentStatus.Succeeded, intent!.Status);
        Assert.StartsWith("pi_", intent.Id);
        Assert.Equal(before.BalanceCents - amount, intent.BalanceAfterCents);

        var after = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");
        Assert.Equal(before.BalanceCents - amount, after!.BalanceCents);

        var history = await client.GetFromJsonAsync<BillingTransactionListDto>(
            $"/v1/billing_accounts/{customerId}/transactions");
        Assert.Contains(history!.Data, t => t.Type == BillingTransactionType.Payment && t.AmountCents == -amount);
    }

    [Theory]
    [InlineData(0, BillingErrorCodes.AmountInvalid)]
    [InlineData(-500, BillingErrorCodes.AmountInvalid)]
    [InlineData(50, BillingErrorCodes.AmountBelowMinimum)]
    [InlineData(long.MaxValue / 4, BillingErrorCodes.AmountExceedsBalance)]
    public async Task Invalid_amounts_are_rejected_without_changing_state(long amountCents, string expectedCode)
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory(skip: 2);

        var before = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");

        var response = await client.PostAsJsonAsync("/v1/payment_intents",
            new CreatePaymentIntentRequest(customerId, amountCents, "usd", "card"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<BillingErrorDto>();
        Assert.Equal(expectedCode, error!.Code);

        var after = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");
        Assert.Equal(before!.BalanceCents, after!.BalanceCents);
    }

    [Theory]
    [InlineData("eur", BillingErrorCodes.CurrencyUnsupported, "card")]
    [InlineData("usd", BillingErrorCodes.PaymentMethodUnsupported, "crypto")]
    public async Task Unsupported_currency_or_method_is_rejected(string currency, string expectedCode, string method)
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory(skip: 3);

        var response = await client.PostAsJsonAsync("/v1/payment_intents",
            new CreatePaymentIntentRequest(customerId, 500, currency, method));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<BillingErrorDto>();
        Assert.Equal(expectedCode, error!.Code);
    }

    [Theory]
    [InlineData(13, BillingErrorCodes.CardDeclined)]
    [InlineData(42, BillingErrorCodes.ProcessingError)]
    public async Task Simulated_provider_failure_leaves_balance_untouched(int centsSuffix, string expectedCode)
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory(skip: 4);

        var before = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");
        var amount = 1000 + centsSuffix;

        var response = await client.PostAsJsonAsync("/v1/payment_intents",
            new CreatePaymentIntentRequest(customerId, amount, "usd", "card"));
        response.EnsureSuccessStatusCode();
        var intent = await response.Content.ReadFromJsonAsync<PaymentIntentDto>();

        Assert.Equal(PaymentIntentStatus.Failed, intent!.Status);
        Assert.Equal(expectedCode, intent.FailureCode);
        Assert.False(string.IsNullOrWhiteSpace(intent.FailureMessage));

        var after = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");
        Assert.Equal(before!.BalanceCents, after!.BalanceCents);

        var history = await client.GetFromJsonAsync<BillingTransactionListDto>(
            $"/v1/billing_accounts/{customerId}/transactions");
        Assert.DoesNotContain(history!.Data, t => t.AmountCents == -amount);
    }

    [Fact]
    public async Task Repeating_an_idempotency_key_does_not_charge_twice()
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory(skip: 5);
        var key = Guid.NewGuid().ToString();

        var before = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");
        var amount = PayableCents(before!.BalanceCents);

        async Task<PaymentIntentDto> PostAsync()
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/v1/payment_intents")
            {
                Content = JsonContent.Create(new CreatePaymentIntentRequest(customerId, amount, "usd", "card"))
            };
            message.Headers.Add(BillingConstants.IdempotencyKeyHeader, key);
            var response = await client.SendAsync(message);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<PaymentIntentDto>())!;
        }

        var first = await PostAsync();
        var second = await PostAsync();

        Assert.Equal(first.Id, second.Id);

        var after = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");
        Assert.Equal(before.BalanceCents - amount, after!.BalanceCents);
    }

    [Fact]
    public async Task Payment_intent_can_be_retrieved_by_id()
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory(skip: 6);

        var created = await (await client.PostAsJsonAsync("/v1/payment_intents",
            new CreatePaymentIntentRequest(customerId, 1013, "usd", "card")))
            .Content.ReadFromJsonAsync<PaymentIntentDto>();

        var fetched = await client.GetFromJsonAsync<PaymentIntentDto>($"/v1/payment_intents/{created!.Id}");

        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.Status, fetched.Status);
    }

    [Fact]
    public async Task Reset_restores_the_deterministic_seed_state()
    {
        var client = _factory.CreateClient();
        var customerId = TestCustomers.WithHistory(skip: 7);

        var before = await client.GetFromJsonAsync<BillingAccountDto>($"/v1/billing_accounts/{customerId}");
        await client.PostAsJsonAsync("/v1/payment_intents",
            new CreatePaymentIntentRequest(customerId, PayableCents(before!.BalanceCents), "usd", "card"));

        var response = await client.PostAsync($"/v1/billing_accounts/{customerId}/reset", content: null);
        response.EnsureSuccessStatusCode();
        var reset = await response.Content.ReadFromJsonAsync<BillingAccountDto>();

        Assert.Equal(before.BalanceCents, reset!.BalanceCents);
        Assert.Equal(before.DueDate, reset.DueDate);
    }

    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
