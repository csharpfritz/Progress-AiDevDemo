# Implementation Tasks

## Phase 1: Complete Delivery Feature

- [ ] **Task 1.1**: Add `CompleteDeliveryResponse` contract
  - File: `ProgressHomeHeating.Contracts/DeliveryOrderContracts.cs`
  - Add record combining `DeliveryOrderDto` and `OilTankDto`

- [ ] **Task 1.2**: Add `/api/orders/{id}/complete` endpoint
  - File: `ProgressHomeHeating.OperationsApi/Endpoints/OrderEndpoints.cs`
  - POST endpoint accepting `CompleteDeliveryOrderRequest`
  - Update order status to `Delivered` and set `GallonsDelivered`
  - Calculate and update tank's `CurrentLevelPercent`
  - Return `CompleteDeliveryResponse`

- [ ] **Task 1.3**: Add `CompleteDeliveryAsync` client method
  - File: `ProgressHomeHeating.Web/Services/OperationsApiClient.cs`
  - Add method to call the new endpoint

- [ ] **Task 1.4**: Verify build succeeds
  - Run `dotnet build` on solution

- [ ] **Task 1.5**: Manual testing
  - Test endpoint via `.http` file or curl
  - Verify order status changes to Delivered
  - Verify tank level is updated correctly
