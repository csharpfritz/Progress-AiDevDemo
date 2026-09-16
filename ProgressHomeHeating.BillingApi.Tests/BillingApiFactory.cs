using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProgressHomeHeating.BillingApi.Tests;

public sealed class BillingApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Billing:SeedAnchorDate"] = "2026-03-01",
                ["Billing:DeclineCents"] = "13",
                ["Billing:ProcessingErrorCents"] = "42"
            });
        });
    }
}
