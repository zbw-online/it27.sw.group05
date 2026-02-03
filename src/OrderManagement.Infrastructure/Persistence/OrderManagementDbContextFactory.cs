using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderManagement.Infrastructure.Persistence
{
    public class OrderManagementDbContextFactory : IDesignTimeDbContextFactory<OrderManagementDbContext>
    {
        public OrderManagementDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OrderManagementDbContext>();

            // Use YOUR connection string 
            _ = optionsBuilder.UseSqlServer(
                "Server=.;Database=OrderManagement;Trusted_Connection=true;TrustServerCertificate=true;",
                b => b.MigrationsAssembly("OrderManagement.Infrastructure"));

            return new OrderManagementDbContext(optionsBuilder.Options);
        }
    }
}
