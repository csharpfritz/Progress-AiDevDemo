# Issue #4: Add customer billing page and Aspire-orchestrated faux billing service

## Summary

Give customers a credible self-service billing experience in the home-heating portal: a Billing page showing balance, due date, and transaction history, backed by a new standalone faux billing microservice with a Stripe-inspired API. The service must be wired into the existing Aspire AppHost with service discovery, health checks, and observability, and must use deterministic seeded data — no real payment provider, card data, or money movement.

## Background and context

Progress.DevAiDemos is an Aspire-orchestrated .NET solution simulating a home-heating-oil operator. Today the solution consists of:

- **ProgressHomeHeating.Web** – Blazor front end (staff/ops-style UI) with existing pages for Dashboard, Customers, Scheduler, Agent Chat, and Dispatch Console, navigated via `NavMenu.razor`. It talks to backend services through typed clients (`OperationsApiClient`, `AgentApiClient`) in `Services/`.
- **ProgressHomeHeating.OperationsApi** – REST API + EF Core/PostgreSQL persistence for customers, tanks, fleet, and delivery orders (`Endpoints/`, `Data/`, seeded via `DbSeeder`).
- **ProgressHomeHeating.AgentApi** – Microsoft Agent Framework-based conversational agent service.
- **ProgressHomeHeating.Contracts** – Shared DTOs referenced by Web and the APIs (`CustomerContracts.cs`, `TankContracts.cs`, `FleetContracts.cs`, `DeliveryOrderContracts.cs`, `AgentContracts.cs`).
- **ServiceDefaults** – Shared Aspire service-defaults library (health checks, resilience, OpenTelemetry logging/metrics/tracing) applied via `builder.AddServiceDefaults()` / `app.MapDefaultEndpoints()`.
- **ProgressHomeHeating.AppHost** – Aspire AppHost (`apphost.cs`) that provisions PostgreSQL, registers `operationsapi` and `agentapi` as Aspire resources, and wires `web` to both via `WithReference` / `WaitFor` for service discovery.

There is currently **no billing capability** and **no customer-facing authentication/session model** — the Web app is an internal, unauthenticated staff console that lists and manages all customer records rather than a customer self-service login experience. This gap must be resolved (see Open Questions) before the "customer-facing" framing of this issue can be implemented as literally described.

## Impacted areas (monolith with multiple services)

This repository is a single solution composed of several independently deployable services orchestrated by Aspire. This issue impacts:

- **New service:** a billing microservice (naming TBD by architecture, e.g. `ProgressHomeHeating.BillingApi`) — new project, new data store/seed data, new REST endpoints.
- **ProgressHomeHeating.Contracts** — new shared DTOs for billing (account balance, invoice/transaction, payment request/response) so Web and the billing service share a contract, consistent with existing patterns (`CustomerContracts.cs`, etc.).
- **ProgressHomeHeating.Web** — new Billing page/route, navigation entry in `NavMenu.razor`, and a new typed client under `Services/` (parallel to `OperationsApiClient`) to call the billing service.
- **ProgressHomeHeating.AppHost** — new resource registration in `apphost.cs` (and any new data store, e.g. Postgres database or volume) plus `WithReference`/`WaitFor` wiring from `web` to the new billing resource, matching the existing `operationsapi`/`agentapi` pattern.
- **ServiceDefaults** — the new service must consume the existing shared library for health checks, resilience, logging, metrics, and tracing rather than introducing a new cross-cutting mechanism.
- **Cross-service contract:** the billing service's API is a new internal contract consumed only by `Web` initially; it must be designed so it does not couple to `OperationsApi`'s persistence, though it may need to resolve a customer identity (see Open Questions on how "the current customer" is determined).

## User story

As a customer, I want to review my account balance and billing history and make a simulated payment so that I can understand and manage what I owe without contacting support.

## Acceptance criteria

| ID | Criterion | Test approach |
|----|-----------|---------------|
| AC-001 | Navigating to the Billing page (via nav menu or direct route) for a customer with seed data displays current balance, payment status, due date, and a list of recent invoices/transactions. | Automated UI/integration test asserts balance, due date, status, and transaction list render for a known seeded account. |
| AC-002 | While billing data is loading, the page shows a loading indicator instead of blank content or a stale prior state. | Component test simulates a delayed API response and asserts loading UI is shown before data appears. |
| AC-003 | If a customer has no transaction history, the page shows an explicit empty state rather than an empty table or error. | Component test with a seeded account that has zero transactions asserts empty-state messaging. |
| AC-004 | If the billing service is unreachable or returns an error, the page shows a clear service-unavailable message and does not crash or show partial/incorrect data. | Integration test stops/mocks the billing service as unavailable and asserts a graceful error state is rendered. |
| AC-005 | A customer can submit a simulated payment (e.g., amount, method) from the Billing page and receive a clear success confirmation, including a provider-style payment identifier and status. | Automated test submits a valid payment and asserts a success confirmation with an identifier/status is shown. |
| AC-006 | Submitting a payment with an invalid amount (e.g., zero, negative, exceeding balance if disallowed, non-numeric) is rejected with a clear validation message and no state change occurs. | Automated test(s) submit each invalid-amount case and assert a validation error is shown and balance/history remain unchanged. |
| AC-007 | A simulated provider failure during payment submission (e.g., simulated decline) results in a clear failure message to the customer and does not corrupt balance or transaction history. | Automated test triggers a simulated-failure code path and asserts failure messaging plus unchanged, consistent billing state. |
| AC-008 | After a successful simulated payment, the displayed balance and transaction history update to reflect the payment without requiring a full application/browser restart. | Automated test performs a successful payment and asserts the UI balance/history refresh in place. |
| AC-009 | The billing service exposes endpoints to retrieve billing details (balance/status/due date), list transactions, and submit a payment, independent of the Web project. | API-level automated tests call each billing endpoint directly and assert expected responses/status codes. |
| AC-010 | The billing service persists or seeds deterministic demo data on startup (e.g., in Development) so the same account/customer always yields the same initial balance and transaction history across runs. | Test restarts the service (or reseeds) and asserts identical seeded output for a known account. |
| AC-011 | The billing service requires no real payment-provider account, API key, or external network call to function; running fully offline via the standard Aspire startup produces working billing behavior. | Manual/automated verification that the service starts and serves requests with no external billing-provider dependency configured. |
| AC-012 | The billing service is registered as an Aspire resource in the AppHost and is reachable by the Web app exclusively via Aspire service discovery (no hard-coded base URL/port). | Code/config review plus integration test confirms Web resolves the billing service through the Aspire-injected configuration. |
| AC-013 | The billing service applies the shared ServiceDefaults for health checks; its readiness is visible in the Aspire dashboard/`aspire describe`/`aspire ps` output. | Manual verification via Aspire dashboard, or automated check against the service's health endpoint. |
| AC-014 | Billing-related requests emit correlated distributed traces and structured logs spanning the Web app and the billing service, consistent with existing services. | Manual/automated verification that a single billing request produces a linked trace across both services in the OTel/dashboard output. |
| AC-015 | Automated tests exist covering the billing service's core API behavior (success, validation failure, simulated provider failure) and the primary Billing page workflow (view + pay) end to end. | CI run of the added xUnit test suite(s) passes and covers the listed scenarios. |
| AC-016 | The full solution (including the new billing service) builds and starts successfully through the existing documented Aspire command (e.g., `aspire run`/AppHost startup) with no manual extra steps. | Manual/CI verification: run the documented startup command and confirm all resources, including billing, reach a healthy state. |

## Technical considerations and dependencies

- **Contract-first design:** Define billing DTOs (account/balance, invoice/transaction, payment request/response, error/status codes) in `ProgressHomeHeating.Contracts` so both the new service and `ProgressHomeHeating.Web` share one source of truth, consistent with existing contract usage.
- **Provider-neutral API shape:** The Stripe-inspired API (retrieve billing details, list transactions, submit payment) should model concepts (e.g., payment intents, transaction/charge identifiers, statuses such as succeeded/failed/requires-action) generically enough that a real provider integration could later replace the faux implementation without changing the Web-facing contract.
- **Aspire resource wiring:** Follow the existing `apphost.cs` pattern (`builder.AddDotnetProject(...)`, `.WithReference(...)`, `.WaitFor(...)`) to register the new service and connect it from `web`; if a database is needed, follow the `postgres.AddDatabase(...)` pattern already used for `operationsdb`.
- **ServiceDefaults reuse:** New service must call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` exactly like `OperationsApi`/`AgentApi` to inherit health checks, resilience, logging, metrics, and tracing — no bespoke observability setup.
- **Data seeding/determinism:** Reuse the `DbSeeder`-style pattern (seed on startup in Development) if persistence is added, or an equivalent deterministic in-memory/seed approach, so demo runs are repeatable and reviewable.
- **Customer identity resolution:** The Web app has no authentication/session concept today; billing must have a defined way to determine "which customer" is viewing the page (e.g., existing customer selection/context used elsewhere in the app). This should be resolved during refinement, not invented ad hoc during implementation (see Open Questions).
- **Client integration in Web:** Add a typed client under `ProgressHomeHeating.Web/Services/` mirroring `OperationsApiClient`/`AgentApiClient` conventions for calling the billing service, and a new routed Blazor page plus a `NavMenu.razor` entry.
- **Testing framework:** Use xUnit, consistent with the existing stack, for both billing-service API tests and Web page/component tests.
- **No production billing concerns in scope:** No PCI compliance, tax handling, real refunds/disputes, or subscription billing — these are explicitly out of scope and should not influence design complexity.

## Edge cases and risks

- **Ambiguous "customer-facing" framing vs. current app shape:** The Web app today is an internal, unauthenticated multi-customer console, not a per-customer self-service portal. Without a defined identity/session model, "as a customer, I want to review my balance" cannot be implemented as literally a logged-in customer experience. Risk: scope creep into building authentication, or an incorrect assumption baked into the UI (e.g., picking a customer from a list, which contradicts the "self-service" framing).
- **Non-idempotent or duplicate payment submission:** A user double-clicking "Pay" or retrying after a network blip could submit two payments against the same balance. Behavior for duplicate/concurrent submissions is undefined and should be clarified.
- **Concurrent balance updates:** If billing data is fetched/paid concurrently (e.g., two browser tabs, or a background refresh mid-payment), the displayed balance could go stale or the payment could be applied against outdated state.
- **Simulated failure realism vs. determinism:** "Simulated provider failures" must remain deterministic enough to test reliably (e.g., a specific amount or flag triggers failure) while still feeling realistic in the UI; the trigger mechanism needs to be defined.
- **Negative/zero/overpayment amounts:** Validation rules for minimum/maximum payment amounts, overpayment (paying more than the balance), and partial payments are not specified and need explicit rules.
- **Data reset semantics:** Because data is seeded/deterministic, repeated demo runs or app restarts must not leave the billing service in an inconsistent state relative to seeded expectations (e.g., a paid invoice reappearing as unpaid, or vice versa) — reseed/reset behavior needs definition.
- **Service unavailability cascades:** If the billing service is down, the rest of the app (Dashboard, Customers, Scheduler) must remain usable; the Billing page failure must be isolated and not block Web startup, consistent with `WaitFor` semantics already used for other services.
- **Currency/formatting consistency:** Amounts, due dates, and statuses must be formatted consistently with any existing conventions in the app (if any) to avoid a visually inconsistent experience.

## Non-functional requirements (NFR)

- **Reliability/Resilience:** Billing service calls from Web must apply the same resilience patterns (retry/circuit breaker/timeout) provided by ServiceDefaults as other service-to-service calls in the solution; transient billing-service errors should not crash the Web app.
- **Observability:** Every billing request (view balance/history, submit payment) must produce structured logs and a distributed trace correlated across Web and the billing service, visible in the Aspire dashboard, matching the observability bar set by existing services.
- **Health/readiness:** The billing service must expose a health endpoint via ServiceDefaults and report accurate readiness/liveness status so Aspire orchestration (`WaitFor`) and the dashboard reflect its real state.
- **Determinism/repeatability:** Given the same seed configuration, billing data and simulated payment outcomes must be reproducible across separate application runs, to support reliable demos and automated tests.
- **Data integrity:** No sequence of valid or invalid payment attempts (including simulated failures) may leave balance or transaction history in an inconsistent or corrupted state.
- **No external dependency / no secrets:** The service must operate with zero required external network calls, API keys, or credentials — consistent with "faux" service intent and the project's local/demo orientation.
- **Testability:** Core billing behaviors (success, validation failure, simulated provider failure, refresh-after-payment) must be covered by automated xUnit tests runnable in CI without manual setup.
- **Startup performance:** Adding the billing service and its seeding step should not introduce a materially slower Aspire startup time relative to the existing `operationsapi`/`agentapi` startup experience.

## Out of scope

- Real billing service integration or processing real money.
- Collection or storage of real payment-card details.
- Production-grade accounting, tax, refunds, disputes, subscriptions, or PCI compliance.

## Open questions (should be resolved before implementation)

1. How is "the current customer" identified on the Billing page given there is no authentication/session model in the Web app today — is a lightweight customer-selection/context mechanism (reusing something from the Customers page) acceptable, or is a login/session concept expected as a prerequisite?
2. Should duplicate/rapid-repeat payment submissions be prevented (e.g., disable the pay action while a request is in flight), or is this left to the UI layer to judge and out of scope for the API contract?
3. Are there defined validation rules for payment amount bounds (minimum, maximum, overpayment allowed or blocked, partial payments)?
4. What determines a "simulated provider failure" — a specific test amount/flag, a random failure rate, or a configurable trigger — and should that mechanism itself be part of acceptance criteria?
5. Does the billing service need its own persistent store (e.g., a new Postgres database via Aspire, matching `operationsdb`), or is in-memory/seeded-on-start data sufficient for this showcase?
