---
name: ticket-read-github
description: >
  Retrieves and reads support tickets from GitHub Issues (ticket workflow).
  Use when working with GitHub tickets, or when the user mentions
  reading, viewing, or retrieving support requests.
version: "1.0.0"
---

# GitHub Ticket Retrieval

This skill guides you through retrieving and reading GitHub issues used as support tickets.

## Prerequisites

- gh CLI installed and authenticated
- Access to the support repository

## Basic Commands

### List Tickets

```bash
# List open tickets
gh issue list

# Filter by labels
gh issue list --label "customer-support"
gh issue list --label "high-priority"

# Filter by assignee
gh issue list --assignee @me
```

### View Ticket

```bash
# View ticket details
gh issue view 123

# View with all comments
gh issue view 123 --comments

# Output as JSON
gh issue view 123 --json title,body,comments,labels
```

## Best Practices

1. **Check full conversation history**: Support tickets have back-and-forth communication.
2. **Review attachments**: Customers often attach screenshots or logs in comments.
3. **Check priority and SLA**: Note urgency via labels and response time requirements.
4. **Review customer context**: Check issue author and previous interactions.

## Placeholders

- `{{ticket_id}}` - The ticket number
- `{{ticket_tracker_repo_slug}}` - The repository (owner/repo format)
