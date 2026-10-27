using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HrEmployeeManagementSystem.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public const string LocalConnection =
        "Server=.;Database=HrEmployeeManagementSystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = configuration.GetConnectionString("DefaultConnection") ?? LocalConnection;
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;
        return new AppDbContext(options);
    }
}
