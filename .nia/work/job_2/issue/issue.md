# Add Fahrenheit / Celsius toggle to the weather display

## Summary
Add a Fahrenheit / Celsius unit toggle to the "Local Weather — 5-Day Report" card on the Dashboard so users can switch the displayed temperature unit for the 5-day forecast without reloading the page.

## Context & Background
- **Repository architecture:** This is a monolith solution (`.NET Aspire` orchestrated app with a single Blazor Server web project, `ProgressHomeHeating.Web`, and a backing API). No multi-repo or multi-service split applies to this change — it is fully contained within `ProgressHomeHeating.Web`.
- **Affected module:** `ProgressHomeHeating.Web/Components/Pages/Home.razor` — the Dashboard page, specifically the "Local Weather — 5-Day Report" card (`weather-strip` / `weather-day` / `weather-temps` markup, lines ~55–70).
- **Current behavior:**
  - `BuildFiveDayForecast()` (in `Home.razor` code-behind) generates 5 days of static demo data as a `WeatherDayForecast` record: `(string DayName, string Icon, string Condition, int HighF, int LowF)`.
  - Temperatures are always in Fahrenheit and rendered directly as `@day.HighF°` / `@day.LowF°` with no unit conversion or indicator beyond the literal `°` symbol.
  - Data is demo/synthetic (not sourced from a real weather API), intentionally trending colder through the week to reinforce the "top off your tank" messaging.
- **Design system constraint:** The app uses **Telerik UI for Blazor v15.0.0** (registered via `AddTelerikBlazor()` in the Web project) as its component library across the dashboard (e.g., `TelerikChart`, `TelerikGrid`). The new toggle must be a Telerik component to remain visually and behaviorally consistent with the rest of the UI.
- **Styling:** Weather card styling lives in `ProgressHomeHeating.Web/wwwroot/css/theme.css` under `.weather-*` classes (`.weather-strip`, `.weather-day`, `.weather-day-name`, `.weather-icon`, `.weather-condition`, `.weather-temps`, `.weather-high`, `.weather-low`, `.weather-footnote`). Any new toggle styling should follow this naming convention if custom CSS is needed.
- **No existing automated UI/component tests** exist in the repository (no bUnit or Blazor component test project was found), so acceptance criteria below are written to be verifiable via manual/exploratory testing as well as automated tests if the team chooses to add them.

## Proposed Solution (non-binding, for context only)
Add a small unit toggle in the "Local Weather — 5-Day Report" card, backed by a `TemperatureUnit` state field (e.g., an enum `Fahrenheit` / `Celsius`), using a Telerik component — `TelerikButtonGroup` (two toggle buttons: °F / °C) or `TelerikSwitch` as an acceptable alternative. The underlying forecast data remains in Fahrenheit; only the display converts, using `C = round((F - 32) * 5 / 9)`.

*(Implementation details, including exact component choice and code structure, are left to the development/architecture team.)*

## Acceptance Criteria
- **AC-001:** Given the Dashboard is loaded, when the "Local Weather — 5-Day Report" card renders, then a Telerik-based unit toggle (`TelerikButtonGroup` or `TelerikSwitch`) is visibly present in the card.
- **AC-002:** Given the Dashboard is loaded for the first time (no prior interaction), when the weather card renders, then the selected/active unit is **Fahrenheit** and all 5 days show high/low values with a `°F`-equivalent indicator (matching current behavior).
- **AC-003:** Given the toggle is set to Fahrenheit, when the user selects Celsius, then all 5 days' high and low temperatures update to their Celsius-converted values immediately, with no full page reload or navigation.
- **AC-004:** Given the toggle is set to Celsius, when the user selects Fahrenheit, then all 5 days' high and low temperatures revert to their original Fahrenheit values immediately, with no full page reload or navigation.
- **AC-005:** Given any unit is selected, when temperatures are displayed, then each displayed value is rounded to the nearest whole degree (no decimals) in both °F and °C.
- **AC-006:** Given the Celsius conversion formula `C = round((F - 32) * 5 / 9)`, when converting each of the current demo dataset's 10 values (5 highs + 5 lows), then the displayed Celsius values match the expected rounded results for that formula (verifiable against a fixed table of inputs/outputs derived from the current demo data).
- **AC-007:** Given either unit is active, when temperatures are rendered, then the unit symbol (°F or °C) is visibly associated with each value, or the active toggle state unambiguously communicates the unit being shown (e.g., a selected/highlighted button labeled "°F" or "°C").
- **AC-008:** Given a keyboard-only user (no mouse), when they tab to the toggle control, then it can receive focus and be operated (e.g., via Enter/Space or arrow keys per the Telerik component's native keyboard support) to switch units.
- **AC-009:** Given the toggle control is rendered, when compared visually to other Telerik controls on the Dashboard (e.g., `TelerikChart`, `TelerikGrid`), then it uses the same Telerik theme and is visually consistent (no unstyled/native HTML controls substituted).
- **AC-010:** Given the user switches units multiple times in succession, when each switch occurs, then the UI never displays a mixed state (e.g., some days in °F and others in °C) — all 5 days update atomically together.
- **AC-011 (out-of-scope confirmation):** Given the user reloads the Dashboard page or navigates away and back, when the page re-renders, then the unit resets to the default (Fahrenheit) — persistence across sessions/reloads is explicitly not required for this issue.

## Technical Considerations & Dependencies
- **Component dependency:** Requires `TelerikButtonGroup`/`ButtonGroupToggleButton` or `TelerikSwitch`, already available via the existing Telerik UI for Blazor v15.0.0 package reference — no new package/version dependency expected.
- **State management:** Conversion must be a pure display transform over the existing `HighF`/`LowF` fields; the underlying `WeatherDayForecast` data model and `BuildFiveDayForecast()` demo data generation should not need to change their storage unit (stays Fahrenheit internally).
- **Render mode:** `Home.razor` uses `@rendermode InteractiveServer` — the toggle's interactivity (immediate re-render without page reload) is expected to work natively under Blazor Server's existing SignalR circuit; no additional JS interop should be required.
- **No backend/API changes anticipated** — this is a client-side/presentation-only change scoped to `Home.razor` and optionally `theme.css`.
- **Testing infrastructure gap:** The repository currently has no bUnit or other Blazor component test project. If the team wants automated coverage for AC-003/004/005/006, a decision is needed on whether to introduce one or rely on manual verification for this issue.

## Edge Cases & Risks
- **Rounding at conversion boundaries:** Some Fahrenheit values may convert to Celsius values that round differently depending on rounding strategy (e.g., round-half-up vs. round-half-to-even/banker's rounding) — the rounding rule should be applied consistently and match AC-006's fixed expected outputs.
- **Negative temperatures:** The demo dataset includes negative Fahrenheit values (e.g., `-2°F`, `-9°F` for "Deep freeze"/"Bitter cold snap" days). Conversion and rounding logic must handle negative numbers correctly (e.g., `-2°F` → `-19°C`, `-9°F` → `-23°C`) without sign errors.
- **Rapid toggling:** Rapidly clicking/switching the toggle multiple times in quick succession should not cause visual flicker, inconsistent intermediate states, or exceptions in the Blazor Server circuit.
- **Layout/overflow:** Adding a toggle control to an already-populated card (`Fleet Summary` and weather card share a flex column) could cause layout shifts or overflow on smaller viewports — verify the card still fits within its `surface-card` container responsively.
- **Accessibility of icons/symbols:** The `°` symbol and unit labels should remain readable by screen readers (e.g., avoid conveying the unit solely through color or an icon-only toggle button with no accessible label).
- **Existing footnote text:** The footnote ("Cold today, colder tomorrow...") references cold weather in a way that should still make sense regardless of displayed unit — no change expected, but worth a visual check that the message isn't unit-dependent.

## Non-Functional Requirements (NFR)
- **Performance:** Unit switching must feel instantaneous (sub-200ms perceived UI update) since it is a pure client-rendered transform with no network/API round-trip.
- **Accessibility:** The toggle must be operable via keyboard alone and expose appropriate ARIA roles/labels (inherited from the Telerik component) so screen readers can announce the current unit selection.
- **Consistency:** Visual styling (colors, spacing, fonts) must match the existing Telerik theme applied elsewhere on the Dashboard — no ad hoc styling that diverges from `.weather-*` conventions in `theme.css`.
- **Maintainability:** Conversion logic should be a single, clearly named, reusable method/helper (not duplicated inline per binding) to minimize risk of inconsistent rounding across the 5 days.
- **No regression:** Existing Dashboard functionality (Tank Levels chart, Fleet Summary, Upcoming Deliveries grid) must remain unaffected by this change.

## Out of Scope
- Persisting the user's unit preference across sessions or page reloads.
- Localizing or auto-defaulting the unit based on region/locale.
- Changing the forecast data source (remains static demo data).

## Affected Areas
- `ProgressHomeHeating.Web/Components/Pages/Home.razor`
- Optional styling: `ProgressHomeHeating.Web/wwwroot/css/theme.css` (`.weather-*` classes)
