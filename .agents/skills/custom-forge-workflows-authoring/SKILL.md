---
name: custom-forge-workflows-authoring
description: Guide for authoring and customizing Nia stateful workflows at the project (repository) level. Use when creating, editing, or debugging TOML workflow definitions under .nia/config/workflows/, adding states/transitions/loops/approval gates, or running/validating workflows with the `nia workflow` CLI.
---

# Authoring Custom Stateful Workflows in Nia (Project Scope)

This guide explains how a repository defines and customizes **stateful
workflows** at the **project (repository) level** — TOML files under
`.nia/config/workflows/` that chain multiple commands/steps/checks into a
multi-state automation pipeline, run with `nia workflow run <name>`.

---

## 1. Where Workflow Files Live

```
.nia/
└── config/
    └── workflows/         # Stateful workflow definitions
        ├── my-workflow.toml
        └── ...
```

Each file defines exactly one workflow. `workflows/*.toml` supports
hierarchical loading (Repository > Application > User > System), but a
repository's own `.nia/config/workflows/*.toml` always takes highest priority
regardless of external-source settings. See
[hierarchical.md](user-docs/src/configuration/hierarchical.md) for merge
rules.

### Bootstrapping

```bash
nia config export --workflows    # Scaffold example workflow file(s)
nia config validate               # Validate after editing
nia config lock                   # Optional: hash config for drift detection/CI
```

---

## 2. Minimal Skeleton

```toml
workflow_schema_version = "1.0.0"

[workflow]
name = "simple-workflow"
description = "A simple linear workflow example"
version = "1.0.0"
required_context = ["issue_id"]   # or [] if no context is required

[workflow.initial_state]
name = "start"

[[workflow.states]]
name = "start"
description = "Create issue draft"
command = { target = "issue", operation = "draft" }
on_success = "review"
on_failure = "draft_failed"

[[workflow.states]]
name = "review"
description = "Review the issue draft"
command = { target = "issue", operation = "review" }
on_success = "completed_success"
on_failure = "review_failed"

# Terminal states — no on_success/on_failure
[[workflow.states]]
name = "completed_success"

[[workflow.states]]
name = "draft_failed"

[[workflow.states]]
name = "review_failed"

[workflow.terminal_states]
success = ["completed_success"]
failed = ["draft_failed", "review_failed"]
```

Save it as `.nia/config/workflows/simple-workflow.toml`, then:

```bash
nia workflow list                     # confirm it's discovered
nia workflow validate simple-workflow # structural + reachability checks
nia workflow run simple-workflow      # execute
```

## 3. Workflow-Level Fields

| Field | Required | Description |
|---|---|---|
| `workflow_schema_version` | Yes | Currently `"1.0.0"` |
| `workflow.name` | Yes | Unique identifier, used in `nia workflow run <name>` |
| `workflow.description` | Yes | Human-readable summary |
| `workflow.version` | Yes | Semantic version of the workflow definition |
| `workflow.required_context` | Yes | Context that must be set before running: `issue_id`, `pr_id`, `ticket_id` (use `[]` if none) |
| `workflow.initial_state.name` | Yes | Starting state |
| `workflow.terminal_states` | No (recommended) | Explicit `success`/`failed`/`cancelled` state lists — no inference from naming |
| `workflow.loop_detection` | No | Overrides default loop-safety thresholds (see below) |
| `workflow.states` | Yes | Array of state definitions |

## 4. States & Transitions

Each `[[workflow.states]]` entry is one step. A state must define exactly one
of: `operation` (single), `operations` (sequence), `command` (legacy
single-command), or `approval`.

```toml
[[workflow.states]]
name = "create_code"
description = "Generate code from plan"
command = { target = "code", operation = "create" }
on_success = "check_tasks"
on_failure = "retry_code_create"
```

- `on_success` / `on_failure` — next state name for each outcome.
- States with **neither** transition are terminal.
- `is_exit_point = true` certifies a state as a valid `--ends-at` target for
  `nia workflow run <name> --ends-at <state>` (stops just before entering it).

**Approval gates** pause for a human decision:

```toml
[[workflow.states]]
name = "await_draft_approval"
on_success = "plan_implementation"
on_failure = "draft_rejected"

[workflow.states.approval]
gate_id = "draft_approval"
message = "Issue draft is ready. Review & edit before approving."
timeout_seconds = 86400
required_code = "PROCEED"   # optional confirmation phrase
```

## 5. Operations: step / check / command

The concrete kind is inferred from the fields present (there is no `kind`
field — don't write `type = "step"`/`"check"`):

- **Command** — has a `target` field: `{ target = "issue", operation = "draft" }`
- **Step** — `type` is `"shell"`, `"builtin"`, or `"agent"`
- **Check** — `type` is one of the check types below

```toml
# Step: shell
operation = { id = "run-tests", type = "shell", command = "cargo test" }

# Step: builtin
operation = { id = "create-output", type = "builtin", action = "make_directory", path = "output" }

# Step: agent
operation = { id = "send-reply", type = "agent", prompt = "Send the initial response.", context = ["ticket"] }

# Check
operation = { id = "config-exists", type = "file_exists", path = ".nia/config.toml", on_false = "fail" }
```

Multiple operations run in order in a single state via `operations = [...]`;
the first failure stops the state and triggers `on_failure`.

Common check types: `file_exists`, `directory_exists`, `path_exists`,
`env_exists`, `env_equals`, `file_contains`, `file_matches`,
`command_exists`, `command_success` (supports `auto_detect = "build" | "test"`
sourced from `project.toml`'s `build_command`/`test_command`),
`tasks_complete` (scans `tasks.md` for unchecked task-section items),
`counter_matches` (arithmetic on loop counters, e.g. `"% 3 == 0"`).

> Workflow-level pre/post steps orchestrate the workflow as a whole, distinct
> from per-command hooks defined in `commands.toml`. See
> [Workflow Steps](user-docs/src/reference/workflow-steps.md).

## 6. Loops & Retries

```toml
[[workflow.states]]
name = "create_code"
loop_enabled = true
loop_counter = "code_iterations"
max_visits = 30                       # per-state override of loop_detection
command = { target = "code", operation = "create" }
on_success = "check_tasks"
on_failure = "retry_code_create"

[[workflow.states.escape_conditions]]
counter_value = 10
action = "approval"
approval_gate = "await_iteration_approval"
approval_message = "Code generation has required 10 iterations. Review progress."
```

Global safety net (defaults: `max_state_visits = 3`, `max_transitions = 100`):

```toml
[workflow.loop_detection]
max_state_visits = 10
max_transitions = 200
on_loop_detected = "approval_gate"   # or "fail"
```

Loop counters are exposed as `NIA_LOOP_COUNTER_{NAME}` env vars for use in
shell steps/checks.

## 7. Validating & Running

```bash
nia workflow list [--verbose]
nia workflow validate <name>
nia workflow run <name> [--start-from <state>] [--bypass-approvals] [--dry-run] [--ends-at <state>]
nia workflow status <name>
nia workflow graph <name>         # visualize as a diagram
nia workflow disable|enable <name>
```

```bash
nia config validate                                       # validate merged config
nia config validate --file .nia/config/workflows/x.toml   # validate one file
nia config lock                                            # hash sources -> .nia/.config_lock (drift detection)
```

- Commit `.nia/config/workflows/` to version control so workflows are shared
  across the team.
- Run `nia config validate` (or `nia workflow validate <name>`) after every
  edit; run it in CI to catch drift.
- In CI/CD, consider `NIA_DISABLE_EXTERNAL_CONFIGS=true` to force
  repository-only config, guaranteeing reproducible builds independent of any
  user/system-level overrides.

---

## 8. Built-in Workflows (reference examples)

The repository's own built-in workflows in [configs/workflows/](configs/workflows) are good
templates to copy from: `code-to-review`, `issue-to-plan`, `issue-to-pr`,
`issue-to-pr-lite`, `issue-to-review`, `issue-to-review-lite`,
`pr-create-publish`, `pr-review-merge`, `pr-to-merge`, `ticket-to-response`.

---

## 9. Security Notes

- **Shell steps/checks** (`type = "shell"`, `command_success`) execute with
  your user's permissions and can see environment variables — never
  interpolate untrusted/unsanitized input into `command`.
- **External config sources** (user/system tiers) are disabled by default
  (`[config.external_sources]`); only enable them for sources you trust, and
  review their contents first.

---

## 10. Further Reading

- [references/issue-to-pr.toml](references/issue-to-pr.toml) — a
  fully annotated example workflow demonstrating states, transitions, loops,
  and approval gates; use it as a starting template when authoring a new
  workflow.
- [references/issue-to-pr-lite.toml](references/issue-to-pr-lite.toml) — a
  streamlined variant with fewer approval gates and `--lite` plan/review
  modifiers, suited to tutorials and simple, low-risk issues; use it as a
  starting template when a full review-gated workflow is overkill.


