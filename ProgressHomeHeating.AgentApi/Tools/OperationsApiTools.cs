using System.ComponentModel;
using System.Globalization;
using ProgressHomeHeating.AgentApi.Clients;
using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.AgentApi.Tools;

public class OperationsApiTools(OperationsApiClient api)
{
    [Description("Lists customers whose oil tank level is at or below the given threshold percentage, so deliveries can be prioritized. Defaults to 20%.")]
    public async Task<string> GetLowOilCustomersAsync(
        [Description("The tank level percentage (0-100) at or below which a customer is considered low on oil.")] int thresholdPercent = 20)
    {
        var lowTanks = await api.GetLowTanksAsync(thresholdPercent);
        if (lowTanks.Count == 0)
        {
            return $"No customers currently at or below {thresholdPercent}% oil.";
        }

        var customers = await api.GetCustomersAsync();
        var lines = lowTanks.Select(t =>
        {
            var customer = customers.FirstOrDefault(c => c.Id == t.CustomerId);
            return $"- {customer?.Name ?? "Unknown customer"} (customerId={t.CustomerId}, tankId={t.Id}): {t.CurrentLevelPercent:0}% remaining, {t.SizeGallons} gallon tank.";
        });

        return string.Join("\n", lines);
    }

    [Description("Looks up a customer by name (partial match allowed) and returns their contact info, address, and oil tank status.")]
    public async Task<string> GetCustomerByNameAsync(
        [Description("Full or partial customer name to search for.")] string name)
    {
        var customers = await api.GetCustomersAsync();
        var matches = customers
            .Where(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return $"No customer found matching \"{name}\".";
        }

        var tanks = await api.GetTanksAsync();
        var lines = matches.Select(c =>
        {
            var tank = tanks.FirstOrDefault(t => t.CustomerId == c.Id);
            var tankInfo = tank is null
                ? "no tank on file"
                : $"tankId={tank.Id}, {tank.CurrentLevelPercent:0}% remaining of {tank.SizeGallons} gallons";
            return $"- {c.Name} (customerId={c.Id}), {c.Email}, {c.Phone}, {c.ServiceAddress.Street}, {c.ServiceAddress.City} {c.ServiceAddress.State} {c.ServiceAddress.Zip}. Tank: {tankInfo}.";
        });

        return string.Join("\n", lines);
    }

    [Description("Schedules a new oil delivery for a customer, identified by name, on a given date. Use GetCustomerByNameAsync first if you need to confirm the customer exists.")]
    public async Task<string> ScheduleDeliveryAsync(
        [Description("Full or partial customer name to schedule the delivery for.")] string customerName,
        [Description("Gallons of oil requested for this delivery, typically 100-500.")] int gallonsRequested,
        [Description("Delivery date in yyyy-MM-dd format.")] string scheduledDate)
    {
        var customers = await api.GetCustomersAsync();
        var matches = customers
            .Where(c => c.Name.Contains(customerName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return $"No customer found matching \"{customerName}\". Cannot schedule delivery.";
        }

        if (matches.Count > 1)
        {
            var names = string.Join(", ", matches.Select(c => c.Name));
            return $"\"{customerName}\" matches multiple customers ({names}). Call again with the full name of the intended customer.";
        }

        var customer = matches[0];

        var tanks = await api.GetTanksAsync(customer.Id);
        var tank = tanks.FirstOrDefault();
        if (tank is null)
        {
            return $"{customer.Name} has no oil tank on file. Cannot schedule delivery.";
        }

        if (!DateOnly.TryParse(scheduledDate, CultureInfo.InvariantCulture, out var date))
        {
            return $"Could not parse date \"{scheduledDate}\". Please use yyyy-MM-dd format.";
        }

        var existingOrders = await api.GetOrdersAsync(from: DateOnly.FromDateTime(DateTime.Today));
        var pendingOrder = existingOrders.FirstOrDefault(o =>
            o.CustomerId == customer.Id
            && o.Status is DeliveryStatus.Requested or DeliveryStatus.Scheduled or DeliveryStatus.EnRoute);
        if (pendingOrder is not null)
        {
            return $"{customer.Name} already has a pending delivery (orderId={pendingOrder.Id}, " +
                   $"{pendingOrder.GallonsRequested} gallons on {pendingOrder.ScheduledDate:yyyy-MM-dd}, status={pendingOrder.Status}). " +
                   "Not scheduling a duplicate.";
        }

        var order = await api.CreateOrderAsync(new CreateDeliveryOrderRequest(
            CustomerId: customer.Id,
            TankId: tank.Id,
            ScheduledDate: date,
            GallonsRequested: gallonsRequested,
            DriverId: null,
            TruckId: null,
            CreatedBy: "agent"));

        return order is null
            ? "Failed to create the delivery order."
            : $"Scheduled: {gallonsRequested} gallons for {customer.Name} on {date:yyyy-MM-dd} (orderId={order.Id}, status={order.Status}).";
    }
}
