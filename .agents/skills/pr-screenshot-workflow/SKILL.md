---
name: pr-screenshot-workflow
description: >
  Capture Playwright CLI screenshots of the running ProgressHomeHeating app
  (Blazor Web frontend) and embed them in a GitHub pull request body as visual
  evidence of a feature/UI change. Use when the user asks to "screenshot the
  change", "show the UI change in the PR", "add before/after screenshots",
  "visually verify", or after implementing a UI feature that should be
  showcased in a PR.
version: "1.0.0"
---

# PR Screenshot Workflow

Standard workflow for this repo: implement a UI change -> start the app ->
capture screenshot(s) with Playwright CLI -> embed them in the PR body.

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

3. **Embed screenshots in the PR body.** GitHub CLI does not upload comment
   attachments directly, so use one of these approaches:
   - Preferred: upload through GitHub's web UI/user-attachments flow, then use
     `gh pr edit <number> --body` to add the returned image markdown under a
     `## Visual Evidence` heading.
   - CLI fallback: use authenticated `gh api` Git Data endpoints to create or
     update a dedicated `pr-assets-<number>` branch that contains only PNGs at
     `.github/pr-assets/pr-<number>/`. Reference each image through its
     `raw.githubusercontent.com` URL, then append the markdown to the PR body
     with `gh pr edit <number> --body`.
   - Preserve the existing PR description when appending evidence. Verify every
     image URL returns HTTP 200 and retrieve the PR body afterward to confirm
     the markdown was saved.

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

# 3. embed in the PR body after uploading the image
gh pr edit 42 --body "$(gh pr view 42 --json body --jq .body)

## Visual Evidence

![Dashboard](<uploaded-asset-url>)"
```

## Related skills

- `playwright-cli` — full browser automation command reference
- `aspire-orchestration` — start/stop/wait on the AppHost
- `aspire-monitoring` — find resource URLs, logs, `aspire ps`
- `pr-read-github` — reading/locating the target PR
