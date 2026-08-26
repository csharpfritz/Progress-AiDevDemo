using ProgressHomeHeating.Contracts;

namespace ProgressHomeHeating.OperationsApi.Data;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required ServiceAddress ServiceAddress { get; set; }

    public List<OilTank> Tanks { get; set; } = [];
    public List<DeliveryOrder> Orders { get; set; } = [];
}

public class ServiceAddress
{
    public required string Street { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string Zip { get; set; }
}

public class OilTank
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int SizeGallons { get; set; }
    public DateOnly InstallDate { get; set; }
    public double CurrentLevelPercent { get; set; }
    public double EstimatedDailyUsageGallons { get; set; }

    public List<DeliveryOrder> Orders { get; set; } = [];
}

public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string LicenseNumber { get; set; }
}

public class Truck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Label { get; set; }
    public int CapacityGallons { get; set; }
}

public class DeliveryOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid TankId { get; set; }
    public OilTank? Tank { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public Guid? TruckId { get; set; }
    public Truck? Truck { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Requested;
    public int GallonsRequested { get; set; }
    public int? GallonsDelivered { get; set; }

    // Set to "agent" for orders created by the AI dispatch agent, so a demo run can be reset.
    // Null for seeded/manually-created orders.
    public string? CreatedBy { get; set; }
}
