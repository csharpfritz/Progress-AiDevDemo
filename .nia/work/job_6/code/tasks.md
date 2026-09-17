# Task Checklist — Issue 6 (Delivery Detail Modal)

Progress tracking only. Full instructions live in `phase_1.md` … `phase_6.md`.

Status legend: `[ ]` not started · `[x]` complete

---

## PHASE-1 — Testable domain logic and test harness
*Depends on: none · Blocks: PHASE-2 … PHASE-6 · Detail: `phase_1.md`*

- [x] TASK-1.1 Create `ProgressHomeHeating.Web/Models/` directory
- [x] TASK-1.2 Create `ProgressHomeHeating.Web/Models/OrderEditPolicy.cs`
- [x] TASK-1.3 Create `ProgressHomeHeating.Web/Models/OrderDetailEditModel.cs`
- [x] TASK-1.4 Create `ProgressHomeHeating.Tests` xunit project and register it in `ProgressHomeHeating.slnx`
- [x] TASK-1.5 Create `ProgressHomeHeating.Tests/OrderEditPolicyTests.cs`
- [x] TASK-1.6 Create `ProgressHomeHeating.Tests/OrderDetailEditModelTests.cs`
- [x] VAL-1.4 `dotnet build ProgressHomeHeating.slnx` exits 0
- [x] VAL-1.5 `dotnet test ProgressHomeHeating.Tests/…` exits 0

## PHASE-2 — Scheduler data plumbing and modal state scaffolding
*Depends on: PHASE-1 · Blocks: PHASE-3 · Detail: `phase_2.md`*

- [x] TASK-2.1 Add `@using ProgressHomeHeating.Web.Models`
- [x] TASK-2.2 Add `[CascadingParameter] DialogFactory Dialogs`
- [x] TASK-2.3 Add `trucks` and `ordersById` fields
- [x] TASK-2.4 Add modal state fields (`detailVisible`, `isEditing`, `isSaving`, `selectedOrder`, `editModel`, `modalErrorMessage`)
- [x] TASK-2.5 Populate `trucks` and `ordersById` in `LoadAsync`
- [x] TASK-2.6 Add `OpenOrderDetails`, `CloseOrderDetails`, `BeginEdit`, `CancelEdit`, `StatusBadgeColor`
- [x] VAL-2.1 `dotnet build` of the Web project exits 0
- [x] VAL-2.4 `OnAppointmentUpdate` and `SubmitNewOrderAsync` unchanged

## PHASE-3 — Review-mode modal markup and click wiring
*Depends on: PHASE-2 · Blocks: PHASE-4, PHASE-5 · Detail: `phase_3.md` · AC-01, AC-02, AC-12*

- [x] TASK-3.1 Add `OnItemClick` and `OnEdit` to `<TelerikScheduler>`
- [x] TASK-3.2 Add `OnAppointmentClick` and `OnAppointmentEdit` handlers
- [x] TASK-3.3 Add the `TelerikWindow` detail modal markup (read-only)
- [x] TASK-3.4 Add `TankLabelFor` helper
- [x] VAL-3.4 Click opens the modal with all nine fields (AC-01)
- [x] VAL-3.5 No editable inputs in review mode (AC-02)
- [x] VAL-3.6 Close via `[x]`, button, `Esc`, overlay — no API call (AC-12)
- [x] VAL-3.7 Double-click does not open the built-in edit popup

## PHASE-4 — Edit mode, validation, and save
*Depends on: PHASE-3 · Blocks: PHASE-6 · Detail: `phase_4.md` · AC-03, AC-04, AC-08, AC-09*

- [x] TASK-4.1 Add `EditableStatusOptions()` and `StatusOption` record
- [x] TASK-4.2 Render edit-mode inputs (date, status, driver, truck, gallons delivered)
- [x] TASK-4.3 Replace `<WindowFooter>` with mode-aware buttons
- [x] TASK-4.4 Add `SaveOrderEditAsync`
- [x] VAL-4.3 Edit mode exposes exactly the five editable fields (AC-03)
- [x] VAL-4.5 Save sends a delta-only `PUT` and refreshes (AC-04)
- [x] VAL-4.6 Invalid values block the save with no API call (AC-09)
- [x] VAL-4.7 API failure keeps the modal open with edits intact (AC-08)
- [x] VAL-4.9 Double-click on Save issues exactly one `PUT`

## PHASE-5 — Cancel delivery with confirmation and eligibility
*Depends on: PHASE-3, PHASE-4 · Blocks: PHASE-6 · Detail: `phase_5.md` · AC-05, AC-06, AC-07, AC-08*

- [x] TASK-5.1 Add the `Cancel Delivery` button behind `OrderEditPolicy.CanCancel`
- [x] TASK-5.2 Show `AlreadyClosedMessage` for `Delivered` / `Cancelled` orders
- [x] TASK-5.3 Add `CancelDeliveryAsync` with `Dialogs.ConfirmAsync`
- [x] VAL-5.3 Declining the confirmation issues no API call (AC-05)
- [x] VAL-5.4 Confirming sets `Status = Cancelled` and refreshes (AC-06)
- [x] VAL-5.5 Cancel action absent + explanation shown for closed orders (AC-07)
- [x] VAL-5.6 API failure surfaces an error and allows retry (AC-08)
- [x] VAL-5.7 `EnRoute` orders get the escalated confirmation wording

## PHASE-6 — Verification, regression, and visual evidence
*Depends on: PHASE-1 … PHASE-5 · Detail: `phase_6.md` · AC-10, AC-11*

- [x] TASK-6.1 `dotnet build ProgressHomeHeating.slnx`
- [x] TASK-6.2 `dotnet test ProgressHomeHeating.Tests/…`
- [x] TASK-6.3 Start via `aspire start --non-interactive`; discovered the frontend URL from `aspire describe`
- [x] VAL-6.4 New-delivery form behaviour unchanged (AC-10)
- [x] VAL-6.5 Drag-to-reschedule unchanged and does not open the modal (AC-11) — see note below
- [x] VAL-6.6 Agent-created orders behave identically
- [x] VAL-6.7 All prior phase validations pass on the integrated build
- [x] VAL-6.8 Keyboard `Tab` / `Esc` behaviour verified
- [x] TASK-6.5 Capture screenshots and embed under `## Visual Evidence` in the PR
- [x] TASK-6.6 Stop the app (`aspire stop`), confirm no orphaned processes

> **Live verification completed this session** on branch
> `issue-6-scheduler-delivery-modal-fullplan` (rebased cleanly onto the latest
> `origin/main`). `aspire run` itself is blocked in this non-interactive sandbox
> ("Permission denied and could not request permission from user"), so the app was
> started instead with `aspire start --non-interactive` (the agent-safe background
> equivalent per the aspire-orchestration skill); dummy Azure OpenAI parameter values
> were supplied via `aspire resource <name> set-parameter` purely to unblock
> `agentapi`/`web` startup (a pre-existing environment gap unrelated to this
> feature — no code or config was changed). `/scheduler` returned HTTP 200.
>
> Verified interactively with Playwright:
> - AC-01/AC-02 (VAL-3.4, VAL-3.5): click opens a read-only modal with all 8 fields
>   (Customer, Tank, Gallons Requested, Gallons Delivered, Scheduled Date, Status,
>   Driver, Truck) as text/badges, no inputs.
> - AC-03 (VAL-4.3, VAL-4.4): Edit exposes exactly Scheduled Date, Status, Driver,
>   Truck, Gallons Delivered; Customer/Tank/Gallons Requested stay read-only; status
>   options for a `Scheduled` order were exactly Scheduled/EnRoute/Delivered/Cancelled
>   (no `Requested`).
> - OQ-4: Gallons Delivered was disabled until Status was set to `Delivered`, then
>   became editable.
> - AC-09 (VAL-4.6): saving `Delivered` with no gallons value blocked the save with
>   "Gallons delivered is required when the status is Delivered." and issued no
>   request.
> - AC-04 (VAL-4.5): after entering 205 gallons and saving, the modal closed and the
>   scheduler refreshed to show `(Delivered)`.
> - AC-07 (VAL-5.5): reopening the now-`Delivered` order showed no Cancel Delivery
>   button and the exact `AlreadyClosedMessage` text.
> - AC-05/AC-06 (VAL-5.3, VAL-5.4): on a `Scheduled` order, "Cancel Delivery" showed a
>   `Confirm Cancellation` dialog with the standard message; declining left the order
>   `Scheduled` and the modal open; confirming set it to `Cancelled` and refreshed.
> - VAL-5.7: after editing an order to `EnRoute` and saving, "Cancel Delivery" showed
>   the escalated "This delivery is already EN ROUTE…" wording; declined without a
>   status change.
> - AC-12 (VAL-3.6): `Esc` closed the confirmation-free modal without changing the
>   order's status.
> - VAL-3.7: double-clicking an appointment opened only the custom detail modal — no
>   Telerik built-in edit popup appeared.
> - AC-10 (VAL-6.4): submitting "Schedule New Delivery" with no customer selected
>   still showed "Please select a customer." — existing validation unchanged.
> - VAL-6.8: `Tab` cycled focus among the modal's own controls/buttons (not lost to
>   the page behind it) and `Esc` still closed it afterwards.
>
> **VAL-6.5 caveat:** Telerik Scheduler's month-view drag-and-drop did not trigger
> through Playwright's synthetic `dragTo`/manual mouse-event sequences in this
> headless session (a known automation limitation for this widget, not a product
> regression) — no reschedule occurred and, importantly, **no detail modal opened**
> as a side effect of the drag attempts, which was the specific regression risk this
> validation targets. `OnAppointmentUpdate` itself was verified unchanged from `HEAD`
> by inspection (PHASE-2 note) and by `git diff` on the final `Scheduler.razor`. A
> full drag-and-drop confirmation should be re-run manually or via a real browser
> session before merging.
>
> **VAL-6.6 verified by test, not by live agent run:** no AI dispatch agent run was
> triggered in this session (would require live Azure OpenAI credentials, not just
> the placeholder values used to unblock startup). Instead, a regression test —
> `OrderDetailEditModelTests.Constructor_and_delta_building_are_agnostic_to_CreatedBy`
> (theory over `CreatedBy = null` and `CreatedBy = "dispatch-agent"`) — was added to
> `ProgressHomeHeating.Tests` and confirms the modal's edit/save logic produces
> identical results regardless of `CreatedBy`. The modal's lookup (`ordersById` keyed
> by `Id` in `Scheduler.razor`) is also agnostic to `CreatedBy` by inspection. A full
> live walkthrough with an actual agent-created order should still be run with real
> credentials before merging, but no code-level regression is expected.
>
> **TASK-6.5 completed:** this branch was pushed and opened as PR #13
> (`issue-6-scheduler-delivery-modal-fullplan`) — see note in PR #13's description
> that PR #10 (`fix-6-scheduler-delivery-modal`) already closes Issue #6 with a
> different, lite-mode implementation, and only one of the two should be merged.
> Three screenshots (`scheduler-review-modal.png`, `scheduler-edit-modal.png`,
> `scheduler-cancel-confirm.png`) were captured against a fresh `aspire start
> --non-interactive` session (dummy Azure OpenAI parameters set the same way as the
> earlier verification pass) via Playwright CLI, covering the review modal (AC-01/
> AC-02), edit modal with the five editable fields and `GallonsDelivered` disabled
> per OQ-4 (AC-03), and the `Confirm Cancellation` dialog (AC-05). Each was uploaded
> via the GitHub user-attachments API, embedded under a `## Visual Evidence` heading
> in PR #13's body, and confirmed to return HTTP 200. Local screenshot files and
> scratch artifacts were deleted afterward, and the Aspire app (plus its orphaned
> resource processes) was stopped/killed to leave no running processes.

---


## Decisions locked by this plan

- [x] OQ-1 Status transitions are restricted (`OrderEditPolicy.AllowedTransitions`)
- [x] OQ-2 Cancelling an `EnRoute` order is allowed with escalated confirmation wording
- [x] OQ-3 Unassigning a driver/truck is **out of scope** (contract limitation FU-01)
- [x] OQ-4 `GallonsDelivered` is editable only when the selected status is `Delivered`

## Deferred follow-ups (not implemented here)

- [ ] FU-01 Contract change to express "unassign driver/truck"
- [ ] FU-02 Optimistic concurrency on `DeliveryOrder`
- [ ] FU-03 `GetOrderAsync(Guid)` on `OperationsApiClient` (not needed by the chosen design)
