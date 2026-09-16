---
name: pr-screenshot-workflow
description: >
  Capture Playwright CLI screenshots of the running ProgressHomeHeating app
  (Blazor Web frontend) and attach them to a GitHub pull request description or
  comment as visual evidence of a feature/UI change. Use when the user asks to
  "screenshot the change", "show the UI change in the PR", "add before/after
  screenshots", "visually verify", or after implementing a UI feature that
  should be showcased in a PR. Combines the playwright-cli skill (browser
  automation) with the github-pr-media skill (uploading images to GitHub).
version: "1.0.0"
---

# PR Screenshot Workflow

Standard workflow for this repo: implement a UI change -> start the app ->
capture screenshot(s) with Playwright CLI -> attach them to the PR.

## Workflow

1. **Make sure the app is running.**
   - This is a .NET Aspire solution. Start it with `aspire run` from
     `ProgressHomeHeating.AppHost/` (see `aspire-orchestration` skill), or ask
     the user if it's already running.
   - Get the `web` resource's URL (HTTPS, dynamically assigned) via
     `aspire ps` / the Aspire dashboard — see `aspire-monitoring` skill. Do not
     guess a fixed port.

2. **Capture screenshots with `playwright-cli`** (see `playwright-cli` skill
   for full command reference):
   ```bash
   playwright-cli open <web-url>
   # navigate to the page/state you changed
   playwright-cli click <ref>          # e.g. open the page under test
   playwright-cli screenshot --filename=/tmp/pr-screenshots/<feature>-after.png
   playwright-cli close
   ```
   - Use descriptive filenames: `<feature>-before.png` / `<feature>-after.png`,
     or `<feature>-<state>.png` (e.g. `dispatch-console-mobile.png`).
   - Save to a scratch directory (e.g. `/tmp/pr-screenshots/`), not inside the
     repo — screenshots are transient artifacts, not committed files.
   - Use `--hires` for crisper images when detail matters; use `resize` first
     for a specific viewport (desktop vs mobile) if the change is
     responsive-layout related.
   - For before/after comparisons, capture "before" on `main` (or before your
     change) first if feasible, then capture "after" once the change is made.

3. **Attach screenshots to the PR** using the `github-pr-media` skill:
   - Upload each image via GitHub's user-attachments API and embed the
     resulting markdown image link in the PR description (`gh pr edit --body`)
     or as a PR comment (`gh pr comment`).
   - Group multiple images under clear headings, e.g. `### Before` / `### After`,
     or one heading per page/feature.

4. **Clean up** local screenshot files from the scratch directory once
   attached (they're not part of the repo and are already gitignored under
   `.playwright-cli/` for any Playwright working files).

## Example

```bash
# 1. find the web URL (after `aspire run` in ProgressHomeHeating.AppHost)
aspire ps

# 2. capture
mkdir -p /tmp/pr-screenshots
playwright-cli open https://localhost:7123/dashboard
playwright-cli screenshot --filename=/tmp/pr-screenshots/dashboard-after.png
playwright-cli close

# 3. attach (see github-pr-media skill for the exact upload + embed steps)
gh pr comment 42 --body "### After\n![dashboard](<uploaded-asset-url>)"
```

## Related skills

- `playwright-cli` — full browser automation command reference
- `aspire-orchestration` — start/stop/wait on the AppHost
- `aspire-monitoring` — find resource URLs, logs, `aspire ps`
- `github-pr-media` — uploading an image to GitHub and embedding it in a PR
- `pr-read-github` — reading/locating the target PR
