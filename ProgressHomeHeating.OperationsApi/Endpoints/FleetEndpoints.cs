using Microsoft.EntityFrameworkCore;
using ProgressHomeHeating.OperationsApi.Data;

namespace ProgressHomeHeating.OperationsApi.Endpoints;

public static class FleetEndpoints
{
    public static void MapFleetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/drivers", async (AppDbContext db) =>
            (await db.Drivers.AsNoTracking().ToListAsync()).Select(d => d.ToDto()))
            .WithTags("Fleet");

        app.MapGet("/api/trucks", async (AppDbContext db) =>
            (await db.Trucks.AsNoTracking().ToListAsync()).Select(t => t.ToDto()))
            .WithTags("Fleet");
    }
}
