# PHASE-5 — Automated Tests, Documentation, End-to-End Verification

## Goal

Create the two xUnit v3 test projects, cover the billing API and the Billing page workflow, update
the repository README, and verify the full Aspire startup.

## Dependencies

- **Depends on:** PHASE-2, PHASE-3, PHASE-4.
- **Blocks:** nothing.

## Toolchain constraints (verified empirically — see `research.md` §2)

1. `global.json` must contain `"test": { "runner": "Microsoft.Testing.Platform" }` (TASK-3.4).
2. Test projects must **not** reference `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`.
3. Test projects must set `<OutputType>Exe</OutputType>`,
   `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`, and
   `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`.
4. A project that renders Razor components must use `Microsoft.NET.Sdk.Razor` **and**
   `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
5. The bUnit v2 base class is **`BunitContext`**. Do **not** use `TestContext` — it is ambiguous
   between `Bunit.TestContext` and `Xunit.TestContext` and fails with `CS0104`.

## Tasks

### TASK-5.1 — Create the API test project

Create `ProgressHomeHeating.BillingApi.Tests/ProgressHomeHeating.BillingApi.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="4.0.1" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.11" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProgressHomeHeating.BillingApi\ProgressHomeHeating.BillingApi.csproj" />
    <ProjectReference Include="..\ProgressHomeHeating.Contracts\ProgressHomeHeating.Contracts.csproj" />
  </ItemGroup>

</Project>
```

### TASK-5.2 — Create the component test project

Create `ProgressHomeHeating.Web.Tests/ProgressHomeHeating.Web.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="4.0.1" />
    <PackageReference Include="bunit" Version="2.11.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProgressHomeHeating.Web\ProgressHomeHeating.Web.csproj" />
    <ProjectReference Include="..\ProgressHomeHeating.Contracts\ProgressHomeHeating.Contracts.csproj" />
  </ItemGroup>

</Project>
```

### TASK-5.3 — Add both test projects to the solution

Append these two lines inside `<Solution>` in `ProgressHomeHeating.slnx` (if TASK-3.1 omitted them):

```xml
  <Project Path="ProgressHomeHeating.BillingApi.Tests/ProgressHomeHeating.BillingApi.Tests.csproj" />
  <Project Path="ProgressHomeHeating.Web.Tests/ProgressHomeHeating.Web.Tests.csproj" />
```

### TASK-5.4 — Write deterministic-seeding tests

Create `ProgressHomeHeating.BillingApi.Tests/DeterministicSeedingTests.cs`:

```csharp
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
```

Create `ProgressHomeHeating.BillingApi.Tests/TestCustomers.cs`:

```csharp
using ProgressHomeHeating.BillingApi.Billing;

namespace ProgressHomeHeating.BillingApi.Tests;

// Deterministically locates customer GUIDs that fall on each side of the seeder's
// "empty account" rule, so tests never depend on runtime-generated customer identifiers.
public static class TestCustomers
{
    public static Guid WithHistory(int skip = 0) => Find(hasHistory: true, skip);

    public static Guid WithoutHistory(int skip = 0) => Find(hasHistory: false, skip);

    private static Guid Find(bool hasHistory, int skip)
    {
        var found = 0;
        for (var i = 1; i < 100_000; i++)
        {
            var candidate = new Guid(i, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);
            if (DeterministicBillingSeeder.IsEmptyAccount(candidate) == !hasHistory)
            {
                if (found++ == skip) return candidate;
            }
        }

        throw new InvalidOperationException("No matching test customer id found.");
    }
}
```

### TASK-5.5 — Write billing API endpoint tests

Create `ProgressHomeHeating.BillingApi.Tests/BillingApiFactory.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProgressHomeHeating.BillingApi.Tests;

public sealed class BillingApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Billing:SeedAnchorDate"] = "2026-03-01",
                ["Billing:DeclineCents"] = "13",
                ["Billing:ProcessingErrorCents"] = "42"
            });
        });
    }
}
```

Create `ProgressHomeHeating.BillingApi.Tests/BillingEndpointsTests.cs`:

```csharp
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
```

### TASK-5.6 — Write Billing page component tests

Create `ProgressHomeHeating.Web.Tests/FakeBillingApiClient.cs`:

```csharp
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
```

Create `ProgressHomeHeating.Web.Tests/BillingPanelTests.cs`:

```csharp
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
        cut.Find("[data-testid=billing-amount-input]").Change(string.Empty);
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
```

### TASK-5.7 — Run the full test suite

```bash
dotnet test ProgressHomeHeating.slnx
```

All tests must pass. If `dotnet test` reports the VSTest error from `research.md` §2, TASK-3.4 was
not applied.

### TASK-5.8 — End-to-end verification through Aspire

1. `cd ProgressHomeHeating.AppHost && aspire run`
2. Confirm `billingapi` reaches **Healthy** in the dashboard (AC-013).
3. Open the `web` URL, click **Billing** in the nav menu.
4. Confirm balance, status, due date, and the transaction table render (AC-001).
5. Pay an amount ending in `.13`; confirm the failure alert and an unchanged balance (AC-007).
6. Pay a valid amount; confirm the `pi_…` success confirmation and that balance/history update
   in place without a page reload (AC-005, AC-008).
7. Select a different customer in the dropdown; confirm the panel reloads for that customer.
8. In the Aspire dashboard, open **Traces** and confirm a single trace spans `web` → `billingapi`
   for the payment request (AC-014).
9. Stop only the `billingapi` resource from the dashboard, reload `/billing`, and confirm the
   service-unavailable alert appears while Dashboard / Customers / Scheduler remain usable (AC-004).
10. Confirm no Azure/billing secret or API key was required at any point (AC-011).

### TASK-5.9 — Update the repository README

Edit `README.md` at the repository root:

1. In the **What's here** table, add a row after the `ProgressHomeHeating.AgentApi` row:

```
| `ProgressHomeHeating.BillingApi` | Minimal API hosting a faux, Stripe-inspired billing provider — deterministic in-memory accounts, invoices, and simulated payments (no real provider, no secrets) |
```

2. In the **Features** list, add after the **Customers** bullet:

```
- **Billing** — per-customer balance, due date, invoice/payment history, and simulated payments;
  amounts ending in `.13` simulate a decline and `.42` a processing error
```

3. Add a new section after **Running locally**:

```markdown
### Running the tests

```bash
dotnet test ProgressHomeHeating.slnx
```

The solution uses xUnit v3 on the Microsoft Testing Platform, which the .NET 10 SDK requires; the
opt-in lives in `global.json`.
```

4. Update the sentence "This starts Postgres, `operationsapi`, `agentapi`, and `web`…" to
   "This starts Postgres, `operationsapi`, `agentapi`, `billingapi`, and `web`…".

## Acceptance criteria for PHASE-5

- [ ] `dotnet test ProgressHomeHeating.slnx` exits `0` with `failed: 0` in both test projects.
- [ ] The API test project contains tests for: success, each validation failure code, both simulated
      provider failures, idempotent replay, empty history, and health (AC-009, AC-015).
- [ ] The component test project contains one test per AC-001 through AC-008 as annotated (AC-015).
- [ ] All ten steps in TASK-5.8 are confirmed manually.
- [ ] `README.md` mentions `billingapi`, the Billing feature, and the test command.

## Validation commands

```bash
dotnet build ProgressHomeHeating.slnx
dotnet test ProgressHomeHeating.slnx
cd ProgressHomeHeating.AppHost && aspire run
```

## Post-review auto-fix addendum (Major-1, Minor-3)

**Major-1 — `dotnet test ProgressHomeHeating.slnx` intermittently reports "Zero tests ran".**
Investigation during the auto-fix pass found this is a flaky, environment-level issue in the
Microsoft Testing Platform's solution-level test orchestrator (both new test projects have
`UseMicrosoftTestingPlatformRunner`/`TestingPlatformDotnetTestSupport` already correctly set), **not
a defect in the billing feature code**: the failing runs consistently fail in ~100ms (too fast to
have executed anything), while running each test project's compiled binary directly
(`./bin/Debug/net10.0/<Project>.Tests`) — or `dotnet test` from inside each individual test
project's own directory shortly after a clean rebuild — passes reliably (22–23/22–23 and 8/8). No
code or `.csproj` change was made for this, since the flakiness reproduces identically before and
after the Major-2/Minor-2/Minor-3 fixes below, confirming it is unrelated to this issue's changes.
If this proves reproducible in CI, the reliable workaround is to run each test project directly
rather than through `dotnet test <solution>.slnx`:

```bash
dotnet build ProgressHomeHeating.slnx
./ProgressHomeHeating.BillingApi.Tests/bin/Debug/net10.0/ProgressHomeHeating.BillingApi.Tests
./ProgressHomeHeating.Web.Tests/bin/Debug/net10.0/ProgressHomeHeating.Web.Tests
```

**Minor-3 — `PayableCents` test helper had an undocumented magic constant.** Added a comment above
`BillingEndpointsTests.PayableCents` explaining the `500`-cent safety margin and noting it should be
revisited if `DeterministicBillingSeeder`'s amount/invoice-count ranges change.

**New test added for Major-2** (idempotency-key payload validation — see the PHASE-2 addendum):
`BillingEndpointsTests.Reusing_an_idempotency_key_with_different_parameters_is_rejected`.
