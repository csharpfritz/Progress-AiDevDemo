---
name: ticket-respond-github
description: >
  Provides guidance for responding to support tickets in GitHub Issues.
  Use when preparing responses to customer requests or support tickets.
version: "1.0.0"
---

# GitHub Ticket Response

This skill guides you through responding to support tickets in GitHub Issues.

## Response Format

### Add Comment

```bash
# Add response comment
gh issue comment 123 --body "Thank you for reporting this issue..."
```

### Update Status

```bash
# Close ticket when resolved
gh issue close 123 --comment "This issue has been resolved."

# Reopen if needed
gh issue reopen 123
```

## Response Guidelines

1. **Acknowledge the issue**: Start by confirming you understand the problem.
2. **Be clear and concise**: Avoid jargon; write for the customer's level.
3. **Provide actionable steps**: Give clear next steps or solutions.
4. **Set expectations**: If follow-up is needed, explain timing.
5. **Professional tone**: Maintain helpful, professional communication.

## Response Template

```markdown
Hello @username,

Thank you for contacting us. [Acknowledge their issue]

[Explanation or solution]

To resolve this:
1. [Step 1]
2. [Step 2]
3. [Step 3]

[Set expectations for follow-up if needed]

Please let us know if you have any questions.
```

## Placeholders

- `{{ticket_id}}` - The ticket number
