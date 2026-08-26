using Microsoft.EntityFrameworkCore;

namespace ProgressHomeHeating.OperationsApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OilTank> Tanks => Set<OilTank>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Truck> Trucks => Set<Truck>();
    public DbSet<DeliveryOrder> Orders => Set<DeliveryOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.OwnsOne(c => c.ServiceAddress);
            entity.HasMany(c => c.Tanks).WithOne(t => t.Customer).HasForeignKey(t => t.CustomerId);
            entity.HasMany(c => c.Orders).WithOne(o => o.Customer).HasForeignKey(o => o.CustomerId);
        });

        modelBuilder.Entity<OilTank>()
            .HasMany(t => t.Orders)
            .WithOne(o => o.Tank)
            .HasForeignKey(o => o.TankId);

        modelBuilder.Entity<DeliveryOrder>(entity =>
        {
            entity.Property(o => o.Status).HasConversion<string>();
            entity.HasOne(o => o.Driver).WithMany().HasForeignKey(o => o.DriverId);
            entity.HasOne(o => o.Truck).WithMany().HasForeignKey(o => o.TruckId);
        });
    }
}
