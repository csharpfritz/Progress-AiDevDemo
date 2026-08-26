using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.OperationsApi.Data;

public static class MappingExtensions
{
    public static CustomerDto ToDto(this Customer c) => new(
        c.Id, c.Name, c.Email, c.Phone,
        new ServiceAddressDto(c.ServiceAddress.Street, c.ServiceAddress.City, c.ServiceAddress.State, c.ServiceAddress.Zip));

    public static OilTankDto ToDto(this OilTank t) => new(
        t.Id, t.CustomerId, t.Customer?.Name ?? "", t.SizeGallons, t.InstallDate,
        t.CurrentLevelPercent, t.EstimatedDailyUsageGallons);

    public static DriverDto ToDto(this Driver d) => new(d.Id, d.Name, d.LicenseNumber);

    public static TruckDto ToDto(this Truck t) => new(t.Id, t.Label, t.CapacityGallons);

    public static DeliveryOrderDto ToDto(this DeliveryOrder o) => new(
        o.Id, o.CustomerId, o.Customer?.Name ?? "", o.TankId,
        o.DriverId, o.Driver?.Name, o.TruckId, o.Truck?.Label,
        o.ScheduledDate, o.Status, o.GallonsRequested, o.GallonsDelivered, o.CreatedBy);
}
