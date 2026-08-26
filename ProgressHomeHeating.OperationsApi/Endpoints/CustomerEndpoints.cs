using Microsoft.EntityFrameworkCore;
using ProgressHomeHeating.Contracts;
using ProgressHomeHeating.OperationsApi.Data;

namespace ProgressHomeHeating.OperationsApi.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", async (AppDbContext db) =>
            (await db.Customers.AsNoTracking().ToListAsync())
                .Select(c => c.ToDto()));

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
            await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id) is { } customer
                ? Results.Ok(customer.ToDto())
                : Results.NotFound());

        group.MapPost("/", async (CreateCustomerRequest request, AppDbContext db) =>
        {
            var customer = new Customer
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                ServiceAddress = new ServiceAddress
                {
                    Street = request.ServiceAddress.Street,
                    City = request.ServiceAddress.City,
                    State = request.ServiceAddress.State,
                    Zip = request.ServiceAddress.Zip
                }
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            return Results.Created($"/api/customers/{customer.Id}", customer.ToDto());
        });
    }
}
