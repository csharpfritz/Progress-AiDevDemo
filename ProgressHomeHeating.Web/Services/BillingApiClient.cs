using System.Net;
using System.Net.Http.Json;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.Web.Services;

public sealed class BillingUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);

public sealed record PaymentSubmissionResult(PaymentIntentDto? Intent, BillingErrorDto? Error);

public interface IBillingApiClient
{
    Task<BillingAccountDto> GetAccountAsync(Guid customerId, CancellationToken ct = default);

    Task<IReadOnlyList<BillingTransactionDto>> GetTransactionsAsync(
        Guid customerId, int limit = BillingConstants.DefaultTransactionLimit, CancellationToken ct = default);

    Task<PaymentSubmissionResult> SubmitPaymentAsync(
        CreatePaymentIntentRequest request, string idempotencyKey, CancellationToken ct = default);
}

public sealed class BillingApiClient(HttpClient http) : IBillingApiClient
{
    public async Task<BillingAccountDto> GetAccountAsync(Guid customerId, CancellationToken ct = default)
    {
        try
        {
            var account = await http.GetFromJsonAsync<BillingAccountDto>(
                $"/v1/billing_accounts/{customerId}", ct);
            return account ?? throw new BillingUnavailableException("Billing service returned no account.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            throw new BillingUnavailableException("The billing service is unavailable.", ex);
        }
    }

    public async Task<IReadOnlyList<BillingTransactionDto>> GetTransactionsAsync(
        Guid customerId, int limit = BillingConstants.DefaultTransactionLimit, CancellationToken ct = default)
    {
        try
        {
            var list = await http.GetFromJsonAsync<BillingTransactionListDto>(
                $"/v1/billing_accounts/{customerId}/transactions?limit={limit}", ct);
            return list?.Data ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            throw new BillingUnavailableException("The billing service is unavailable.", ex);
        }
    }

    public async Task<PaymentSubmissionResult> SubmitPaymentAsync(
        CreatePaymentIntentRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/v1/payment_intents")
            {
                Content = JsonContent.Create(request)
            };
            message.Headers.Add(BillingConstants.IdempotencyKeyHeader, idempotencyKey);

            using var response = await http.SendAsync(message, ct);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadFromJsonAsync<BillingErrorDto>(ct)
                            ?? new BillingErrorDto(BillingErrorCodes.AmountInvalid, "The payment was rejected.");
                return new PaymentSubmissionResult(null, error);
            }

            response.EnsureSuccessStatusCode();

            var intent = await response.Content.ReadFromJsonAsync<PaymentIntentDto>(ct)
                         ?? throw new BillingUnavailableException("Billing service returned no payment intent.");
            return new PaymentSubmissionResult(intent, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            throw new BillingUnavailableException("The billing service is unavailable.", ex);
        }
    }
}
