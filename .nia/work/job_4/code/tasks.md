# Implementation Tasks — Issue #4 (Billing page + faux billing service)

Progress tracking only. Full instructions live in `phase_1.md` … `phase_5.md`.

## Phase dependencies

| Phase | Depends on |
|-------|-----------|
| PHASE-1 | — |
| PHASE-2 | PHASE-1 |
| PHASE-3 | PHASE-2 |
| PHASE-4 | PHASE-1, PHASE-2 |
| PHASE-5 | PHASE-2, PHASE-3, PHASE-4 |

PHASE-3 and PHASE-4 may be executed in parallel.

## PHASE-1 — Shared billing contracts

- [x] **TASK-1.1** Create `ProgressHomeHeating.Contracts/BillingContracts.cs`
- [x] **TASK-1.2** Confirm no `ProgressHomeHeating.Contracts.csproj` change is needed
- [x] **TASK-1.3** Build the contracts project

## PHASE-2 — `ProgressHomeHeating.BillingApi` service

- [x] **TASK-2.1** Create `ProgressHomeHeating.BillingApi.csproj`
- [x] **TASK-2.2** Create `appsettings.json`, `appsettings.Development.json`, `Properties/launchSettings.json`
- [x] **TASK-2.3** Create `Billing/BillingOptions.cs`, `Billing/BillingRecords.cs`, `Billing/BillingTelemetry.cs`
- [x] **TASK-2.4** Create `Billing/DeterministicBillingSeeder.cs`
- [x] **TASK-2.5** Create `Billing/FauxPaymentProcessor.cs`
- [x] **TASK-2.6** Create `Billing/BillingStore.cs` (`IBillingStore` + `InMemoryBillingStore`)
- [x] **TASK-2.7** Create `Billing/MappingExtensions.cs` and `Endpoints/BillingEndpoints.cs`
- [x] **TASK-2.8** Create `Billing/BillingStoreHealthCheck.cs` and `Program.cs`
- [x] **TASK-2.9** Create `ProgressHomeHeating.BillingApi.http`
- [x] **TASK-2.10** Build and smoke-test the service with `curl`

## PHASE-3 — Aspire AppHost and solution wiring

- [x] **TASK-3.1** Add `ProgressHomeHeating.BillingApi` to `ProgressHomeHeating.slnx` (test projects added in TASK-5.3)
- [x] **TASK-3.2** Register `billingapi` in `apphost.cs` and reference it from `web` (no `WaitFor`)
- [x] **TASK-3.3** Verify `aspire run` starts all five resources
- [x] **TASK-3.4** Add the Microsoft Testing Platform opt-in to `global.json`

## PHASE-4 — Web Billing page and typed client

- [x] **TASK-4.1** Create `ProgressHomeHeating.Web/Services/BillingApiClient.cs`
- [x] **TASK-4.2** Register `IBillingApiClient` in `ProgressHomeHeating.Web/Program.cs`
- [x] **TASK-4.3** Create `Components/Billing/BillingPanel.razor`
- [x] **TASK-4.4** Create `Components/Pages/Billing.razor`
- [x] **TASK-4.5** Add the Billing entry to `Components/Layout/NavMenu.razor`
- [x] **TASK-4.6** Build the Web project

## PHASE-5 — Tests, documentation, end-to-end verification

- [x] **TASK-5.1** Create `ProgressHomeHeating.BillingApi.Tests.csproj`
- [x] **TASK-5.2** Create `ProgressHomeHeating.Web.Tests.csproj`
- [x] **TASK-5.3** Add both test projects to `ProgressHomeHeating.slnx`
- [x] **TASK-5.4** Write `TestCustomers.cs` and `DeterministicSeedingTests.cs`
- [x] **TASK-5.5** Write `BillingApiFactory.cs` and `BillingEndpointsTests.cs`
- [x] **TASK-5.6** Write `FakeBillingApiClient.cs` and `BillingPanelTests.cs`
- [x] **TASK-5.7** Run `dotnet test ProgressHomeHeating.slnx`
- [x] **TASK-5.8** End-to-end verification through `aspire run` (10 steps)
- [x] **TASK-5.9** Update the repository `README.md`

## Acceptance-criteria sign-off

- [x] AC-001 Billing page shows balance, status, due date, transactions
- [x] AC-002 Loading indicator while data loads
- [x] AC-003 Explicit empty state for zero transactions
- [x] AC-004 Graceful service-unavailable state
- [x] AC-005 Successful simulated payment with provider-style identifier
- [x] AC-006 Invalid amounts rejected with no state change
- [x] AC-007 Simulated provider failure messaged, state intact
- [x] AC-008 Balance/history refresh in place after payment
- [x] AC-009 Billing endpoints usable independently of Web
- [x] AC-010 Deterministic seed data across restarts
- [x] AC-011 No provider account, key, or external call required
- [x] AC-012 Reached exclusively via Aspire service discovery
- [x] AC-013 ServiceDefaults health check visible in Aspire
- [x] AC-014 Correlated traces/logs across `web` and `billingapi`
- [x] AC-015 xUnit coverage of API + page workflows
- [x] AC-016 Full solution starts via `aspire run` with no extra steps
