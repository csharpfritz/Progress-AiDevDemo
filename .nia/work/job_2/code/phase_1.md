# Phase 1 — Fahrenheit / Celsius Toggle on the Weather Card

```yaml
phase: 1
title: Fahrenheit / Celsius toggle on the 5-Day Weather Report card
issue: 2
depends_on: []
scope: presentation-only
files:
  - path: ProgressHomeHeating.Web/Components/Pages/Home.razor
    action: modify
  - path: ProgressHomeHeating.Web/wwwroot/css/theme.css
    action: modify
estimated_loc: ~45
```

## Preconditions

- Repo checked out at `/Users/zdravkov/work/lab/Progress-AiDevDemo`.
- `Telerik.UI.for.Blazor` v15.0.0 restored (already in `ProgressHomeHeating.Web.csproj`, line 9).
- `@using Telerik.Blazor` and `@using Telerik.Blazor.Components` already present in
  `ProgressHomeHeating.Web/Components/_Imports.razor` — **no new `@using` directives required**.
- Telerik NuGet feed credentials configured (needed for restore/build).

---

## T1 — Add `TemperatureUnit` enum and state field

**File:** `ProgressHomeHeating.Web/Components/Pages/Home.razor`
**Depends on:** none

In the `@code { ... }` block, immediately after the line:

```csharp
    private List<WeatherDayForecast> forecast = [];
```

insert:

```csharp
    private TemperatureUnit unit = TemperatureUnit.Fahrenheit;
```

Then, at the bottom of the `@code` block, immediately **before** the line:

```csharp
    private sealed record WeatherDayForecast(string DayName, string Icon, string Condition, int HighF, int LowF);
```

insert:

```csharp
    private enum TemperatureUnit
    {
        Fahrenheit,
        Celsius
    }
```

**Rationale:** The field is instance-scoped, so it is re-initialised to `Fahrenheit` on every page
render/circuit — satisfying AC-002 and AC-011 without extra code.

**Done when:** File compiles (verified in T6); `unit` and `TemperatureUnit` exist in `Home.razor`.

---

## T2 — Add conversion helpers

**File:** `ProgressHomeHeating.Web/Components/Pages/Home.razor`
**Depends on:** T1

In the `@code` block, immediately after the `BuildFiveDayForecast()` method's closing brace and
before the `TemperatureUnit` enum added in T1, insert:

```csharp
    private string UnitSymbol => unit == TemperatureUnit.Celsius ? "°C" : "°F";

    // Pure display transform: forecast data is always stored in Fahrenheit.
    private string FormatTemp(int fahrenheit) => unit switch
    {
        TemperatureUnit.Celsius =>
            ((int)Math.Round((fahrenheit - 32) * 5d / 9d, MidpointRounding.AwayFromZero))
                .ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => fahrenheit.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
```

**Constraints:**
- Use `5d / 9d` (double division) — integer division would truncate to `0` and produce wrong values.
- `MidpointRounding.AwayFromZero` is required by the plan for determinism on negatives.
- Do **not** duplicate this logic inline in the markup (NFR: Maintainability).

**Done when:** `FormatTemp(-9)` returns `"-23"` and `FormatTemp(28)` returns `"-2"` when
`unit == Celsius`; both return the input unchanged when `unit == Fahrenheit`.

---

## T3 — Add the Telerik toggle to the card header

**File:** `ProgressHomeHeating.Web/Components/Pages/Home.razor`
**Depends on:** T1

Replace this single line inside the weather `surface-card`:

```razor
                <h4>Local Weather — 5-Day Report</h4>
```

with:

```razor
                <div class="weather-card-header">
                    <h4 class="mb-0">Local Weather — 5-Day Report</h4>
                    <TelerikButtonGroup SelectionMode="ButtonGroupSelectionMode.Single"
                                        Class="weather-unit-toggle">
                        <ButtonGroupToggleButton Selected="@(unit == TemperatureUnit.Fahrenheit)"
                                                 OnClick="@(() => unit = TemperatureUnit.Fahrenheit)"
                                                 Title="Show temperatures in Fahrenheit"
                                                 aria-label="Show temperatures in Fahrenheit">
                            °F
                        </ButtonGroupToggleButton>
                        <ButtonGroupToggleButton Selected="@(unit == TemperatureUnit.Celsius)"
                                                 OnClick="@(() => unit = TemperatureUnit.Celsius)"
                                                 Title="Show temperatures in Celsius"
                                                 aria-label="Show temperatures in Celsius">
                            °C
                        </ButtonGroupToggleButton>
                    </TelerikButtonGroup>
                </div>
```

**Constraints:**
- `SelectionMode="ButtonGroupSelectionMode.Single"` is mandatory — it enforces exactly one active
  unit and prevents a mixed/none state (AC-010).
- Do **not** call `StateHasChanged()`; the `OnClick` handler already triggers a re-render under
  `InteractiveServer`, and the whole `@foreach` re-evaluates in one pass (AC-003, AC-004, AC-010).
- Do **not** substitute a plain `<input type="checkbox">` or `<select>` (AC-009).
- Keep explicit `aria-label` values — the button text is a symbol-only glyph (NFR: Accessibility).

**Fallback (only if `ButtonGroupToggleButton` / `SelectionMode` are unavailable in v15.0.0):**
use `<TelerikSwitch @bind-Value="@isCelsius" OnLabel="°C" OffLabel="°F" />` with a
`bool isCelsius` field and a visible adjacent `<span>` showing the active unit. Record the
substitution in the PR description.

**Done when:** The toggle renders inside the card, styled by the Telerik theme, with °F pre-selected.

---

## T4 — Bind temperatures through the helper

**File:** `ProgressHomeHeating.Web/Components/Pages/Home.razor`
**Depends on:** T2

Replace:

```razor
                            <div class="weather-temps">
                                <span class="weather-high">@day.HighF&deg;</span>
                                <span class="weather-low">@day.LowF&deg;</span>
                            </div>
```

with:

```razor
                            <div class="weather-temps">
                                <span class="weather-high">@FormatTemp(day.HighF)@UnitSymbol</span>
                                <span class="weather-low">@FormatTemp(day.LowF)@UnitSymbol</span>
                            </div>
```

**Constraints:**
- The `&deg;` HTML entity is replaced by the `°` already contained in `UnitSymbol` — do not emit both.
- `BuildFiveDayForecast()` and the `WeatherDayForecast` record are **not** modified; `HighF`/`LowF`
  remain Fahrenheit.

**Done when:** All 10 rendered values carry an explicit `°F` or `°C` suffix (AC-007).

---

## T5 — Add supporting CSS

**File:** `ProgressHomeHeating.Web/wwwroot/css/theme.css`
**Depends on:** T3

Insert immediately **before** the existing `.weather-strip { ... }` rule (currently line ~55):

```css
.weather-card-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
}

.weather-unit-toggle {
    font-size: 0.8rem;
}
```

**Constraints:**
- Do not override Telerik's colors, borders, or focus ring on the button group — only placement and
  scale, so the control stays theme-consistent (AC-009).
- `flex-wrap: wrap` prevents the header overflowing the `surface-card` on narrow viewports
  (Edge case: layout/overflow).

**Done when:** The heading and toggle sit on one row on desktop and wrap gracefully below ~576px.

---

## T6 — Build

**Depends on:** T1–T5

```bash
cd /Users/zdravkov/work/lab/Progress-AiDevDemo
dotnet build ProgressHomeHeating.sln
```

**Acceptance:** Build succeeds with 0 errors. Pre-existing warnings unrelated to `Home.razor` are
acceptable; no new warnings originating from `Home.razor` or the Web project.

**If `ButtonGroupToggleButton` or `ButtonGroupSelectionMode` fails to resolve:** apply the T3
fallback (TelerikSwitch) and rebuild. Do not add a new NuGet package.

---

## T7 — Manual verification

**Depends on:** T6

Run the app:

```bash
cd /Users/zdravkov/work/lab/Progress-AiDevDemo
dotnet run --project ProgressHomeHeating.AppHost
```

Open the Web frontend URL from the Aspire dashboard and navigate to `/`.

### Verification checklist

| # | Check | Expected | AC |
|---|---|---|---|
| 1 | Weather card renders | A two-button °F / °C Telerik group appears beside the card heading | AC-001 |
| 2 | Initial load, no interaction | `°F` button is selected/highlighted; all 5 days show original values with `°F` suffix (28°F/19°F … 6°F/-9°F) | AC-002, AC-007 |
| 3 | Click `°C` | All 5 days switch instantly; no page reload, URL unchanged, other cards untouched | AC-003 |
| 4 | Read all 10 Celsius values | `-2/-7`, `-4/-10`, `-7/-13`, `-11/-19`, `-14/-23` | AC-005, AC-006 |
| 5 | Negative source values | Day 4 low `-2°F` → `-19°C`; Day 5 low `-9°F` → `-23°C` (correct sign) | AC-006, edge case |
| 6 | Click `°F` again | Values revert exactly to 28/19, 24/14, 19/8, 12/-2, 6/-9 with `°F` suffix | AC-004 |
| 7 | Toggle rapidly ~10× | No flicker, no exception banner, no circuit disconnect; final state matches last click | AC-010, edge case |
| 8 | Inspect mid-toggle rendering | Never a mix of °F and °C across the 5 day tiles | AC-010 |
| 9 | Keyboard only: `Tab` to the toggle, then `Enter`/`Space`/arrow keys | Visible focus ring; unit switches without a mouse | AC-008 |
| 10 | Screen reader / inspect DOM | Buttons expose `aria-label` describing the unit; pressed state reflected | AC-008, NFR a11y |
| 11 | Visual comparison to `TelerikChart` / `TelerikGrid` on the same page | Same Telerik theme, fonts, and accent colors; no native/unstyled control | AC-009 |
| 12 | Resize browser to ~375px width | Header wraps; card content stays within `surface-card`; no horizontal scrollbar | Edge case: layout |
| 13 | Hard-reload the page (F5) | Unit resets to °F | AC-011 |
| 14 | Regression sweep | Tank Levels chart, Fleet Summary counts, and Upcoming Deliveries grid (sorting + paging) all behave as before | NFR: No regression |
| 15 | Perceived latency on toggle | Update appears immediate (<200ms), no spinner or network wait | NFR: Performance |

**Phase complete when:** all 15 checks pass and `dotnet build` is clean.

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Telerik v15.0.0 API shape differs from expectation | T3 fallback to `TelerikSwitch`; all ACs remain satisfiable |
| Integer division bug in conversion (`5 / 9` → `0`) | T2 mandates `5d / 9d`; check #4 in T7 catches it immediately |
| Sign error on negative Fahrenheit values | T7 check #5 explicitly targets `-2°F` and `-9°F` |
| Header layout overflow in the shared flex column | `flex-wrap: wrap` in T5; T7 check #12 |
| Unit conveyed only by button highlight | Explicit `°F`/`°C` suffix on every value (T4) plus `aria-label`s (T3) |
| Telerik NuGet restore failure blocking T6 | Confirm feed credentials before starting; this is an environment prerequisite, not a code change |

## Follow-up (not part of this issue)

Introducing a bUnit component test project would allow automating AC-003 through AC-006. Recommended
as a separate issue; deliberately excluded here to keep this change lite-scoped.
