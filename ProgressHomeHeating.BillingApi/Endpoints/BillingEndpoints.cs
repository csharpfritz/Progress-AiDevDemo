using ProgressHomeHeating.BillingApi.Billing;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.BillingApi.Endpoints;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/v1/billing_accounts").WithTags("BillingAccounts");

        accounts.MapGet("/{customerId:guid}", (Guid customerId, IBillingStore store, TimeProvider time) =>
        {
            var now = time.GetUtcNow();
            var account = store.GetOrSeed(customerId);
            return Results.Ok(account.ToDto(DateOnly.FromDateTime(now.UtcDateTime), now));
        });

        accounts.MapGet("/{customerId:guid}/transactions", (Guid customerId, int? limit, IBillingStore store) =>
        {
            var take = limit ?? BillingConstants.DefaultTransactionLimit;
            if (take < 1 || take > BillingConstants.MaxTransactionLimit)
            {
                return Results.BadRequest(new BillingErrorDto(
                    BillingErrorCodes.LimitInvalid,
                    $"limit must be between 1 and {BillingConstants.MaxTransactionLimit}.",
                    "limit"));
            }

            var account = store.GetOrSeed(customerId);
            List<BillingTransactionRecord> snapshot;
            lock (account.Gate)
            {
                snapshot = [.. account.Transactions];
            }

            var page = snapshot.Take(take).Select(t => t.ToDto(customerId)).ToList();
            return Results.Ok(new BillingTransactionListDto(page, snapshot.Count > take));
        });

        accounts.MapPost("/{customerId:guid}/reset", (Guid customerId, IBillingStore store, TimeProvider time) =>
        {
            var now = time.GetUtcNow();
            var account = store.Reset(customerId);
            return Results.Ok(account.ToDto(DateOnly.FromDateTime(now.UtcDateTime), now));
        });

        var intents = app.MapGroup("/v1/payment_intents").WithTags("PaymentIntents");

        intents.MapPost("/", (CreatePaymentIntentRequest request, HttpContext http, IBillingStore store) =>
        {
            using var activity = BillingTelemetry.Source.StartActivity("billing.create_payment_intent");
            activity?.SetTag("billing.customer_id", request.CustomerId);
            activity?.SetTag("billing.amount_cents", request.AmountCents);

            var key = http.Request.Headers[BillingConstants.IdempotencyKeyHeader].FirstOrDefault();
            var idempotencyKey = string.IsNullOrWhiteSpace(key) ? null : key;

            // A replay is looked up — and returned — before validation, so it never fails against a
            // balance that has already moved since the original, successful attempt. A key reused
            // with different request parameters is rejected rather than replayed (409).
            if (idempotencyKey is not null)
            {
                var replay = store.FindReplay(request, idempotencyKey);
                if (replay.Kind == PaymentReplayKind.Matched)
                {
                    activity?.SetTag("billing.result", replay.Intent!.Status.ToString());
                    activity?.SetTag("billing.idempotency_replayed", true);
                    http.Response.Headers[BillingConstants.IdempotencyReplayedHeader] = "true";
                    return Results.Ok(replay.Intent!.ToDto());
                }

                if (replay.Kind == PaymentReplayKind.Mismatched)
                {
                    var mismatchError = new BillingErrorDto(
                        BillingErrorCodes.IdempotencyKeyMismatch,
                        "This idempotency key was already used with different payment parameters.",
                        BillingConstants.IdempotencyKeyHeader);
                    activity?.SetTag("billing.result", mismatchError.Code);
                    return Results.Conflict(mismatchError);
                }
            }

            var account = store.GetOrSeed(request.CustomerId);
            var validationError = FauxPaymentProcessor.Validate(request, account.BalanceCents);
            if (validationError is not null)
            {
                activity?.SetTag("billing.result", validationError.Code);
                return Results.BadRequest(validationError);
            }

            var application = store.ApplyPayment(request, idempotencyKey);

            if (application.Mismatched)
            {
                // Rare race: another request recorded a conflicting payload under the same key
                // between the lookup above and acquiring the account lock in ApplyPayment.
                var mismatchError = new BillingErrorDto(
                    BillingErrorCodes.IdempotencyKeyMismatch,
                    "This idempotency key was already used with different payment parameters.",
                    BillingConstants.IdempotencyKeyHeader);
                activity?.SetTag("billing.result", mismatchError.Code);
                return Results.Conflict(mismatchError);
            }

            activity?.SetTag("billing.result", application.Intent!.Status.ToString());
            activity?.SetTag("billing.idempotency_replayed", application.Replayed);

            if (application.Replayed)
            {
                http.Response.Headers[BillingConstants.IdempotencyReplayedHeader] = "true";
            }

            return Results.Ok(application.Intent!.ToDto());
        });

        intents.MapGet("/{id}", (string id, IBillingStore store) =>
            store.FindIntent(id) is { } intent ? Results.Ok(intent.ToDto()) : Results.NotFound());
    }
}
