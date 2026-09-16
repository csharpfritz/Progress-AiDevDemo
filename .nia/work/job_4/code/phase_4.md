# PHASE-4 — Web Billing Page and Typed Client

## Goal

Add a typed, mockable billing client to `ProgressHomeHeating.Web`, a testable `BillingPanel`
component that renders every required state, the routed `/billing` page with a customer selector,
and the navigation entry.

## Dependencies

- **Depends on:** PHASE-1 (contracts), PHASE-2 (endpoint routes and semantics).
- **Blocks:** PHASE-5.
- **Parallel with:** PHASE-3.

## Design notes binding on this phase

- All new markup uses **plain Bootstrap classes only** — no Telerik components — so PHASE-5 bUnit
  tests can render `BillingPanel` without a `TelerikRootComponent` (DEC-008). This matches the
  existing plain-markup blocks in `Components/Pages/Dispatch.razor`.
- Every state-bearing element carries a `data-testid` attribute; PHASE-5 tests select on those
  attributes only, never on CSS classes or text position.
- `BillingPanel` receives `CustomerId` as a parameter and knows nothing about customer selection,
  so replacing the selector with a real authenticated identity later touches only `Billing.razor`.

## Tasks

### TASK-4.1 — Create the typed client

Create `ProgressHomeHeating.Web/Services/BillingApiClient.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.Web.Services;

public sealed class BillingUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);

public sealed record PaymentSubmissionResult(PaymentIntentDto? Intent, BillingErrorDto? Error);

public interface IBillingApiClient
{
    Task<BillingAccountDto> GetAccountAsync(Guid customerId, CancellationToken ct = default);

    Task<IReadOnlyList<BillingTransactionDto>> GetTransactionsAsync(
        Guid customerId, int limit = BillingConstants.DefaultTransactionLimit, CancellationToken ct = default);

    Task<PaymentSubmissionResult> SubmitPaymentAsync(
        CreatePaymentIntentRequest request, string idempotencyKey, CancellationToken ct = default);
}

public sealed class BillingApiClient(HttpClient http) : IBillingApiClient
{
    public async Task<BillingAccountDto> GetAccountAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var account = await http.GetFromJsonAsync<BillingAccountDto>(
                $"/v1/billing_accounts/{customerId}", ct);
            return account ?? throw new BillingUnavailableException("Billing service returned no account.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            throw new BillingUnavailableException("The billing service is unavailable.", ex);
        }
    }

    public async Task<IReadOnlyList<BillingTransactionDto>> GetTransactionsAsync(
        Guid customerId, int limit = BillingConstants.DefaultTransactionLimit, CancellationToken ct = default)
    {
        try
        {
            var list = await http.GetFromJsonAsync<BillingTransactionListDto>(
                $"/v1/billing_accounts/{customerId}/transactions?limit={limit}", ct);
            return list?.Data ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            throw new BillingUnavailableException("The billing service is unavailable.", ex);
        }
    }

    public async Task<PaymentSubmissionResult> SubmitPaymentAsync(
        CreatePaymentIntentRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/v1/payment_intents")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add(BillingConstants.IdempotencyKeyHeader, idempotencyKey);

            using var response = await http.SendAsync(message, ct);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadFromJsonAsync<BillingErrorDto>(ct)
                            ?? new BillingErrorDto(BillingErrorCodes.AmountInvalid, "The payment was rejected.");
                return new PaymentSubmissionResult(null, error);
            }

            response.EnsureSuccessStatusCode();

            var intent = await response.Content.ReadFromJsonAsync<PaymentIntentDto>(ct)
                         ?? throw new BillingUnavailableException("Billing service returned no payment intent.");
            return new PaymentSubmissionResult(intent, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            throw new BillingUnavailableException("The billing service is unavailable.", ex);
        }
    }
}
```

### TASK-4.2 — Register the client in `Program.cs`

Edit `ProgressHomeHeating.Web/Program.cs`. Insert immediately after the existing
`builder.Services.AddHttpClient<AgentApiClient>(…)` block:

```csharp
builder.Services.AddHttpClient<IBillingApiClient, BillingApiClient>(client =>
{
    client.BaseAddress = new Uri("http://billingapi");
});
```

The logical host name `billingapi` must match the Aspire resource name from TASK-3.2 exactly.
No port, no scheme override, no configuration key (AC-012).

### TASK-4.3 — Create the `BillingPanel` component

Create the directory `ProgressHomeHeating.Web/Components/Billing/` and the file
`ProgressHomeHeating.Web/Components/Billing/BillingPanel.razor`:

```razor
@using ProgressHomeHeating.Web.Services
@inject IBillingApiClient Billing

@if (loading)
{
    <div class="surface-card" data-testid="billing-loading">
        <p class="mb-0"><em>Loading billing details...</em></p>
    </div>
}
else if (unavailable)
{
    <div class="alert alert-danger" data-testid="billing-unavailable">
        Billing is temporarily unavailable. Your balance and payment history could not be loaded.
        Please try again in a moment.
        <div class="mt-2">
            <button type="button" class="btn btn-sm btn-outline-danger"
                    data-testid="billing-retry" @onclick="ReloadAsync">Retry</button>
        </div>
    </div>
}
else if (account is not null)
{
    <div class="surface-card mb-4" data-testid="billing-summary">
        <div class="row g-4">
            <div class="col-md-4">
                <div class="text-muted small">Current balance</div>
                <div class="fs-2 fw-semibold" data-testid="billing-balance">@FormatCents(account.BalanceCents)</div>
            </div>
            <div class="col-md-4">
                <div class="text-muted small">Status</div>
                <div class="fs-5" data-testid="billing-status">@StatusText(account.Status)</div>
            </div>
            <div class="col-md-4">
                <div class="text-muted small">Payment due</div>
                <div class="fs-5" data-testid="billing-due-date">
                    @(account.DueDate is null ? "Nothing due" : account.DueDate.Value.ToString("MMMM d, yyyy"))
                </div>
            </div>
        </div>
    </div>

    <div class="surface-card mb-4" data-testid="billing-payment">
        <h4>Make a payment</h4>
        @if (account.BalanceCents <= 0)
        {
            <p class="text-muted mb-0" data-testid="billing-nothing-due">
                Your account is paid in full. There is nothing to pay right now.
            </p>
        }
        else
        {
            <div class="row g-3 align-items-end">
                <div class="col-sm-4">
                    <label class="form-label" for="billing-amount">Amount (USD)</label>
                    <input id="billing-amount" class="form-control" type="number" step="0.01" min="1"
                           data-testid="billing-amount-input"
                           disabled="@submitting" @bind="amount" @bind:event="oninput" />
                </div>
                <div class="col-sm-4">
                    <label class="form-label" for="billing-method">Payment method</label>
                    <select id="billing-method" class="form-select" data-testid="billing-method-select"
                            disabled="@submitting" @bind="paymentMethod">
                        <option value="card">Card</option>
                        <option value="bank_account">Bank account</option>
                    </select>
                </div>
                <div class="col-sm-4">
                    <button type="button" class="btn btn-primary w-100"
                            data-testid="billing-pay-button"
                            disabled="@submitting" @onclick="SubmitPaymentAsync">
                        @(submitting ? "Processing..." : "Pay now")
                    </button>
                </div>
            </div>
            <p class="text-muted small mt-2 mb-0" data-testid="billing-demo-hint">
                Demo tip: an amount ending in .13 simulates a declined payment, and .42 simulates a
                processing error. No real money moves and no card details are collected.
            </p>
        }

        @if (validationMessage is not null)
        {
            <div class="alert alert-warning mt-3 mb-0" data-testid="billing-validation-error">@validationMessage</div>
        }
        @if (failureMessage is not null)
        {
            <div class="alert alert-danger mt-3 mb-0" data-testid="billing-payment-failure">@failureMessage</div>
        }
        @if (successMessage is not null)
        {
            <div class="alert alert-success mt-3 mb-0" data-testid="billing-payment-success">@successMessage</div>
        }
    </div>

    <div class="surface-card" data-testid="billing-history">
        <h4>Transaction history</h4>
        @if (transactions.Count == 0)
        {
            <p class="text-muted mb-0" data-testid="billing-empty-history">
                No billing activity yet. Invoices and payments will appear here once your first
                delivery is billed.
            </p>
        }
        else
        {
            <table class="table table-sm mb-0" data-testid="billing-transactions-table">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Description</th>
                        <th>Type</th>
                        <th>Status</th>
                        <th class="text-end">Amount</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var transaction in transactions)
                    {
                        <tr data-testid="billing-transaction-row">
                            <td>@transaction.CreatedAt.ToLocalTime().ToString("MMM d, yyyy")</td>
                            <td>@transaction.Description</td>
                            <td>@transaction.Type</td>
                            <td>@transaction.Status</td>
                            <td class="text-end">@FormatCents(transaction.AmountCents)</td>
                        </tr>
                    }
                </tbody>
            </table>
        }
    </div>
}

@code {
    [Parameter, EditorRequired] public Guid CustomerId { get; set; }

    private bool loading = true;
    private bool unavailable;
    private bool submitting;
    private BillingAccountDto? account;
    private List<BillingTransactionDto> transactions = [];
    private decimal? amount;
    private string paymentMethod = BillingPaymentMethods.Card;
    private string? validationMessage;
    private string? failureMessage;
    private string? successMessage;
    private Guid loadedCustomerId;

    protected override async Task OnParametersSetAsync()
    {
        if (CustomerId == loadedCustomerId) return;
        loadedCustomerId = CustomerId;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        loading = true;
        unavailable = false;
        validationMessage = null;
        failureMessage = null;
        successMessage = null;
        StateHasChanged();

        try
        {
            account = await Billing.GetAccountAsync(CustomerId);
            transactions = [.. await Billing.GetTransactionsAsync(CustomerId)];
            amount = account.BalanceCents > 0 ? account.BalanceCents / 100m : null;
        }
        catch (BillingUnavailableException)
        {
            unavailable = true;
            account = null;
            transactions = [];
        }
        finally
        {
            loading = false;
            StateHasChanged();
        }
    }

    private async Task SubmitPaymentAsync()
    {
        if (submitting || account is null) return;

        validationMessage = null;
        failureMessage = null;
        successMessage = null;

        if (amount is null)
        {
            validationMessage = "Enter a payment amount.";
            return;
        }

        var cents = (long)Math.Round(amount.Value * 100m, MidpointRounding.AwayFromZero);
        if (cents <= 0)
        {
            validationMessage = "Payment amount must be greater than zero.";
            return;
        }

        submitting = true;
        StateHasChanged();

        try
        {
            var request = new CreatePaymentIntentRequest(
                CustomerId, cents, BillingConstants.Currency, paymentMethod);
            var result = await Billing.SubmitPaymentAsync(request, Guid.NewGuid().ToString());

            if (result.Error is not null)
            {
                validationMessage = result.Error.Message;
            }
            else if (result.Intent is { Status: PaymentIntentStatus.Failed } failed)
            {
                failureMessage = $"Payment {failed.Id} was not completed: {failed.FailureMessage} " +
                                 $"(code: {failed.FailureCode}). Your balance is unchanged.";
            }
            else if (result.Intent is { } succeeded)
            {
                successMessage = $"Payment {succeeded.Id} of {FormatCents(succeeded.AmountCents)} succeeded. " +
                                 $"Status: {succeeded.Status}.";
                account = await Billing.GetAccountAsync(CustomerId);
                transactions = [.. await Billing.GetTransactionsAsync(CustomerId)];
                amount = account.BalanceCents > 0 ? account.BalanceCents / 100m : null;
            }
        }
        catch (BillingUnavailableException)
        {
            failureMessage = "Billing is temporarily unavailable. The payment was not submitted.";
        }
        finally
        {
            submitting = false;
            StateHasChanged();
        }
    }

    private static string FormatCents(long cents) => (cents / 100m).ToString("C");

    private static string StatusText(BillingAccountStatus status) => status switch
    {
        BillingAccountStatus.PaidInFull => "Paid in full",
        BillingAccountStatus.DueSoon => "Due soon",
        BillingAccountStatus.PastDue => "Past due",
        _ => "Current"
    };
}
```

### TASK-4.4 — Create the Billing page

Create `ProgressHomeHeating.Web/Components/Pages/Billing.razor`:

```razor
@page "/billing"
@page "/billing/{CustomerId:guid}"
@rendermode InteractiveServer
@using ProgressHomeHeating.Web.Components.Billing
@inject OperationsApiClient Operations
@inject NavigationManager Navigation

<PageTitle>Billing</PageTitle>

<h1>Billing</h1>

@if (loadingCustomers)
{
    <p data-testid="billing-customers-loading"><em>Loading account...</em></p>
}
else if (customersUnavailable)
{
    <div class="alert alert-danger" data-testid="billing-customers-unavailable">
        The customer directory is unavailable, so billing cannot be shown right now.
    </div>
}
else
{
    <div class="surface-card mb-4">
        <label class="form-label" for="billing-customer">Viewing billing for</label>
        <select id="billing-customer" class="form-select" style="max-width: 24rem;"
                data-testid="billing-customer-select"
                value="@selectedCustomerId" @onchange="OnCustomerChanged">
            @foreach (var customer in customers)
            {
                <option value="@customer.Id">@customer.Name</option>
            }
        </select>
    </div>

    @if (selectedCustomerId is { } id)
    {
        <BillingPanel CustomerId="@id" />
    }
}

@code {
    [Parameter] public Guid? CustomerId { get; set; }

    private bool loadingCustomers = true;
    private bool customersUnavailable;
    private List<CustomerDto> customers = [];
    private Guid? selectedCustomerId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            customers = [.. (await Operations.GetCustomersAsync()).OrderBy(c => c.Name)];
        }
        catch (Exception)
        {
            customersUnavailable = true;
        }
        finally
        {
            loadingCustomers = false;
        }

        selectedCustomerId = CustomerId is { } routeId && customers.Any(c => c.Id == routeId)
            ? routeId
            : customers.FirstOrDefault()?.Id;
    }

    protected override void OnParametersSet()
    {
        if (CustomerId is { } routeId && customers.Any(c => c.Id == routeId))
        {
            selectedCustomerId = routeId;
        }
    }

    private void OnCustomerChanged(ChangeEventArgs args)
    {
        if (Guid.TryParse(args.Value?.ToString(), out var id))
        {
            selectedCustomerId = id;
            Navigation.NavigateTo($"/billing/{id}");
        }
    }
}
```

### TASK-4.5 — Add the navigation entry

Edit `ProgressHomeHeating.Web/Components/Layout/NavMenu.razor`. Insert the following block
immediately **after** the `customers` nav item and **before** the `scheduler` nav item:

```razor
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="billing">
                <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Billing
            </NavLink>
        </div>
```

If no `bi-list-nested-nav-menu` icon class is defined in the site CSS, the link still renders with
text only; confirm visually and fall back to reusing an existing icon class already present in the
file (for example `bi-house-door-fill-nav-menu`) rather than adding new CSS.

### TASK-4.6 — Build the Web project

```bash
dotnet build ProgressHomeHeating.Web/ProgressHomeHeating.Web.csproj
```

## Acceptance criteria for PHASE-4

- [ ] `dotnet build ProgressHomeHeating.Web/ProgressHomeHeating.Web.csproj` exits `0` with `0 Error(s)`.
- [ ] `BillingPanel.razor` contains all of these `data-testid` values: `billing-loading`,
      `billing-unavailable`, `billing-summary`, `billing-balance`, `billing-status`,
      `billing-due-date`, `billing-amount-input`, `billing-pay-button`,
      `billing-validation-error`, `billing-payment-failure`, `billing-payment-success`,
      `billing-empty-history`, `billing-transactions-table`.
- [ ] `grep -R "Telerik" ProgressHomeHeating.Web/Components/Billing ProgressHomeHeating.Web/Components/Pages/Billing.razor` returns no matches (DEC-008).
- [ ] The Pay button is bound to `disabled="@submitting"` (DEC-004).
- [ ] `SubmitPaymentAsync` passes a fresh `Guid.NewGuid().ToString()` idempotency key per click.
- [ ] Navigating to `/billing` in a running app shows a customer selector and billing data;
      `/billing/{guid}` deep-links to that customer.

## Validation commands

```bash
dotnet build ProgressHomeHeating.Web/ProgressHomeHeating.Web.csproj
grep -c "data-testid" ProgressHomeHeating.Web/Components/Billing/BillingPanel.razor
```
