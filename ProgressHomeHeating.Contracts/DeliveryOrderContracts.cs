namespace ProgressHomeHeating.Contracts;

public record DeliveryOrderDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid TankId,
    Guid? DriverId,
    string? DriverName,
    Guid? TruckId,
    string? TruckLabel,
    DateOnly ScheduledDate,
    DeliveryStatus Status,
    int GallonsRequested,
    int? GallonsDelivered,
    string? CreatedBy = null);

public record CreateDeliveryOrderRequest(
    Guid CustomerId,
    Guid TankId,
    DateOnly ScheduledDate,
    int GallonsRequested,
    Guid? DriverId,
    Guid? TruckId,
    string? CreatedBy = null);

public record UpdateDeliveryOrderRequest(
    DateOnly? ScheduledDate,
    DeliveryStatus? Status,
    Guid? DriverId,
    Guid? TruckId,
    int? GallonsDelivered);

public record CompleteDeliveryOrderRequest(int GallonsDelivered);
