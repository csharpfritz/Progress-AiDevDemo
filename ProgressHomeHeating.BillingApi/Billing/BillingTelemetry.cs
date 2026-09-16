using System.Diagnostics;

namespace ProgressHomeHeating.BillingApi.Billing;

public static class BillingTelemetry
{
    public const string ActivitySourceName = "ProgressHomeHeating.Billing";

    public static readonly ActivitySource Source = new(ActivitySourceName);
}
