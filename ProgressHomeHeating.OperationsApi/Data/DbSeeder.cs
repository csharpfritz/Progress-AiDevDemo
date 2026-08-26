using Bogus;
using Microsoft.EntityFrameworkCore;

namespace ProgressHomeHeating.OperationsApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Customers.AnyAsync()) return;

        var rng = new Faker();

        var addressFaker = new Faker<ServiceAddress>()
            .CustomInstantiator(f => new ServiceAddress
            {
                Street = f.Address.StreetAddress(),
                City = f.Address.City(),
                State = f.Address.StateAbbr(),
                Zip = f.Address.ZipCode()
            });

        var customerFaker = new Faker<Customer>()
            .CustomInstantiator(f => new Customer
            {
                Name = f.Name.FullName(),
                Email = f.Internet.Email(),
                Phone = f.Phone.PhoneNumber("###-###-####"),
                ServiceAddress = addressFaker.Generate()
            });

        var customers = customerFaker.Generate(20);

        foreach (var customer in customers)
        {
            var size = rng.PickRandom(275, 330, 500, 1000);
            var level = rng.Random.Double(5, 95);
            customer.Tanks.Add(new OilTank
            {
                CustomerId = customer.Id,
                SizeGallons = size,
                InstallDate = DateOnly.FromDateTime(rng.Date.Past(15)),
                CurrentLevelPercent = Math.Round(level, 1),
                EstimatedDailyUsageGallons = Math.Round(rng.Random.Double(1.5, 6.0), 2)
            });
        }

        var drivers = new Faker<Driver>()
            .CustomInstantiator(f => new Driver
            {
                Name = f.Name.FullName(),
                LicenseNumber = f.Random.Replace("CDL-#######")
            })
            .Generate(4);

        var trucks = Enumerable.Range(1, 4)
            .Select(i => new Truck
            {
                Label = $"Truck {i}",
                CapacityGallons = rng.PickRandom(2000, 2500, 3000)
            })
            .ToList();

        var orders = new List<DeliveryOrder>();
        foreach (var customer in customers)
        {
            var tank = customer.Tanks[0];
            var orderCount = rng.Random.Int(0, 2);
            for (var i = 0; i < orderCount; i++)
            {
                var isFuture = rng.Random.Bool();
                var scheduledDate = DateOnly.FromDateTime(isFuture
                    ? rng.Date.Soon(14)
                    : rng.Date.Recent(30));
                var isScheduled = rng.Random.Bool(0.7f);

                orders.Add(new DeliveryOrder
                {
                    CustomerId = customer.Id,
                    TankId = tank.Id,
                    ScheduledDate = scheduledDate,
                    GallonsRequested = rng.Random.Int(100, tank.SizeGallons),
                    Status = isFuture
                        ? (isScheduled ? Contracts.DeliveryStatus.Scheduled : Contracts.DeliveryStatus.Requested)
                        : Contracts.DeliveryStatus.Delivered,
                    GallonsDelivered = isFuture ? null : rng.Random.Int(100, tank.SizeGallons),
                    DriverId = isScheduled || !isFuture ? rng.PickRandom(drivers).Id : null,
                    TruckId = isScheduled || !isFuture ? rng.PickRandom(trucks).Id : null
                });
            }
        }

        db.Customers.AddRange(customers);
        db.Drivers.AddRange(drivers);
        db.Trucks.AddRange(trucks);
        db.Orders.AddRange(orders);

        await db.SaveChangesAsync();
    }
}
