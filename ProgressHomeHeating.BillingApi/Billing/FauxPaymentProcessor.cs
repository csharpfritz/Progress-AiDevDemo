using Microsoft.Extensions.Options;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.BillingApi.Billing;

public sealed class FauxPaymentProcessor(IOptions<BillingOptions> options)
{
    private readonly BillingOptions _options = options.Value;

    // Deterministic, provider-style outcomes. No randomness, no external call (AC-011, NFR determinism).
    public PaymentOutcome Evaluate(long amountCents)
    {
        var cents = (int)(amountCents % 100);

        if (cents == _options.DeclineCents)
        {
            return new PaymentOutcome(false, BillingErrorCodes.CardDeclined,
                "Your payment method was declined by the issuing bank.");
        }

        if (cents == _options.ProcessingErrorCents)
        {
            return new PaymentOutcome(false, BillingErrorCodes.ProcessingError,
                "The payment could not be processed. No funds were moved.");
        }

        return new PaymentOutcome(true, null, null);
    }

    // Returns null when the request is valid; otherwise the validation error to return as HTTP 400.
    public static BillingErrorDto? Validate(CreatePaymentIntentRequest request, long balanceCents)
    {
        if (request.AmountCents <= 0)
        {
            return new BillingErrorDto(BillingErrorCodes.AmountInvalid,
                "Payment amount must be greater than zero.", "amountCents");
        }

        if (request.AmountCents < BillingConstants.MinimumPaymentCents)
        {
            return new BillingErrorDto(BillingErrorCodes.AmountBelowMinimum,
                "The minimum payment is $1.00.", "amountCents");
        }

        if (request.AmountCents > balanceCents)
        {
            return new BillingErrorDto(BillingErrorCodes.AmountExceedsBalance,
                "Payment amount cannot exceed the outstanding balance.", "amountCents");
        }

        if (!string.Equals(request.Currency, BillingConstants.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return new BillingErrorDto(BillingErrorCodes.CurrencyUnsupported,
                "Only USD payments are supported.", "currency");
        }

        if (!BillingPaymentMethods.All.Contains(request.PaymentMethod, StringComparer.OrdinalIgnoreCase))
        {
            return new BillingErrorDto(BillingErrorCodes.PaymentMethodUnsupported,
                "Supported payment methods are 'card' and 'bank_account'.", "paymentMethod");
        }

        return null;
    }
}
