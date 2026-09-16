# Visual Pull Request Evidence

These instructions apply to every GitHub Copilot CLI workflow in this repository, including workflows invoked through NIA.

## Decision

Inspect the issue, user request, and git diff. A change is visual when it affects a user-facing page, component, rendered text, layout, style, chart, grid, form, dialog, navigation, interaction, or responsive behavior. Backend-only changes, tests, build configuration, documentation, and internal refactors are non-visual unless they change rendered behavior.

## Required Workflow For Visual Changes

1. Read and follow `.agents/skills/playwright-cli/SKILL.md` and `.agents/skills/pr-screenshot-workflow/SKILL.md`.
2. Start or inspect the Aspire application using its documented lifecycle commands. Discover the frontend URL from Aspire; do not guess a port.
3. Use Playwright CLI to navigate to and exercise the changed state. Capture a descriptive, high-resolution PNG under `/tmp/pr-screenshots/`.
4. When a pull request exists, upload the PNG and append it beneath a `## Visual Evidence` heading in the PR body. Preserve the existing PR body. Verify the image URL returns HTTP 200 and the PR body includes the image markdown.
5. Clean up the transient screenshot after successful upload.

## Boundaries

- Do not claim visual verification succeeded unless the screenshot is embedded in the PR body.
- If the application cannot start or the changed state is inaccessible, report the concrete blocker and do not fabricate evidence.
- Do not modify unrelated application configuration or use placeholder secrets solely to obtain a screenshot.