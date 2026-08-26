namespace ProgressHomeHeating.Contracts;

public record ServiceAddressDto(string Street, string City, string State, string Zip);

public record CustomerDto(Guid Id, string Name, string Email, string Phone, ServiceAddressDto ServiceAddress);

public record CreateCustomerRequest(string Name, string Email, string Phone, ServiceAddressDto ServiceAddress);
