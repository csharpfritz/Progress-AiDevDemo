# Code Review — Issue #4: Customer Billing Page + Aspire-Orchestrated Faux Billing Service

**Plan type:** Full plan (README.md, research.md, tasks.md, phase_1.md–phase_5.md)
**Commits reviewed:** `cba7046` → `fc7d91f` (6 commits, 34 files, +1886/-2 lines)
**Branch:** `issue/4`

## Summary

This is a well-executed, self-contained feature addition: a new `ProgressHomeHeating.BillingApi`
microservice with a deterministic in-memory store, a typed `BillingApiClient` + `BillingPanel`/`Billing`
Blazor page, Aspire wiring, and two new xUnit v3 test projects (30 tests total). All 16 acceptance
criteria and 5 phases are marked complete, and my independent verification confirms the claims:

- `dotnet build ProgressHomeHeating.slnx` succeeds with 0 errors.
- Both new test assemblies pass in full when executed directly (`BillingApi.Tests`: 22/22,
  `Web.Tests`: 8/8) — see **Major-1** for a caveat about the `dotnet test` wrapper.
- The design decisions in `research.md`/README (DEC-001…DEC-009) are all reflected faithfully in
  the code: deterministic seeding, GUID-derived accounts, idempotency-key handling, magic-cent
  simulated failures, `.WithReference` without `.WaitFor` for billing, and the MTP opt-in in
  `global.json`.

Code quality is high: clear separation of concerns (`BillingOptions`/`BillingRecords`/`BillingStore`/
`FauxPaymentProcessor`/`MappingExtensions`/`BillingEndpoints`), consistent use of `ConcurrentDictionary`
+ per-account locks for thread safety, and good inline comments explaining non-obvious decisions
(replay-before-validation ordering, `en-US`-pinned currency formatting, no-`WaitFor` rationale).

No critical, blocking defects were found. A few major/minor issues are worth addressing before or
shortly after merge.

---

## Critical Issues

None found.

---

## Major Issues

### Major-1: `dotnet test ProgressHomeHeating.slnx` reports "Zero tests ran" in this environment

- **Location:** Repo root / both new test projects (`ProgressHomeHeating.BillingApi.Tests`,
  `ProgressHomeHeating.Web.Tests`)
- **Issue:** Running `dotnet test` (the exact command documented in the new README section and in
  `tasks.md` TASK-5.7) produces `Zero tests ran` / exit code 5 for **both** new test projects in
  this sandbox, even though the assemblies build successfully. Running the compiled test binaries
  directly (`./bin/Debug/net10.0/ProgressHomeHeating.BillingApi.Tests`) works and passes all 22/8
  tests. `dotnet test -v diag` surfaces SDK resolver warnings
  (`MSB4276: ... Microsoft.NET.SDK.WorkloadAutoImportPropsLocator ...`) suggesting an environment/
  SDK-workload mismatch with the Microsoft.Testing.Platform `dotnet test` integration rather than a
  bug in the test code itself.
- **Impact:** If CI or other contributors' machines exhibit the same behavior, the documented
  `dotnet test ProgressHomeHeating.slnx` command (now called out in README and `tasks.md`) will
  falsely report zero tests / a non-zero exit code, which could be mistaken for "no tests exist" or
  break a CI gate even though the tests themselves are correct and passing.
- **Fix:** Verify this in the actual CI runner image (not just this local/sandbox SDK install)
  before relying on `dotnet test` as the sole verification gate. If it reproduces in CI, consider
  documenting/using `dotnet run` on the test project's output executable, or pin/repair the SDK
  workload manifests referenced in the warning. Since PHASE-5/TASK-5.7 claims `dotnet test` was run
  successfully, please double check whether that run happened in a different (working) SDK install
  than this one, and note the caveat in `tasks.md` if so.

### Major-2: Idempotency replay does not validate the replayed request matches the original

- **Location:** `ProgressHomeHeating.BillingApi/Billing/BillingStore.cs` (`ApplyPayment`,
  `FindReplay`), `Endpoints/BillingEndpoints.cs`
- **Issue:** `FindReplay`/`ApplyPayment` key solely on `idempotencyKey` per account and return the
  original stored `PaymentIntentRecord` without checking that `AmountCents`/`Currency`/
  `PaymentMethod` on the replay request match the original request. A client (or a bug in the UI)
  that reuses an `Idempotency-Key` with a *different* amount would silently get back the original
  intent's result instead of an error, which could mask double-submission bugs.
- **Impact:** Low real-world risk given the client always generates a fresh
  `Guid.NewGuid().ToString()` per submission (see `BillingPanel.razor`), but it's a latent
  correctness gap versus Stripe's actual idempotency semantics (which return HTTP 409 on parameter
  mismatch) and worth a code comment or a follow-up guard, especially since AC-009 explicitly calls
  out the endpoints must be usable independently of the Web app (i.e., by other, less careful
  clients).
- **Fix:** Either document the simplification explicitly (e.g., a code comment noting "for this
  demo, replay is keyed on the header alone; full Stripe semantics would revalidate the payload"),
  or add a lightweight check that stores a hash of `(AmountCents, Currency, PaymentMethod)` alongside
  the idempotency key and returns a 409/`BillingErrorDto` on mismatch.

---

## Minor Issues

### Minor-1: Unbounded account creation via arbitrary `customerId`

- **Location:** `InMemoryBillingStore.GetOrSeed` / `BillingEndpoints.cs` (`GET /v1/billing_accounts/{customerId:guid}`)
- **Issue:** Any syntactically valid GUID passed to `GET /v1/billing_accounts/{customerId}` will
  auto-seed and permanently retain an account in the process-lifetime `ConcurrentDictionary`, with
  no cap, eviction, or authentication. This is consistent with DEC-003 (no auth model) and is a
  demo app, so this is not a hard blocker, but it is worth a one-line note in `research.md`/README
  as a known limitation, since it's a straightforward, unbounded-memory-growth vector if the service
  were ever exposed beyond the Aspire-internal network.
- **Fix (optional):** No code change required for a demo; consider documenting the limitation, or
  add a soft cap (e.g., log a warning past N accounts) if this project is ever extended toward a
  more production-like posture.

### Minor-2: `Billing.razor` has no explicit "no customers" empty state

- **Location:** `ProgressHomeHeating.Web/Components/Pages/Billing.razor`
- **Issue:** If `Operations.GetCustomersAsync()` succeeds but returns an empty list, `customers` is
  empty, `selectedCustomerId` ends up `null`, and the page silently renders just the (now-empty)
  `<select>` with no rows and no `BillingPanel` — with no message telling the user why. Every other
  state (loading, unavailable, loaded) has an explicit UI branch except this one.
- **Impact:** Minor UX gap; unlikely in practice since the demo always seeds customers, but it's an
  inconsistency versus the otherwise thorough state handling (AC-002/AC-003/AC-004 pattern) elsewhere
  in this feature.
- **Fix:**
  ```razor
  else if (customers.Count == 0)
  {
      <p class="text-muted" data-testid="billing-no-customers">No customers are available yet.</p>
  }
  else
  {
      ... existing selector + BillingPanel ...
  }
  ```

### Minor-3: `PayableCents` test helper duplicates production validation logic implicitly

- **Location:** `ProgressHomeHeating.BillingApi.Tests/BillingEndpointsTests.cs` (`PayableCents`)
- **Issue:** The helper computes a "safe" payable amount using its own arithmetic
  (`balance - (balance % 100) - 500`) rather than referencing `BillingConstants.MinimumPaymentCents`
  more directly in the subtraction, and assumes accounts seeded via `TestCustomers.WithHistory` will
  always have balance ≥ 600 cents. This is a reasonable pragmatic choice for a test fixture, but if
  the seeder's `random.Int(9_000, 68_000)` range or invoice count range ever changes, this magic
  constant (`500`) could silently produce an invalid (too-low or too-high) payment and fail tests in
  a confusing way.
- **Fix:** Not urgent; consider a short comment above `PayableCents` explaining the safety margin
  assumption, or deriving the margin from `BillingConstants.MinimumPaymentCents` for clarity.

---

## Suggestions

- **`FauxPaymentProcessor.Validate`** checks currency/payment-method *after* the balance check. For
  a very large invalid amount with an unsupported currency, the error returned will be
  `AmountExceedsBalance` rather than `CurrencyUnsupported`, which is a minor ordering choice but
  could be made currency/method-first (structural validation before business-rule validation) for a
  more conventional API contract. Not a defect — Stripe itself doesn't guarantee a canonical error
  ordering either — just a stylistic suggestion.
- **`DeterministicBillingSeeder.DeriveSeed`** uses a simple FNV-ish rolling hash (`hash*31+b`) over
  the GUID bytes. This is fine for this use case (deterministic, not security-sensitive), but a code
  comment noting "not cryptographic, purely for reproducible demo data" would preempt future
  confusion if someone later tries to reuse this pattern elsewhere.
- Consider adding a test that asserts the `Idempotency-Replayed` response header is actually set on
  replay (currently only tested indirectly via matching intent IDs in
  `Repeating_an_idempotency_key_does_not_charge_twice`).

---

## Positive Observations

- **Faithful adherence to the plan.** Every design decision (DEC-001…DEC-009) in `README.md`/
  `research.md` is verifiably implemented exactly as described — this is a rare and appreciated
  level of plan-to-code fidelity.
- **Thread safety done right.** `ConcurrentDictionary<Guid, BillingAccountRecord>` plus a per-account
  `lock (account.Gate)` around both reads (`transactions` snapshot) and writes (`ApplyPayment`)
  correctly avoids torn reads/writes without over-locking the whole store.
- **Deterministic-by-design testing.** `DeterministicBillingSeeder` + `TestCustomers` (which
  brute-force-locates GUIDs satisfying `IsEmptyAccount`) is a clever, dependency-free way to get
  reproducible "empty" vs. "with history" fixtures without mocking or seeding a database — nicely
  supports AC-003 and AC-010 simultaneously.
- **Consistent, thoughtful error semantics.** `BillingErrorDto`/`BillingErrorCodes` mirror Stripe's
  error shape closely enough to feel authentic while staying provider-neutral, and the endpoint
  correctly returns HTTP 400 (not 422/500) for validation failures per AC-006.
  Failed-but-processed payments (declined/processing-error) correctly return HTTP 200 with
  `status=failed` rather than an HTTP error — matching how Stripe actually represents payment
  intents, and matching the plan's AC-007 requirement that balance stays untouched.
- **Good UX state coverage.** `BillingPanel.razor` and `Billing.razor` explicitly handle loading,
  unavailable (with retry), empty transaction history, validation error, payment failure, and
  success states — directly traceable to AC-001–AC-004/AC-006–AC-008 with `data-testid` hooks that
  make the bUnit tests (`BillingPanelTests.cs`) meaningful rather than superficial.
- **No `WaitFor` on billing, by design.** The AppHost correctly wires `billingapi` as a
  `.WithReference` only (not `.WaitFor`), matching DEC-007's explicit rationale that a billing
  outage must never block the rest of the app's startup — and the Web-side `BillingUnavailableException`
  handling backs this up end-to-end.
- **Culture-safe currency formatting.** `BillingPanel.FormatCents` pins `en-US` formatting since the
  API is hard-coded to USD, correctly avoiding locale-dependent currency symbol bugs — a small but
  easy-to-miss detail that was handled correctly.
- **Comprehensive test suite.** 22 API tests (`BillingEndpointsTests`, `DeterministicSeedingTests`)
  and 8 component tests (`BillingPanelTests`) cover normal, edge (empty/invalid limit), and
  provider-failure/idempotency-replay paths — a strong, balanced mix of integration
  (`WebApplicationFactory`) and unit-level (bUnit + fake client) coverage.

---

## Recommendation

**Approve with minor follow-ups.** The implementation is correct, well-tested, and faithful to the
approved plan; build and test verification (via direct binary execution) confirms all claims in
`tasks.md`. Recommend:

1. Confirm **Major-1** (`dotnet test` wrapper behavior) in the real CI environment before treating
   it as a reliable gate; this is likely an environment/SDK-install quirk rather than a code defect,
   but it should be verified rather than assumed.
2. Consider addressing **Major-2** (idempotency payload mismatch) with either a code comment or a
   small guard, given it's a genuine (if low-probability) correctness gap in a feature explicitly
   designed around idempotency safety (DEC-004).
3. Minor-1/Minor-2/Minor-3 and the suggestions are optional polish and do not block merge.
