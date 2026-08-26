using Microsoft.EntityFrameworkCore;
using ProgressHomeHeating.Contracts;
using ProgressHomeHeating.OperationsApi.Data;

namespace ProgressHomeHeating.OperationsApi.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        static IQueryable<DeliveryOrder> WithDetails(AppDbContext db) =>
            db.Orders.Include(o => o.Customer).Include(o => o.Driver).Include(o => o.Truck).AsNoTracking();

        group.MapGet("/", async (DateOnly? from, DateOnly? to, AppDbContext db) =>
        {
            var query = WithDetails(db);
            if (from is not null) query = query.Where(o => o.ScheduledDate >= from);
            if (to is not null) query = query.Where(o => o.ScheduledDate <= to);
            return (await query.OrderBy(o => o.ScheduledDate).ToListAsync()).Select(o => o.ToDto());
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
            await WithDetails(db).FirstOrDefaultAsync(o => o.Id == id) is { } order
                ? Results.Ok(order.ToDto())
                : Results.NotFound());

        // The agent's "schedule a delivery" tool target.
        group.MapPost("/", async (CreateDeliveryOrderRequest request, AppDbContext db) =>
        {
            var tank = await db.Tanks.FindAsync(request.TankId);
            if (tank is null || tank.CustomerId != request.CustomerId)
                return Results.BadRequest("Tank does not belong to the specified customer.");

            var order = new DeliveryOrder
            {
                CustomerId = request.CustomerId,
                TankId = request.TankId,
                ScheduledDate = request.ScheduledDate,
                GallonsRequested = request.GallonsRequested,
                DriverId = request.DriverId,
                TruckId = request.TruckId,
                Status = request.DriverId is not null ? DeliveryStatus.Scheduled : DeliveryStatus.Requested,
                CreatedBy = request.CreatedBy
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var created = await WithDetails(db).FirstAsync(o => o.Id == order.Id);
            return Results.Created($"/api/orders/{order.Id}", created.ToDto());
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateDeliveryOrderRequest request, AppDbContext db) =>
        {
            var order = await db.Orders.FindAsync(id);
            if (order is null) return Results.NotFound();

            if (request.ScheduledDate is not null) order.ScheduledDate = request.ScheduledDate.Value;
            if (request.Status is not null) order.Status = request.Status.Value;
            if (request.DriverId is not null) order.DriverId = request.DriverId;
            if (request.TruckId is not null) order.TruckId = request.TruckId;
            if (request.GallonsDelivered is not null) order.GallonsDelivered = request.GallonsDelivered;

            await db.SaveChangesAsync();
            var updated = await WithDetails(db).FirstAsync(o => o.Id == id);
            return Results.Ok(updated.ToDto());
        });

        // Demo reset: removes every order the AI agent has created (CreatedBy = "agent"),
        // leaving seeded/manually-created orders untouched, so the dispatch demo can be re-run.
        group.MapDelete("/agent-runs", async (AppDbContext db) =>
        {
            var deleted = await db.Orders.Where(o => o.CreatedBy == "agent").ExecuteDeleteAsync();
            return Results.Ok(new { deleted });
        });
    }
}
