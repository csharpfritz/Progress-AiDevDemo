using OpenTelemetry;
using OpenTelemetry.Trace;
using ProgressHomeHeating.BillingApi.Billing;
using ProgressHomeHeating.BillingApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.Services.Configure<BillingOptions>(builder.Configuration.GetSection(BillingOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<FauxPaymentProcessor>();
builder.Services.AddSingleton<IBillingStore, InMemoryBillingStore>();

builder.Services.AddHealthChecks()
    .AddCheck<BillingStoreHealthCheck>("billing-store");

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(BillingTelemetry.ActivitySourceName));

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapBillingEndpoints();

app.Run();

// Exposed so ProgressHomeHeating.BillingApi.Tests can use WebApplicationFactory<Program>.
public partial class Program;
