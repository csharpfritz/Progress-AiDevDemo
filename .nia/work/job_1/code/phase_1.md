# Phase 1: Complete Delivery Feature

## Overview

Implement the "Mark Tank as Filled" feature by adding an atomic delivery completion endpoint that updates both the delivery order status and the customer's tank level in a single transaction.

---

## Task 1.1: Add CompleteDeliveryResponse Contract

**File**: `ProgressHomeHeating.Contracts/DeliveryOrderContracts.cs`

**Description**: Add a response record that combines the updated delivery order and tank information.

**Implementation**:
```csharp
public record CompleteDeliveryResponse(DeliveryOrderDto Order, OilTankDto Tank);
```

**Location**: Add after `CompleteDeliveryOrderRequest` (line 34)

**Dependencies**: None

**Acceptance Criteria**:
- [ ] Record compiles without errors
- [ ] Contains both `DeliveryOrderDto` and `OilTankDto` properties

---

## Task 1.2: Add Complete Delivery Endpoint

**File**: `ProgressHomeHeating.OperationsApi/Endpoints/OrderEndpoints.cs`

**Description**: Add a POST endpoint to complete a delivery, updating the order status and tank level atomically.

**Implementation**:
```csharp
group.MapPost("/{id:guid}/complete", async (Guid id, CompleteDeliveryOrderRequest request, AppDbContext db) =>
{
    var order = await db.Orders
        .Include(o => o.Customer)
        .Include(o => o.Driver)
        .Include(o => o.Truck)
        .FirstOrDefaultAsync(o => o.Id == id);
    
    if (order is null)
        return Results.NotFound("Delivery order not found.");
    
    if (order.Status == DeliveryStatus.Delivered)
        return Results.BadRequest("Order has already been delivered.");
    
    if (order.Status == DeliveryStatus.Cancelled)
        return Results.BadRequest("Cannot complete a cancelled order.");
    
    var tank = await db.Tanks.Include(t => t.Customer).FirstOrDefaultAsync(t => t.Id == order.TankId);
    if (tank is null)
        return Results.BadRequest("Tank not found for this order.");
    
    // Update order
    order.Status = DeliveryStatus.Delivered;
    order.GallonsDelivered = request.GallonsDelivered;
    
    // Calculate new tank level
    var gallonsAdded = request.GallonsDelivered;
    var percentAdded = (double)gallonsAdded / tank.SizeGallons * 100;
    tank.CurrentLevelPercent = Math.Min(100, tank.CurrentLevelPercent + percentAdded);
    
    await db.SaveChangesAsync();
    
    return Results.Ok(new CompleteDeliveryResponse(order.ToDto(), tank.ToDto()));
});
```

**Location**: Add before the `MapDelete("/agent-runs", ...)` endpoint (around line 72)

**Dependencies**: Task 1.1 (CompleteDeliveryResponse contract must exist)

**Edge Cases Handled**:
- Order not found → 404 NotFound
- Order already delivered → 400 BadRequest
- Order cancelled → 400 BadRequest
- Tank not found → 400 BadRequest
- Tank overfill → Capped at 100%

**Acceptance Criteria**:
- [ ] Endpoint responds to `POST /api/orders/{id}/complete`
- [ ] Sets order status to `Delivered`
- [ ] Records `GallonsDelivered` on the order
- [ ] Updates tank's `CurrentLevelPercent` correctly
- [ ] Returns 404 for non-existent orders
- [ ] Returns 400 for already-delivered orders
- [ ] Returns 400 for cancelled orders
- [ ] Tank level capped at 100%

---

## Task 1.3: Add Client Method

**File**: `ProgressHomeHeating.Web/Services/OperationsApiClient.cs`

**Description**: Add a method to call the complete delivery endpoint from the web frontend.

**Implementation**:
```csharp
public async Task<CompleteDeliveryResponse?> CompleteDeliveryAsync(Guid orderId, int gallonsDelivered, CancellationToken ct = default)
{
    var request = new CompleteDeliveryOrderRequest(gallonsDelivered);
    var response = await http.PostAsJsonAsync($"/api/orders/{orderId}/complete", request, ct);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<CompleteDeliveryResponse>(ct);
}
```

**Location**: Add after `UpdateOrderAsync` method (around line 41)

**Dependencies**: Task 1.1 (CompleteDeliveryResponse contract must exist)

**Acceptance Criteria**:
- [ ] Method compiles without errors
- [ ] Accepts orderId and gallonsDelivered parameters
- [ ] Calls correct endpoint path
- [ ] Returns `CompleteDeliveryResponse`

---

## Task 1.4: Build Verification

**Command**: `dotnet build ProgressHomeHeating.slnx`

**Dependencies**: Tasks 1.1, 1.2, 1.3

**Acceptance Criteria**:
- [ ] Solution builds without errors
- [ ] No new warnings introduced

---

## Task 1.5: Add HTTP Test Request

**File**: `ProgressHomeHeating.OperationsApi/ProgressHomeHeating.OperationsApi.http`

**Description**: Add a test request for the new endpoint.

**Implementation**:
```http
### Complete a delivery order
POST {{host}}/api/orders/{{orderId}}/complete
Content-Type: application/json

{
    "gallonsDelivered": 150
}
```

**Dependencies**: Task 1.2

**Acceptance Criteria**:
- [ ] Test request added to .http file
- [ ] Can be executed successfully against running API

---

## Dependency Graph

```
Task 1.1 (Contract)
    ↓
Task 1.2 (Endpoint) ←─── Task 1.5 (.http test)
    ↓
Task 1.3 (Client)
    ↓
Task 1.4 (Build)
```

---

## Testing Strategy

### Manual Testing Steps

1. Start the Aspire AppHost
2. Use the `.http` file or curl to:
   - Create a test delivery order (if needed)
   - Call `POST /api/orders/{id}/complete` with `gallonsDelivered`
   - Verify response contains updated order (status = Delivered) and tank (new level)
3. Call `GET /api/tanks` to confirm tank level persisted
4. Call `GET /api/orders/{id}` to confirm order status persisted

### Expected Behavior

| Scenario | Input | Expected Result |
|----------|-------|-----------------|
| Normal completion | Order in Scheduled status, 150 gallons | Status → Delivered, Tank level increased |
| Tank near full | Tank at 90%, add 50 gallons to 275gal tank | Tank capped at 100% |
| Already delivered | Order with status Delivered | 400 Bad Request |
| Cancelled order | Order with status Cancelled | 400 Bad Request |
| Invalid order ID | Non-existent GUID | 404 Not Found |
