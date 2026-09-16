using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.BillingApi.Billing;

public interface IBillingStore
{
    BillingAccountRecord GetOrSeed(Guid customerId);
    BillingAccountRecord Reset(Guid customerId);
    PaymentReplayResult FindReplay(CreatePaymentIntentRequest request, string idempotencyKey);
    PaymentApplication ApplyPayment(CreatePaymentIntentRequest request, string? idempotencyKey);
    PaymentIntentRecord? FindIntent(string intentId);
    int AccountCount { get; }
}

public sealed class InMemoryBillingStore(
    IOptions<BillingOptions> options,
    FauxPaymentProcessor processor,
    TimeProvider timeProvider,
    ILogger<InMemoryBillingStore> logger) : IBillingStore
{
    private readonly ConcurrentDictionary<Guid, BillingAccountRecord> _accounts = new();
    private readonly ConcurrentDictionary<string, PaymentIntentRecord> _intents = new();
    private readonly BillingOptions _options = options.Value;

    public int AccountCount => _accounts.Count;

    private DateOnly AnchorDate =>
        _options.SeedAnchorDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    public BillingAccountRecord GetOrSeed(Guid customerId) =>
        _accounts.GetOrAdd(customerId, id =>
        {
            logger.LogInformation("Seeding deterministic billing account for customer {CustomerId}", id);
            return DeterministicBillingSeeder.Create(id, AnchorDate);
        });

    public BillingAccountRecord Reset(Guid customerId)
    {
        var seeded = DeterministicBillingSeeder.Create(customerId, AnchorDate);
        _accounts[customerId] = seeded;
        logger.LogInformation("Reset billing account for customer {CustomerId}", customerId);
        return seeded;
    }

    public PaymentIntentRecord? FindIntent(string intentId) =>
        _intents.TryGetValue(intentId, out var intent) ? intent : null;

    // Looked up before request validation so a replayed request never fails validation against a
    // balance that has already moved since the original, successful attempt. Validates that the
    // replayed request carries the same amount/currency/method as the original — a mismatched
    // payload under a reused key is rejected rather than silently returning the earlier result.
    public PaymentReplayResult FindReplay(CreatePaymentIntentRequest request, string idempotencyKey)
    {
        var account = GetOrSeed(request.CustomerId);
        lock (account.Gate)
        {
            return ResolveReplay(account, idempotencyKey, request);
        }
    }

    public PaymentApplication ApplyPayment(CreatePaymentIntentRequest request, string? idempotencyKey)
    {
        var account = GetOrSeed(request.CustomerId);

        lock (account.Gate)
        {
            if (idempotencyKey is not null)
            {
                var replay = ResolveReplay(account, idempotencyKey, request);
                switch (replay.Kind)
                {
                    case PaymentReplayKind.Matched:
                        logger.LogInformation(
                            "Replaying payment intent {PaymentIntentId} for idempotency key {IdempotencyKey}",
                            replay.Intent!.Id, idempotencyKey);
                        return PaymentApplication.Succeeded(replay.Intent, replayed: true);
                    case PaymentReplayKind.Mismatched:
                        logger.LogWarning(
                            "Idempotency key {IdempotencyKey} for customer {CustomerId} was reused with different parameters",
                            idempotencyKey, request.CustomerId);
                        return PaymentApplication.KeyMismatch;
                }
            }

            var outcome = processor.Evaluate(request.AmountCents);
            var now = timeProvider.GetUtcNow();
            var intentId = $"pi_{Guid.CreateVersion7():N}";

            if (outcome.Succeeded)
            {
                account.BalanceCents -= request.AmountCents;
                account.Transactions.Insert(0, new BillingTransactionRecord
                {
                    Id = $"txn_{Guid.CreateVersion7():N}",
                    Type = BillingTransactionType.Payment,
                    Description = "Payment received — thank you",
                    AmountCents = -request.AmountCents,
                    CreatedAt = now,
                    Status = "succeeded"
                });

                if (account.BalanceCents == 0)
                {
                    account.DueDate = null;
                }
            }

            var intent = new PaymentIntentRecord
            {
                Id = intentId,
                CustomerId = request.CustomerId,
                AmountCents = request.AmountCents,
                Status = outcome.Succeeded ? PaymentIntentStatus.Succeeded : PaymentIntentStatus.Failed,
                CreatedAt = now,
                BalanceAfterCents = account.BalanceCents,
                FailureCode = outcome.FailureCode,
                FailureMessage = outcome.FailureMessage
            };

            _intents[intentId] = intent;

            if (idempotencyKey is not null)
            {
                account.IdempotencyKeys[idempotencyKey] = new IdempotencyRecord(
                    intentId, request.AmountCents, request.Currency, request.PaymentMethod);
            }

            logger.LogInformation(
                "Payment intent {PaymentIntentId} for customer {CustomerId} completed with status {Status}",
                intent.Id, request.CustomerId, intent.Status);

            return PaymentApplication.Succeeded(intent, replayed: false);
        }
    }

    // Must be called while holding account.Gate.
    private PaymentReplayResult ResolveReplay(
        BillingAccountRecord account, string idempotencyKey, CreatePaymentIntentRequest request)
    {
        if (!account.IdempotencyKeys.TryGetValue(idempotencyKey, out var record)
            || !_intents.TryGetValue(record.IntentId, out var existing))
        {
            return PaymentReplayResult.NotFound;
        }

        var matches = record.AmountCents == request.AmountCents
            && string.Equals(record.Currency, request.Currency, StringComparison.OrdinalIgnoreCase)
            && string.Equals(record.PaymentMethod, request.PaymentMethod, StringComparison.OrdinalIgnoreCase);

        return matches ? PaymentReplayResult.Matched(existing) : PaymentReplayResult.Mismatched;
    }
}
