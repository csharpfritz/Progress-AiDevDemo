using ProgressHomeHeating.BillingApi.Billing;

namespace ProgressHomeHeating.BillingApi.Tests;

// Deterministically locates customer GUIDs that fall on each side of the seeder's
// "empty account" rule, so tests never depend on runtime-generated customer identifiers.
public static class TestCustomers
{
    public static Guid WithHistory(int skip = 0) => Find(hasHistory: true, skip);

    public static Guid WithoutHistory(int skip = 0) => Find(hasHistory: false, skip);

    private static Guid Find(bool hasHistory, int skip)
    {
        var found = 0;
        for (var i = 1; i < 100_000; i++)
        {
            var candidate = new Guid(i, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]);
            if (DeterministicBillingSeeder.IsEmptyAccount(candidate) == !hasHistory)
            {
                if (found++ == skip) return candidate;
            }
        }

        throw new InvalidOperationException("No matching test customer id found.");
    }
}
