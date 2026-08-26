namespace ProgressHomeHeating.Contracts;

public record DriverDto(Guid Id, string Name, string LicenseNumber);

public record TruckDto(Guid Id, string Label, int CapacityGallons);
