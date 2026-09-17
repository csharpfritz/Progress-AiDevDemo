using Bogus;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.BillingApi.Billing;

public static class DeterministicBillingSeeder
{
    // Accounts whose derived seed satisfies this rule are seeded with no history and a zero balance,
    // so the Billing page's empty state is reachable deterministically (AC-003).
    public const int EmptyAccountModulus = 7;

    private static readonly string[] InvoiceDescriptions =
    [
        "Heating oil delivery",
        "Automatic delivery top-up",
        "Annual service plan",
        "Emergency delivery surcharge",
        "Burner maintenance visit"
    ];

    public static int DeriveSeed(Guid customerId)
    {
        var hash = 17;
        foreach (var b in customerId.ToByteArray())
        {
            hash = unchecked((hash * 31) + b);
        }

        return hash & 0x7FFFFFFF;
    }

    public static bool IsEmptyAccount(Guid customerId) =>
        DeriveSeed(customerId) % EmptyAccountModulus == 0;

    public static BillingAccountRecord Create(Guid customerId, DateOnly anchorDate)
    {
        var seed = DeriveSeed(customerId);
        var random = new Randomizer(seed);

        var account = new BillingAccountRecord
        {
            Id = $"acct_{customerId:N}",
            CustomerId = customerId
        };

        if (seed % EmptyAccountModulus == 0)
        {
            account.BalanceCents = 0;
            account.DueDate = null;
            return account;
        }

        var invoiceCount = random.Int(2, 6);
        var anchorUtc = new DateTimeOffset(anchorDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var balance = 0L;

        for (var i = invoiceCount; i >= 1; i--)
        {
            var amount = random.Int(9_000, 68_000);
            var createdAt = anchorUtc.AddDays(-30 * i);
            var isPaid = i > 1;

            account.Transactions.Add(new BillingTransactionRecord
            {
                Id = $"txn_{DeterministicGuid(seed, i * 2):N}",
                Type = BillingTransactionType.Invoice,
                Description = $"{InvoiceDescriptions[random.Int(0, InvoiceDescriptions.Length - 1)]} — {random.Int(75, 300)} gal",
                AmountCents = amount,
                CreatedAt = createdAt,
                Status = isPaid ? "paid" : "open"
            });

            if (isPaid)
            {
                account.Transactions.Add(new BillingTransactionRecord
                {
                    Id = $"txn_{DeterministicGuid(seed, (i * 2) + 1):N}",
                    Type = BillingTransactionType.Payment,
                    Description = "Payment received — thank you",
                    AmountCents = -amount,
                    CreatedAt = createdAt.AddDays(random.Int(3, 12)),
                    Status = "succeeded"
                });
            }
            else
            {
                balance = amount;
            }
        }

        account.BalanceCents = balance;
        account.DueDate = anchorDate.AddDays(random.Int(-6, 21));

        account.Transactions.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        return account;
    }

    private static Guid DeterministicGuid(int seed, int ordinal)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(seed).CopyTo(bytes, 0);
        BitConverter.GetBytes(ordinal).CopyTo(bytes, 4);
        BitConverter.GetBytes(seed ^ ordinal).CopyTo(bytes, 8);
        BitConverter.GetBytes(unchecked(seed * 31) + ordinal).CopyTo(bytes, 12);
        return new Guid(bytes);
    }
}
