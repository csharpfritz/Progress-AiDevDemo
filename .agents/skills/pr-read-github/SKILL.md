---
name: pr-read-github
description: >
  Retrieves and reads pull requests from GitHub using gh CLI.
  Use when working with GitHub PRs, or when the user mentions reading,
  viewing, or retrieving pull requests from GitHub.
version: "1.0.0"
---

# GitHub Pull Request Retrieval

This skill guides you through retrieving and reading GitHub pull requests using the gh CLI tool.

## Prerequisites

- gh CLI installed and authenticated
- Access to the target repository

## Basic Commands

### List Pull Requests

```bash
# List open PRs
gh pr list

# List PRs with specific state
gh pr list --state merged
gh pr list --state closed
gh pr list --state all

# Filter by author
gh pr list --author @me
gh pr list --author username

# Filter by base branch
gh pr list --base main
```

### View Pull Request

```bash
# View PR by number
gh pr view 123

# View with specific details
gh pr view 123 --comments

# Output as JSON
gh pr view 123 --json title,body,files,commits,reviews
```

### View PR Diff

```bash
# View the diff
gh pr diff 123

# View files changed
gh pr view 123 --json files
```

### Cross-Repository Access

```bash
gh pr list --repo owner/repo
gh pr view 123 --repo owner/repo
```

## Best Practices

1. **Review the description**: PR descriptions explain the purpose and approach.
2. **Check review status**: Look for approvals, change requests, and comments.
3. **Review CI status**: Check if tests are passing.
4. **Check linked issues**: PRs often link to issues they address.
5. **Review commits**: Understand the change history.

## Common Patterns

### Get Full PR Context

```bash
gh pr view 123 --json title,body,files,commits,reviews,comments
```

### Check Merge Readiness

```bash
gh pr checks 123
gh pr view 123 --json mergeable,reviewDecision
```

## Placeholders

- `{{code_platform_repo_slug}}` - The repository (owner/repo format)
