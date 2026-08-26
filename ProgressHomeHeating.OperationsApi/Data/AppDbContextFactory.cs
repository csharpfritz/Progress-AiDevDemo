using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProgressHomeHeating.OperationsApi.Data;

// Used only by `dotnet ef migrations add` at design time — Aspire injects the real
// connection string at runtime via AddNpgsqlDbContext, so no live database is needed here.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=operationsdb_designtime;Username=postgres;Password=postgres");
        return new AppDbContext(optionsBuilder.Options);
    }
}
