# Progress Home Heating Oil

A demo distributed application for a residential heating oil delivery company, built with
.NET Aspire, Blazor Server, and an AI dispatch agent backed by Azure OpenAI.

![Dashboard screenshot](docs/dashboard-screenshot.png)

## What's here

| Project | Purpose |
|---|---|
| `ProgressHomeHeating.AppHost` | .NET Aspire app host (file-based `apphost.cs`) — orchestrates Postgres, the APIs, and the web frontend |
| `ProgressHomeHeating.Web` | Blazor Server frontend (Telerik UI for Blazor) — dashboard, customers, scheduler, dispatch console, and agent chat |
| `ProgressHomeHeating.OperationsApi` | Minimal API + EF Core over Postgres — customers, oil tanks, fleet, and delivery orders |
| `ProgressHomeHeating.AgentApi` | Minimal API hosting an `AIAgent` (Microsoft.Agents.AI) backed by Azure OpenAI, with tools for looking up customers/tanks and scheduling deliveries |
| `ProgressHomeHeating.BillingApi` | Minimal API hosting a faux, Stripe-inspired billing provider — deterministic in-memory accounts, invoices, and simulated payments (no real provider, no secrets) |
| `ProgressHomeHeating.Contracts` | Shared DTOs used across the web app and both APIs |
| `ServiceDefaults` | Shared Aspire service defaults (health checks, resilience, OpenTelemetry) |
| `knowledge-base-content` | Markdown policy docs (pricing, safety, service area, cancellation) used as the agent's knowledge base |

## Features

- **Dashboard** — low oil tank levels, fleet summary, and upcoming deliveries
- **Customers** — customer roster and tank details
- **Billing** — per-customer balance, due date, invoice/payment history, and simulated payments;
  amounts ending in `.13` simulate a decline and `.42` a processing error
- **Scheduler** — delivery scheduling
- **Dispatch Console** — day-of dispatch view
- **Agent Chat** — ask the AI dispatch assistant about customers, oil levels, or to schedule a delivery; it can also search the policy knowledge base

## Running locally

Prerequisites:
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`)
- [Aspire CLI](https://aspire.dev)
- Docker (for the Postgres container)

```bash
cd ProgressHomeHeating.AppHost
aspire run
```

This starts Postgres, `operationsapi`, `agentapi`, `billingapi`, and `web`, and opens the Aspire dashboard.
The web frontend is available at the URL shown for the `web` resource (HTTPS, dynamically assigned
by default).

### Running the tests

```bash
dotnet test ProgressHomeHeating.slnx
```

The solution uses xUnit v3 on the Microsoft Testing Platform, which the .NET 10 SDK requires; the
opt-in lives in `global.json`.

> **Note:** solution-level `dotnet test` can intermittently report "Zero tests ran" due to a known
> flakiness in the Testing Platform's multi-project orchestrator. If that happens, run each test
> project directly instead:
> ```bash
> dotnet build ProgressHomeHeating.slnx
> ./ProgressHomeHeating.BillingApi.Tests/bin/Debug/net10.0/ProgressHomeHeating.BillingApi.Tests
> ./ProgressHomeHeating.Web.Tests/bin/Debug/net10.0/ProgressHomeHeating.Web.Tests
> ```

### Agent Chat configuration

The Agent Chat page needs Azure OpenAI credentials to become active. Set them as user secrets (or
Aspire parameters) on the AppHost:

```bash
cd ProgressHomeHeating.AppHost
dotnet user-secrets set Parameters:azure-openai-endpoint "<your-endpoint>"
dotnet user-secrets set Parameters:azure-openai-api-key "<your-api-key>"
dotnet user-secrets set Parameters:azure-openai-deployment-name "<your-deployment-name>"
```

Without these, the rest of the app runs normally and the Agent Chat page shows a "not configured"
notice instead of erroring.
