using System.Net.Http.Json;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.Web.Services;

public class OperationsApiClient(HttpClient http)
{
    public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<CustomerDto>>("/api/customers", ct) ?? [];

    public async Task<List<OilTankDto>> GetTanksAsync(Guid? customerId = null, CancellationToken ct = default)
    {
        var url = customerId is null ? "/api/tanks" : $"/api/tanks?customerId={customerId}";
        return await http.GetFromJsonAsync<List<OilTankDto>>(url, ct) ?? [];
    }

    public async Task<List<OilTankDto>> GetLowTanksAsync(double thresholdPercent = 20, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<OilTankDto>>($"/api/tanks/low?thresholdPercent={thresholdPercent}", ct) ?? [];

    public async Task<List<DeliveryOrderDto>> GetOrdersAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (from is not null) query.Add($"from={from:yyyy-MM-dd}");
        if (to is not null) query.Add($"to={to:yyyy-MM-dd}");
        var url = "/api/orders" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return await http.GetFromJsonAsync<List<DeliveryOrderDto>>(url, ct) ?? [];
    }

    public async Task<DeliveryOrderDto?> CreateOrderAsync(CreateDeliveryOrderRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/orders", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeliveryOrderDto>(ct);
    }

    public async Task<DeliveryOrderDto?> UpdateOrderAsync(Guid id, UpdateDeliveryOrderRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"/api/orders/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeliveryOrderDto>(ct);
    }

    public async Task<List<DriverDto>> GetDriversAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<DriverDto>>("/api/drivers", ct) ?? [];

    public async Task<int> ResetAgentOrdersAsync(CancellationToken ct = default)
    {
        var response = await http.DeleteAsync("/api/orders/agent-runs", ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AgentResetResult>(ct);
        return result?.Deleted ?? 0;
    }

    private record AgentResetResult(int Deleted);

    public async Task<List<TruckDto>> GetTrucksAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<TruckDto>>("/api/trucks", ct) ?? [];
}
