# Implementation Checklist — Issue 2

Single phase. Execute tasks top to bottom; each depends on the one above it.

## Phase 1 — Fahrenheit / Celsius Toggle

- [x] **T1** — Add `TemperatureUnit` enum and `unit` state field to the `@code` block of `ProgressHomeHeating.Web/Components/Pages/Home.razor`
- [x] **T2** — Add `UnitSymbol` property and `FormatTemp(int fahrenheit)` helper to the same `@code` block
- [x] **T3** — Wrap the weather card heading in a `.weather-card-header` flex row and add the `TelerikButtonGroup` toggle
- [x] **T4** — Replace the two hard-coded temperature bindings in `.weather-temps` with `FormatTemp` + `UnitSymbol` output
- [x] **T5** — Add `.weather-card-header` and `.weather-unit-toggle` rules to `ProgressHomeHeating.Web/wwwroot/css/theme.css`
- [x] **T6** — Build the solution and resolve any compile errors. *(No `.sln` exists in this repo; built `ProgressHomeHeating.Web/ProgressHomeHeating.Web.csproj` directly — 0 errors, only pre-existing Telerik license and unrelated `App.razor` warnings.)*
- [~] **T7** — Run the app via the Aspire AppHost and complete the manual verification checklist in `phase_1.md`. *(Partial: the AppHost started successfully via `dotnet run apphost.cs` with all resources (Web, OperationsApi, AgentApi) launching cleanly, confirming no startup regressions. However, direct browser/HTTP verification of the rendered page in this non-interactive, no-browser environment was not achievable — the Web project's dynamically assigned Kestrel ports were not reachable via `curl` from this sandbox. Manual UI verification against the 15-point checklist should be performed by a developer running `dotnet run --project ProgressHomeHeating.AppHost` locally with a browser.)*

## Acceptance Criteria Coverage

- [x] AC-001 — Telerik toggle visible in the card (T3)
- [x] AC-002 — Defaults to Fahrenheit with unit indicator (T1, T4)
- [x] AC-003 — °F → °C updates all 5 days immediately (T3, T4)
- [x] AC-004 — °C → °F reverts immediately (T3, T4)
- [x] AC-005 — Whole-degree values, no decimals (T2)
- [x] AC-006 — Conversion table matches expected outputs (T2, verified by inspection: FormatTemp(28)→"-2", FormatTemp(-9)→"-23", etc.)
- [x] AC-007 — Unit symbol visible per value and on the active button (T3, T4)
- [x] AC-008 — Keyboard focusable and operable (T3, inherited from `TelerikButtonGroup`; not manually re-verified in-browser, see T7 note)
- [x] AC-009 — Visually consistent with other Telerik controls (T3, T5)
- [x] AC-010 — No mixed-unit state across days (T1, T3)
- [x] AC-011 — Resets to Fahrenheit on reload (T1)

**Note:** All code-level tasks (T1–T6) are complete and verified via successful build. T7's manual,
in-browser checklist is recommended for a developer with GUI/browser access before merging, since
this run was performed in a non-interactive, headless environment.

## Auto-Fix Pass (Code Review Follow-up)

`review.md` (lightweight review — bugs, security, breaking changes only) reported **no issues**
across all severities. `fix.md` requested fixes for critical, major, and minor findings, but since
the review identified zero findings in any category, **no code changes were required or applied**.

- [x] **Auto-fix scope evaluated** — Reviewed `review.md` findings against `fix.md`'s
  critical/major/minor scope. Result: 0 findings to address.
- No changes made to `Home.razor`, `theme.css`, or any other file.
- No new tests added (no behavior changed).
- Re-ran `dotnet build ProgressHomeHeating.Web/ProgressHomeHeating.Web.csproj` to reconfirm a clean
  baseline — 0 errors (same pre-existing Telerik license / `App.razor` warnings as before).
