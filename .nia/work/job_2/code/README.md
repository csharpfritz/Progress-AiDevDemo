# Issue 2 — Fahrenheit / Celsius Toggle on the Weather Card

## Overview

Add a Telerik-based °F / °C unit toggle to the **"Local Weather — 5-Day Report"** card on the
Dashboard (`/`). The change is **presentation-only**: the demo forecast data stays in Fahrenheit
and a pure display transform converts values when Celsius is selected.

This is a **single-phase, single-file-plus-CSS** change. No API, contract, or data-model changes.

## Approach

| Decision | Choice | Rationale |
|---|---|---|
| Component | `TelerikButtonGroup` with two `ButtonGroupToggleButton` children (`°F`, `°C`) | Explicit labels satisfy AC-007 unambiguously; `SelectionMode="ButtonGroupSelectionMode.Single"` guarantees exactly one active unit (AC-010). A `TelerikSwitch` would need an extra text label to convey which unit is active. |
| State | `private TemperatureUnit unit = TemperatureUnit.Fahrenheit;` private enum in `Home.razor` | Smallest footprint; matches the file's existing use of a private nested `record`. Field resets on each page load, satisfying AC-011 (no persistence). |
| Conversion | Single helper `FormatTemp(int fahrenheit)` returning the rounded numeric string, plus `UnitSymbol` property | One reusable method prevents rounding drift across the 5 days (NFR: Maintainability). |
| Rounding | `Math.Round(value, MidpointRounding.AwayFromZero)` on a `double` | Deterministic and correct for negatives; avoids banker's rounding surprises. |
| Re-render | Native Blazor Server via existing `@rendermode InteractiveServer` | No JS interop, no `StateHasChanged()` needed — the button group's `SelectedButtonChanged`/bound value triggers re-render of the whole card atomically (AC-003, AC-004, AC-010). |
| Styling | New `.weather-unit-toggle` class in `theme.css`, following existing `.weather-*` convention | Keeps ad hoc inline styles out; only handles placement/spacing, letting the Telerik theme own the control's look (AC-009). |

## Files Changed

| File | Change | Type |
|---|---|---|
| `ProgressHomeHeating.Web/Components/Pages/Home.razor` | Add `TemperatureUnit` enum, `unit` field, `FormatTemp`/`UnitSymbol` helpers, `TelerikButtonGroup` markup in the weather card header, and swap the two temperature bindings | Modify |
| `ProgressHomeHeating.Web/wwwroot/css/theme.css` | Add `.weather-card-header` and `.weather-unit-toggle` rules near the existing `.weather-*` block (after line ~53, before `.weather-strip`) | Modify |

**No other files are touched.** `BuildFiveDayForecast()` and the `WeatherDayForecast` record keep
their Fahrenheit storage unit unchanged.

## Conversion Reference (AC-006)

Formula: `C = round((F - 32) * 5 / 9)`, away-from-zero.

| Day | Condition | High °F | High °C | Low °F | Low °C |
|---|---|---|---|---|---|
| 1 | Chilly & overcast | 28 | -2 | 19 | -7 |
| 2 | Light snow showers | 24 | -4 | 14 | -10 |
| 3 | Snow likely | 19 | -7 | 8 | -13 |
| 4 | Bitter cold snap | 12 | -11 | -2 | -19 |
| 5 | Deep freeze | 6 | -14 | -9 | -23 |

None of the 10 values land on an exact `.5` midpoint, so the result table is identical under
away-from-zero and to-even rounding. Away-from-zero is still specified for future-proofing.

## Testing Position

The repository has **no bUnit or component test project**. This issue does **not** introduce one —
scope is a ~30-line presentation change and standing up a new test project would exceed the lite
scope. Verification is via `dotnet build` plus the manual checklist in `phase_1.md`. Introducing
bUnit is recorded as a follow-up recommendation, not a blocker.

## Assumptions

1. Telerik UI for Blazor v15.0.0 exposes `TelerikButtonGroup` / `ButtonGroupToggleButton` with
   `SelectionMode` — already referenced in `ProgressHomeHeating.Web.csproj`, no new package needed.
   *If the exact API differs in v15.0.0, fall back to `TelerikSwitch` + adjacent `°F`/`°C` labels;
   all acceptance criteria remain satisfiable.*
2. The unit resets to Fahrenheit on reload — explicitly desired per AC-011.
3. Both high and low use the same unit at all times; per-value units are not a requirement.
4. The footnote copy is unit-agnostic and needs no edit.
5. The app runs via the Aspire AppHost for manual verification.

## Out of Scope

- Persisting unit preference across sessions/reloads.
- Locale-based default unit.
- Replacing the static demo forecast with a live weather API.
- Adding a component test project.
