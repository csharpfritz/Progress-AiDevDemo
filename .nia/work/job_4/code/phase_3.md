# PHASE-3 — Aspire AppHost and Solution Wiring

## Goal

Register `billingapi` as an Aspire resource, connect `web` to it through service discovery, add the
new projects to the solution, and opt the repository into the Microsoft Testing Platform runner
required by the .NET 10 SDK.

## Dependencies

- **Depends on:** PHASE-2 (`ProgressHomeHeating.BillingApi.csproj` must exist and build).
- **Blocks:** PHASE-5.
- **Parallel with:** PHASE-4.

## Tasks

### TASK-3.1 — Add projects to the solution

Edit `ProgressHomeHeating.slnx`. Replace its full content with:

```xml
<Solution>
  <Project Path="ProgressHomeHeating.Web/ProgressHomeHeating.Web.csproj" />
  <Project Path="ServiceDefaults/ServiceDefaults.csproj" />
  <Project Path="ProgressHomeHeating.Contracts/ProgressHomeHeating.Contracts.csproj" />
  <Project Path="ProgressHomeHeating.OperationsApi/ProgressHomeHeating.OperationsApi.csproj" />
  <Project Path="ProgressHomeHeating.AgentApi/ProgressHomeHeating.AgentApi.csproj" />
  <Project Path="ProgressHomeHeating.BillingApi/ProgressHomeHeating.BillingApi.csproj" />
  <Project Path="ProgressHomeHeating.BillingApi.Tests/ProgressHomeHeating.BillingApi.Tests.csproj" />
  <Project Path="ProgressHomeHeating.Web.Tests/ProgressHomeHeating.Web.Tests.csproj" />
</Solution>
```

> The two test projects are created in PHASE-5. Until then `dotnet build ProgressHomeHeating.slnx`
> fails with "project not found". Therefore: either execute this task **after** TASK-5.1 and
> TASK-5.2, or temporarily omit the last two `<Project>` lines and add them in TASK-5.3.
> The declared order in `tasks.md` uses the second approach.

### TASK-3.2 — Register `billingapi` in the AppHost

Edit `ProgressHomeHeating.AppHost/apphost.cs`.

1. Insert the following **after** the `agentApi` declaration and **before** the `web` declaration:

```csharp
var billingApi = builder.AddDotnetProject("billingapi", "../ProgressHomeHeating.BillingApi/ProgressHomeHeating.BillingApi.csproj");
```

2. Replace the existing `web` registration block with:

```csharp
// Billing is referenced but deliberately not awaited: a billing outage must not block web startup
// or the Dashboard/Customers/Scheduler pages. The Billing page degrades on its own (AC-004).
builder.AddDotnetProject("web", "../ProgressHomeHeating.Web/ProgressHomeHeating.Web.csproj")
    .WithReference(operationsApi)
    .WaitFor(operationsApi)
    .WithReference(agentApi)
    .WaitFor(agentApi)
    .WithReference(billingApi);
```

Do **not** add a Postgres database for billing — the service is intentionally in-memory (DEC-001).
Do **not** add any `WithEnvironment` call for billing — it needs no secrets (AC-011).

### TASK-3.3 — Verify the AppHost starts all four services

```bash
cd ProgressHomeHeating.AppHost
aspire run
```

Confirm in the Aspire dashboard that a `billingapi` resource appears and reaches the **Running /
Healthy** state, then stop with `Ctrl+C`.

Non-interactive alternative:

```bash
cd ProgressHomeHeating.AppHost
aspire start --wait
aspire ps
aspire describe
aspire stop
```

### TASK-3.4 — Opt in to the Microsoft Testing Platform runner

Edit `global.json` at the repository root. Replace its full content with:

```json
{
  "sdk": {
    "version": "10.0.400"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

Rationale (verified empirically — see `research.md` §2): on the .NET 10 SDK, `dotnet test` fails with
*"Testing with VSTest target is no longer supported"* unless this opt-in is present. An INI
`dotnet.config` file does **not** work.

## Acceptance criteria for PHASE-3

- [ ] `ProgressHomeHeating.AppHost/apphost.cs` contains exactly one `AddDotnetProject("billingapi", …)` call.
- [ ] The `web` registration contains `.WithReference(billingApi)` and **no** `.WaitFor(billingApi)`.
- [ ] `grep -R "localhost:53\|localhost:73" ProgressHomeHeating.Web --include=*.cs --include=*.razor` returns no matches — Web never hard-codes a billing URL (AC-012).
- [ ] `aspire run` (or `aspire start --wait`) brings up `postgres`, `operationsapi`, `agentapi`, `billingapi`, and `web` with no manual extra steps (AC-016).
- [ ] `aspire ps` lists `billingapi` (AC-013).
- [ ] `global.json` contains the `test.runner` section.
- [ ] `dotnet build ProgressHomeHeating.slnx` exits `0`.

## Validation commands

```bash
dotnet build ProgressHomeHeating.slnx
cd ProgressHomeHeating.AppHost && aspire start --wait && aspire ps && aspire stop
```
