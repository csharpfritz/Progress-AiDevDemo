# Issue #1: Mark Customer's Tank as Filled

## Overview

When a delivery driver completes their run, we need to mark the customer's tank as filled and record the new fill level when the driver departed.

## Implementation Approach

This feature requires a **single atomic operation** that:
1. Marks the delivery order as `Delivered`
2. Records gallons delivered
3. Updates the tank's current level percentage

### Key Design Decisions

- **New "Complete Delivery" endpoint**: Add `POST /api/orders/{id}/complete` to handle the atomic completion operation
- **Calculate new tank level**: Based on gallons delivered and tank capacity
- **Existing contracts are sufficient**: `CompleteDeliveryOrderRequest` already exists with `GallonsDelivered`
- **Add new contract**: `CompleteDeliveryResponse` to return both order and tank state

## Files to Modify

| File | Change |
|------|--------|
| `ProgressHomeHeating.Contracts/DeliveryOrderContracts.cs` | Add `CompleteDeliveryResponse` record |
| `ProgressHomeHeating.OperationsApi/Endpoints/OrderEndpoints.cs` | Add `POST /api/orders/{id}/complete` endpoint |
| `ProgressHomeHeating.Web/Services/OperationsApiClient.cs` | Add `CompleteDeliveryAsync` method |

## Validation Criteria

- [ ] Completing a delivery updates order status to `Delivered`
- [ ] Completing a delivery records `GallonsDelivered` on the order
- [ ] Completing a delivery updates tank's `CurrentLevelPercent`
- [ ] Tank level calculation: `min(100, previous% + (gallonsDelivered / tankCapacity * 100))`
- [ ] Returns both updated order and tank information
- [ ] Validates order exists and is in valid state for completion
- [ ] Validates tank exists for the order
