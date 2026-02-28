using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OrderManagement.Infrastructure.Persistence
{
    public class OrderManagementDbContextFactory(IConfiguration config)
            : IDesignTimeDbContextFactory<OrderManagementDbContext>
    {
        private readonly IConfiguration _config = config;

        public OrderManagementDbContextFactory()
            : this(new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<OrderManagementDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build())
        {
        }

        public OrderManagementDbContext CreateDbContext(string[] args)
        {
            string? connectionString = _config.GetConnectionString("OrderManagement");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'ConnectionStrings:OrderManagement' was not found. " +
                    "Set it via 'dotnet user-secrets set \"ConnectionStrings:OrderManagement\" \"<conn>\"' " +
                    "or an environment variable.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<OrderManagementDbContext>();

            _ = optionsBuilder.UseSqlServer(
                connectionString,
                b => b.MigrationsAssembly("OrderManagement.Infrastructure"));

            return new OrderManagementDbContext(optionsBuilder.Options);
        }
    }
}
