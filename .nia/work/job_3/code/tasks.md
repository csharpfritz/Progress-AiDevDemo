# Issue 3 — Task Checklist

Single phase. Execute in order; each task depends on the one before it.

## Phase 1 — Customer Detail Modal

- [x] **T1** — Widen `CustomerRow` record in `Customers.razor` to carry `CustomerId`, `CustomerDto`, and `OilTankDto?`
- [x] **T2** — Load delivery orders in `OnInitializedAsync` and build the `ordersByCustomer` lookup
- [x] **T3** — Add modal state fields (`detailVisible`, `selectedRow`, `selectedOrders`) and the `OpenDetails` / `CloseDetails` handlers
- [x] **T4** — Wire `OnRowClick` on the `TelerikGrid` and add a "View Details" command column
- [x] **T5** — Add the `TelerikWindow` modal markup with customer, tank, and order-history sections
- [x] **T6** — Add tank-level visual indicator and quick-action button in the modal footer
- [x] **T7** — Add supporting CSS in `wwwroot/css/theme.css` (skipped — Bootstrap utility classes were sufficient, no overflow/cramping observed in the markup)
- [x] **T8** — Build the solution and fix any compilation errors (`dotnet build ProgressHomeHeating.slnx` — 0 errors, 1 pre-existing unrelated warning)
- [x] **T9** — Run the Telerik validator on `Customers.razor` (all component properties valid)
- [~] **T10** — Manually verify all acceptance criteria in the browser — **partially blocked**: `aspire start` brings up `operationsapi`/`postgres` fine, but `web` waits on `agentapi`, which in turn waits on `azure-openai-endpoint` / `azure-openai-api-key` / `azure-openai-deployment-name` parameters that have no value in this environment (pre-existing, unrelated to this change). Could not reach a live `/customers` page to click through the 11-point checklist. Code review of the markup confirms all wiring matches the T5/T6 acceptance criteria (row click → `OpenDetails`, `Modal="true"` + `CloseOnOverlayClick="true"` + `WindowAction Name="Close"` for the three close paths, `Filterable="false" Sortable="false"` command column preserving grid behavior).

## Acceptance Criteria (from Issue 3)

- [x] Clicking a customer in the grid opens a modal window with that customer's details (via `OnRowClick` and "View Details" column, both calling `OpenDetails`)
- [x] Modal displays customer profile and tank status information (`WindowContent` sections)
- [x] Modal closes cleanly via close button, backdrop, or Escape key (`Modal="true"`, `CloseOnOverlayClick="true"`, `WindowAction Name="Close"`, footer `Close` button)
- [x] Existing grid functionality (pagination, filter row, sorting) continues to work (grid columns/props unchanged; new column is non-filterable/non-sortable)

**Note:** Items above are verified by code inspection and successful build/validator run.
Live end-to-end browser verification (T10) should be re-run once `agentapi`'s Azure OpenAI
parameters are configured in this environment.
