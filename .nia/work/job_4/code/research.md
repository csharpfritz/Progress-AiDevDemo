# Research Notes — Issue #4

## 1. Codebase findings (verified by reading the repository)

| Finding | Evidence | Consequence for the plan |
|---------|----------|--------------------------|
| AppHost is a **file-based** C# AppHost using `builder.AddDotnetProject(name, relativeCsprojPath)` under `#pragma warning disable ASPIREDOTNETPROJECT001` | `ProgressHomeHeating.AppHost/apphost.cs` | New resource is registered with the identical API; no `.csproj` for the AppHost exists |
| Web resolves services with **scheme-less logical hostnames**: `new Uri("http://operationsapi")` | `ProgressHomeHeating.Web/Program.cs` | Billing client must use `http://billingapi` (satisfies AC-012 — no hard-coded port) |
| `ServiceDefaults.AddServiceDefaults()` registers OTel, service discovery, `AddStandardResilienceHandler()` on **all** HTTP clients, and health checks; `MapDefaultEndpoints()` maps `/health` and `/alive` **only in Development** | `ServiceDefaults/Extensions.cs` | BillingApi gets resilience/tracing/health for free (AC-013, AC-014, NFR resilience); no bespoke setup |
| Existing services keep DTOs in `ProgressHomeHeating.Contracts` as positional `record`s, no `System.ComponentModel.DataAnnotations` attributes | `CustomerContracts.cs`, `DeliveryOrderContracts.cs` | Billing DTOs follow the same style; validation happens in the endpoint layer, matching `OrderEndpoints.cs` |
| Endpoints use minimal APIs grouped by `app.MapGroup("/api/…").WithTags(…)`, returning `Results.Ok/BadRequest/NotFound` | `ProgressHomeHeating.OperationsApi/Endpoints/*.cs` | Billing endpoints reuse the shape, but under `/v1/…` for the Stripe-inspired surface |
| `DbSeeder` seeds **only when the table is empty**, using Bogus **without a fixed seed** | `ProgressHomeHeating.OperationsApi/Data/DbSeeder.cs` | Customer GUIDs are *not* stable across database resets — billing must not hard-code customer IDs (drives DEC-002) |
| Pages already mix Telerik components with plain Bootstrap markup (`alert`, `table table-sm`) | `Components/Pages/Dispatch.razor` | A Telerik-free Billing panel is stylistically consistent (DEC-008) |
| **No test project exists anywhere in the repository**; `.slnx` lists 5 projects | `ProgressHomeHeating.slnx`, file listing | Test infrastructure is greenfield and must be created from scratch (PHASE-5) |
| No CI workflow exists (`.github/` absent) | repository root listing | "CI run" in AC-015 is satisfied by a documented, repeatable `dotnet test` command |
| SDK pinned to `10.0.400`; projects target `net10.0` | `global.json`, `*.csproj` | All new projects target `net10.0` |

## 2. Toolchain research (empirically verified on this machine)

A scratch project was built and executed to de-risk the test stack before planning it.

| Question | Result | Evidence |
|----------|--------|----------|
| Does `dotnet test` work with `Microsoft.NET.Test.Sdk` + VSTest on the .NET 10 SDK? | **No.** Build fails: *"Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later."* | Ran `dotnet test` on a scratch `net10.0` project with `Microsoft.NET.Test.Sdk 18.10.1` |
| Does an INI `dotnet.config` with `[dotnet.test.runner]` opt in? | **No.** Same failure. | Scratch run with `dotnet.config` present |
| What *does* work? | `global.json` containing `"test": { "runner": "Microsoft.Testing.Platform" }`, plus per-project `<OutputType>Exe</OutputType>`, `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`, `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`, and **no** `Microsoft.NET.Test.Sdk` / `xunit.runner.visualstudio` references | Scratch run reported `Test run summary: Passed! total: 2` |
| Do bUnit and xUnit v3 coexist on `net10.0`? | **Yes**, with `bunit 2.11.3` + `xunit.v3 4.0.1`. Note: `TestContext` is **ambiguous** between `Bunit.TestContext` and `Xunit.TestContext` — the bUnit v2 base class to inherit is **`BunitContext`** | Scratch compile error then green run after switching base class |
| Extra requirement for rendering Razor components from a test project | Test project needs `Microsoft.NET.Sdk.Razor` **and** `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, otherwise the component type is not generated | Scratch `CS0246: 'Sample' could not be found` until the framework reference was added |

Pinned package versions (latest stable at planning time, all restore cleanly on `net10.0`):

```yaml
bunit: 2.11.3
xunit.v3: 4.0.1
Microsoft.AspNetCore.Mvc.Testing: 10.0.11   # matches Microsoft.AspNetCore.OpenApi 10.0.11 already used
Bogus: 35.6.5                                # already used by OperationsApi
Microsoft.AspNetCore.OpenApi: 10.0.11        # already used by OperationsApi
```

> **Caveat recorded as ASM-006:** adding `"test"` to the repository `global.json` is a global change.
> It does not affect `dotnet build`, `dotnet run`, or `aspire run`; it only changes how `dotnet test`
> selects a runner. Since no test project exists today, there is no regression risk.

## 3. Resolution of the issue's open questions

### OQ-1 — How is "the current customer" identified? → **DEC-003**

Three options were considered:

| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| A. Build authentication/session (ASP.NET Core Identity or cookie login) | Literally matches the "as a customer" user story | Large scope, touches every page, no requirement in the issue, high risk of derailing the demo | ✗ Rejected — explicit scope-creep risk named in the issue |
| B. Hard-code a single "demo customer" GUID | Trivial | Customer GUIDs are randomly generated by `DbSeeder` and change on volume reset — would break on every fresh database | ✗ Rejected (see §1, non-deterministic GUIDs) |
| C. **Customer selector + route parameter**: `/billing` (defaults to first customer by name) and `/billing/{customerId:guid}` (deep link) | Zero new infrastructure, reuses the existing `OperationsApiClient.GetCustomersAsync()`, deep-linkable, testable, mirrors the app's existing internal-console shape | Not a literal "logged-in customer" experience | ✓ **Selected** |

The UI labels the selector *"Viewing billing for"* so the demo narrative stays honest: the staff console
is impersonating a customer's self-service view. The `BillingPanel` child component receives only a
`CustomerId` parameter, so swapping the selector for a real authenticated identity later is a
one-line change in `Billing.razor`.

### OQ-2 — Duplicate / rapid-repeat payment submissions? → **DEC-004**

Defended at **both** layers, because the UI-only defence is insufficient (a network retry by the
`AddStandardResilienceHandler` retry policy in `ServiceDefaults` can legitimately re-send a POST):

1. **API layer (authoritative):** Stripe-style optional `Idempotency-Key` request header.
   The store keeps `(customerId, idempotencyKey) → paymentIntentId`. A repeat key returns the
   *original* `PaymentIntentDto` unchanged, with response header `Idempotency-Replayed: true`,
   and applies **no** second balance mutation.
2. **UI layer:** the Pay button is disabled while a request is in flight, and one
   `Idempotency-Key` GUID is generated per user click (not per retry).

### OQ-3 — Payment amount validation rules? → **DEC-006**

All amounts are transported as **integer cents** (`long AmountCents`) in a `usd` currency field —
the Stripe convention — which removes floating-point rounding from the contract entirely.

| Rule | Bound | Error code | HTTP |
|------|-------|-----------|------|
| Must be positive | `AmountCents > 0` | `amount_invalid` | 400 |
| Minimum payment | `AmountCents >= 100` ($1.00) | `amount_below_minimum` | 400 |
| Overpayment blocked | `AmountCents <= BalanceCents` | `amount_exceeds_balance` | 400 |
| Zero-balance account | `BalanceCents == 0` → any payment rejected | `amount_exceeds_balance` | 400 |
| Partial payments | allowed (any value in `[100, BalanceCents]`) | — | — |
| Currency | `usd` only (case-insensitive compare) | `currency_unsupported` | 400 |
| Payment method | `card` or `bank_account` only | `payment_method_unsupported` | 400 |
| Unknown account | account is lazily seeded, so never 404 for a valid GUID | — | — |

Non-numeric input never reaches the API: the amount field is `<input type="number" step="0.01">`
bound to a `decimal?`, and the panel rejects `null`/unparsed input locally with the same message text.

### OQ-4 — What triggers a simulated provider failure? → **DEC-005**

| Option | Verdict |
|--------|---------|
| Random failure rate (e.g. 10%) | ✗ Breaks NFR "determinism/repeatability" and makes AC-007 flaky |
| Dedicated `?simulateFailure=true` query flag | ✗ Not credible in a demo; a real provider has no such flag |
| **Magic amounts** (Stripe's own test-card philosophy, applied to amounts) | ✓ **Selected** — deterministic, realistic, self-documenting |

Rules, evaluated **after** validation and **before** any state mutation:

| Condition | Outcome | `failure_code` |
|-----------|---------|----------------|
| `AmountCents % 100 == 13` | `PaymentIntentStatus.Failed` | `card_declined` |
| `AmountCents % 100 == 42` | `PaymentIntentStatus.Failed` | `processing_error` |
| otherwise | `PaymentIntentStatus.Succeeded` | — |

Both trigger values are exposed as configuration (`Billing:DeclineCents`, `Billing:ProcessingErrorCents`)
so tests can pin them, and the Billing page renders a small hint — *"Demo tip: end an amount in .13 to
simulate a declined payment"* — so a reviewer can exercise AC-007 by hand.

**Transport vs. provider outcome:** a simulated decline returns **HTTP 200** with a
`PaymentIntentDto` whose `Status` is `Failed`, not an HTTP error. Rationale: the request itself
succeeded; the *provider outcome* failed. This mirrors Stripe's `payment_intent` object model, keeps
the typed client free of exception-based control flow, and cleanly separates AC-006 (400 validation)
from AC-007 (200 + failed intent). It is documented in the `.http` file and the README.

### OQ-5 — Does billing need its own persistent store? → **DEC-001 / DEC-002**

| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| A. New Postgres database `billingdb` via `postgres.AddDatabase("billingdb")` + EF Core + migrations | Mirrors `operationsdb` exactly; survives restarts | **Breaks AC-010**: with a data volume, a paid invoice stays paid, so a restart does *not* reproduce the seeded state; adds migrations, a `DbContext`, a design-time factory, and startup migration cost (violates the startup-performance NFR); adds a container dependency to every billing test | ✗ Rejected |
| B. **In-memory, lazily and deterministically seeded store** | Restart always reproduces identical seed data (AC-010 literally); zero migrations; no container needed for tests; instant startup; still exercises the full Aspire/service-discovery/OTel story | State is lost on restart — acceptable and in fact *desired* for a repeatable demo | ✓ **Selected** |

The store is a `ConcurrentDictionary<Guid, BillingAccountRecord>` with a per-account `lock`
protecting read-modify-write of balance + history, which closes the "concurrent balance updates"
risk named in the issue. A `POST /v1/billing_accounts/{customerId}/reset` endpoint re-seeds one
account on demand, resolving the "data reset semantics" risk and mirroring the existing
`DELETE /api/orders/agent-runs` demo-reset precedent.

**Determinism mechanism (DEC-002).** Because `DbSeeder` generates customer GUIDs randomly, billing
cannot ship a fixed table of accounts. Instead it *derives* the account from the customer GUID:

```
seed      = FNV-style 32-bit hash of customerId.ToByteArray(), masked to a non-negative int
randomizer= new Bogus.Randomizer(seed)          // Bogus is already a dependency of the solution
```

Everything — balance, due date offset, the number of invoices, their amounts and descriptions — is
drawn from that single seeded `Randomizer`. The same customer therefore always yields the same
billing history, on every machine and every run, with **no** cross-service data synchronisation.

**Date anchoring (ASM-004).** Seeded dates are computed relative to an *anchor date*, not
`DateTime.Now` sprinkled through the code. The anchor is `Billing:SeedAnchorDate` when configured,
otherwise today's UTC date. Determinism therefore holds for the pair `(customerId, anchorDate)`;
tests pin `SeedAnchorDate` so assertions are stable forever, while the running demo shows dates
relative to today.

**Empty-account rule (AC-003).** Accounts whose derived seed satisfies `seed % 7 == 0` are seeded
with **zero transactions and a zero balance**. This makes the empty state reachable in a live demo
and lets a test deterministically *find* such a customer by enumerating synthetic GUIDs, without any
special test-only endpoint.

## 4. API surface (provider-neutral, Stripe-inspired)

| Method | Route | Purpose | Success | Failure |
|--------|-------|---------|---------|---------|
| `GET` | `/v1/billing_accounts/{customerId:guid}` | Balance, status, due date | `200 BillingAccountDto` | — |
| `GET` | `/v1/billing_accounts/{customerId:guid}/transactions?limit=20` | Invoice/payment history, newest first | `200 BillingTransactionListDto` | `400` on `limit` outside `[1,100]` |
| `POST` | `/v1/payment_intents` (header `Idempotency-Key`) | Submit a simulated payment | `200 PaymentIntentDto` (`succeeded` **or** `failed`) | `400 BillingErrorDto` on validation |
| `GET` | `/v1/payment_intents/{id}` | Retrieve a previously created intent | `200 PaymentIntentDto` | `404` |
| `POST` | `/v1/billing_accounts/{customerId:guid}/reset` | Demo reset — re-seed one account | `200 BillingAccountDto` | — |

Identifier formats deliberately echo a real provider so the faux service is credible and could be
swapped for a real one without a contract change: `acct_{guid:N}`, `txn_{guid:N}`, `pi_{guid:N}`.

The `PaymentIntentDto` carries `BalanceAfterCents`, letting the UI update optimistically while it
refreshes; the refresh (AC-008) is still a real re-fetch so the displayed state is server-authoritative.

## 5. Aspire wiring decision (DEC-007)

Existing services use `.WithReference(x).WaitFor(x)`. For billing the plan deliberately uses
`.WithReference(billingApi)` **without** `.WaitFor(billingApi)`:

- The issue's edge-case section requires that a billing outage must **not** block Web startup or make
  the Dashboard/Customers/Scheduler pages unusable. `WaitFor` would gate `web` on `billingapi`
  reaching a healthy state, doing exactly the opposite.
- `WithReference` alone is sufficient for AC-012, since it is what injects the
  `services__billingapi__http__0` configuration consumed by service discovery.
- The Billing page's own failure isolation (AC-004) is handled by catching
  `BillingUnavailableException` in `BillingPanel` and rendering a service-unavailable alert.

## 6. Alternatives considered and rejected (summary)

| Area | Rejected alternative | Reason |
|------|----------------------|--------|
| Billing UI | `TelerikGrid` for the transaction table | bUnit rendering would require `TelerikRootComponent` + Telerik service registration in the test host; plain markup keeps AC-001/002/003 tests simple. Dispatch.razor already uses plain tables, so it is consistent. |
| Client design | Reuse the concrete-class client style of `OperationsApiClient` | Not mockable; `IBillingApiClient` is introduced *only* for the billing client so `BillingPanel` can be component-tested with a fake |
| Money type | `decimal Amount` in DTOs | Cents-as-`long` removes rounding/serialisation ambiguity; the UI converts once at the edge |
| Failure signalling | HTTP 402 for simulated declines | Conflates transport failure with provider outcome and forces exception-based flow in the client |
| Test layout | One combined test project | API tests need `Microsoft.NET.Sdk.Web`-style references while component tests need `Microsoft.NET.Sdk.Razor` + Telerik-bearing Web reference; separating them keeps each project minimal and fast |
| Duplicate-payment defence | UI button disable only | `AddStandardResilienceHandler` may retry a POST automatically; server-side idempotency is required |

## 7. Assumptions

| ID | Assumption | Impact if wrong |
|----|-----------|-----------------|
| ASM-001 | Impersonating a selected customer is acceptable in place of authentication | Billing page would need a login flow — `BillingPanel` is already decoupled behind a `CustomerId` parameter, so only `Billing.razor` changes |
| ASM-002 | In-memory billing state (lost on restart) is acceptable for a demo | Would require re-introducing the rejected Postgres design from §3 OQ-5 option A |
| ASM-003 | USD-only, cents-based amounts are sufficient | Multi-currency would need a currency table; the DTO already carries a `Currency` field |
| ASM-004 | Determinism is required per `(customerId, anchorDate)` rather than absolutely | Pin `Billing:SeedAnchorDate` in `appsettings.json` to make it absolute |
| ASM-005 | The Telerik licence available locally is not required for the new plain-markup components | None — no new Telerik usage is introduced |
| ASM-006 | Adding a `test` section to `global.json` is acceptable | Without it `dotnet test` cannot run at all on the .NET 10 SDK (verified) |
| ASM-007 | `dotnet test` from the repository root is the "CI run" referenced by AC-015 | A future workflow file would simply call the same command |
| ASM-008 | Billing does not need to reconcile with `OperationsApi` delivery orders | If reconciliation is later wanted, seeded invoices can be replaced by an `OperationsApi` query without changing the Web-facing contract |
