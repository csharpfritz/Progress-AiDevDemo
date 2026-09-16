using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ProgressHomeHeating.BillingApi.Billing;

public sealed class BillingStoreHealthCheck(IBillingStore store) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy(
            "Billing store ready.",
            new Dictionary<string, object> { ["seededAccounts"] = store.AccountCount }));
}
