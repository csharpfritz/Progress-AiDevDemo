using System.Net.Http.Json;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.AgentApi.Clients;

public class OperationsApiClient(HttpClient http)
{
    public async Task<List<CustomerDto>> GetCustomersAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<CustomerDto>>("/api/customers", ct) ?? [];

    public async Task<List<OilTankDto>> GetLowTanksAsync(double thresholdPercent = 20, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<OilTankDto>>($"/api/tanks/low?thresholdPercent={thresholdPercent}", ct) ?? [];

    public async Task<List<OilTankDto>> GetTanksAsync(Guid? customerId = null, CancellationToken ct = default)
    {
        var url = customerId is null ? "/api/tanks" : $"/api/tanks?customerId={customerId}";
        return await http.GetFromJsonAsync<List<OilTankDto>>(url, ct) ?? [];
    }

    public async Task<List<DriverDto>> GetDriversAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<DriverDto>>("/api/drivers", ct) ?? [];

    public async Task<List<DeliveryOrderDto>> GetOrdersAsync(DateOnly? from = null, CancellationToken ct = default)
    {
        var url = from is null ? "/api/orders" : $"/api/orders?from={from:yyyy-MM-dd}";
        return await http.GetFromJsonAsync<List<DeliveryOrderDto>>(url, ct) ?? [];
    }

    public async Task<DeliveryOrderDto?> CreateOrderAsync(CreateDeliveryOrderRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/orders", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeliveryOrderDto>(ct);
    }
}
