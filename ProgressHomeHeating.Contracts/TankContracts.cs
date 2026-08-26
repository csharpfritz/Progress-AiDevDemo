namespace ProgressHomeHeating.Contracts;

public record OilTankDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    int SizeGallons,
    DateOnly InstallDate,
    double CurrentLevelPercent,
    double EstimatedDailyUsageGallons);

public record UpdateTankLevelRequest(double CurrentLevelPercent);
