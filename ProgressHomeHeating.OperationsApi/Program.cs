using Microsoft.EntityFrameworkCore;
using ProgressHomeHeating.OperationsApi.Data;
using ProgressHomeHeating.OperationsApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.AddNpgsqlDbContext<AppDbContext>("operationsdb");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();

app.MapCustomerEndpoints();
app.MapTankEndpoints();
app.MapFleetEndpoints();
app.MapOrderEndpoints();

app.Run();
