Description

<!-- Provide a brief description of your changes -->

Adds a Fahrenheit/Celsius unit toggle to the "Local Weather — 5-Day Report" card on the Dashboard. The change is presentation-only: the demo forecast data (`WeatherDayForecast.HighF`/`LowF`) continues to be generated and stored in Fahrenheit, and a pure display transform (`FormatTemp`/`UnitSymbol`) converts values to Celsius only when that unit is selected.

A `TelerikButtonGroup` with two `ButtonGroupToggleButton` options (°F / °C) is added to the card header, backed by a private `TemperatureUnit` enum field that defaults to Fahrenheit and is not persisted, so it resets to Fahrenheit on every page reload. Supporting CSS (`.weather-card-header`, `.weather-unit-toggle`) was added to `theme.css` to lay out the new header row and keep it responsive on narrow viewports, following the existing `.weather-*` naming convention.

This addresses the acceptance criteria in Issue #2: an explicit, unambiguous unit control, correct away-from-zero rounding of Celsius values (including negatives), and instantaneous re-render with no persistence across reloads.

Type of Change

<!-- Mark the relevant option with an "x" -->

[ ] 🐛 Bug fix (non-breaking change which fixes an issue)
[x] ✨ New feature (non-breaking change which adds functionality)
[ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
[ ] 📚 Documentation update (changes to documentation only)
[ ] 🔧 Configuration change (changes to configuration files)
[x] 🎨 Code style update (formatting, renaming, etc.)
[ ] ♻️ Refactoring (no functional changes)
[ ] ⚡ Performance improvement
[ ] ✅ Test update (adding or updating tests)
[ ] 🔨 Build/CI update (changes to build process or CI configuration)

Related Issues

<!-- Link to related issues using # (e.g., Fixes #123, Related to #456) -->

Fixes #2

Changes Made

<!-- List the specific changes made in this PR -->

- Added a `TelerikButtonGroup` (°F / °C, `ButtonGroupSelectionMode.Single`) to the weather card header in `Home.razor`, wrapped in a new `.weather-card-header` container alongside the existing heading
- Added a private `TemperatureUnit` enum (`Fahrenheit`, `Celsius`) and a `unit` field defaulting to `Fahrenheit`, with no persistence so it resets on reload
- Added `FormatTemp(int fahrenheit)` and `UnitSymbol` helper members that perform an away-from-zero Fahrenheit-to-Celsius conversion for display only, leaving stored forecast data unchanged
- Updated the high/low temperature bindings in the weather strip to use `FormatTemp`/`UnitSymbol` instead of the raw `HighF`/`LowF` values with a hard-coded `&deg;`
- Added `.weather-card-header` (flex row, wraps on narrow viewports) and `.weather-unit-toggle` (font-size) CSS rules in `theme.css`
- Added a `.nia/work/*/approvals/` and `.nia/work/*/traces/` ignore rule to `.gitignore`
- Added workflow/planning artifacts under `.nia/work/job_2/` (issue notes, phase plan, task list, code review, and fix notes) documenting the design and verification of this change

Testing Performed

<!-- Describe the testing you've done -->

[ ] Ran linting - all checks pass
[ ] Ran tests - all tests pass
[x] Tested manually in local environment
[ ] Added new tests for new functionality
[ ] Updated existing tests as needed

End User Documentation Checklist

<!-- Complete this section if your PR includes documentation changes -->

[ ] Updated relevant documentation
[ ] Added/updated code examples where appropriate
[ ] Ran documentation build - succeeds without warnings
[ ] Tested documentation locally
[ ] Updated navigation if new pages added
[ ] Checked for broken internal links
[ ] Updated search index if significant content changes
[ ] Updated README.md if changes affect getting started
[ ] Updated CONTRIBUTING.md if changes affect contribution process

Code Quality Checklist

<!-- Complete this section for code changes -->

[x] Code follows project style guidelines
[x] Self-reviewed my own code
[x] Commented code in hard-to-understand areas
[ ] Made corresponding changes to documentation
[x] Changes generate no new warnings
[ ] Added tests that prove fix is effective or feature works
[ ] New and existing tests pass locally
[ ] Any dependent changes have been merged and published

Screenshots (if applicable)

<!-- Add screenshots to help reviewers understand your changes -->

<!-- No screenshots required -->

Additional Context

<!-- Add any other context about the PR here -->

This is a presentation-only change: `BuildFiveDayForecast()` and the `WeatherDayForecast` record still store temperatures in Fahrenheit only. The repository has no bUnit/component test project, so this change was verified via `dotnet build` and manual verification rather than automated component tests; introducing a test project is noted as a follow-up recommendation, not part of this PR's scope.

Reviewer Notes

<!-- Any specific areas you'd like reviewers to focus on? -->

Please double-check the Celsius rounding behavior in `FormatTemp` (away-from-zero rounding, especially for negative Fahrenheit values) and confirm the `TelerikButtonGroup`/`ButtonGroupToggleButton` API usage matches the installed Telerik UI for Blazor version.

---

For Maintainers

<!-- This section is for maintainers only -->

Pre-Merge Checklist

[ ] PR title follows conventional commit format
[ ] All CI checks pass
[ ] Code has been reviewed and approved
[ ] Documentation is complete and accurate
[ ] Breaking changes are documented
[ ] Version number updated (if applicable)
[ ] Changelog updated (if applicable)
