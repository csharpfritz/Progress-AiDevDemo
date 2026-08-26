using Microsoft.EntityFrameworkCore;
using ProgressHomeHeating.Contracts;
using ProgressHomeHeating.OperationsApi.Data;

namespace ProgressHomeHeating.OperationsApi.Endpoints;

public static class TankEndpoints
{
    public static void MapTankEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tanks").WithTags("Tanks");

        group.MapGet("/", async (Guid? customerId, AppDbContext db) =>
        {
            var query = db.Tanks.Include(t => t.Customer).AsNoTracking().AsQueryable();
            if (customerId is not null)
                query = query.Where(t => t.CustomerId == customerId);
            return (await query.ToListAsync()).Select(t => t.ToDto());
        });

        // The agent's "who is at risk of running out of oil" tool target.
        group.MapGet("/low", async (double thresholdPercent, AppDbContext db) =>
            (await db.Tanks.Include(t => t.Customer)
                .Where(t => t.CurrentLevelPercent <= thresholdPercent)
                .AsNoTracking()
                .ToListAsync())
                .Select(t => t.ToDto()));

        group.MapPut("/{id:guid}/level", async (Guid id, UpdateTankLevelRequest request, AppDbContext db) =>
        {
            var tank = await db.Tanks.FindAsync(id);
            if (tank is null) return Results.NotFound();
            tank.CurrentLevelPercent = request.CurrentLevelPercent;
            await db.SaveChangesAsync();
            return Results.Ok(tank.ToDto());
        });
    }
}
