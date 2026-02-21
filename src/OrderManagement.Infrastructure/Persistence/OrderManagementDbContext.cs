using Microsoft.EntityFrameworkCore;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence.EntityConfigurations;

using SharedKernel.SeedWork;

namespace OrderManagement.Infrastructure.Persistence
{
    public class OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options) : DbContext(options)
    {

        // DbSets for aggregate roots (optional - Set<T>() works without them)
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleGroup> ArticleGroups { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        // TODO: Enable when Order repository is implemented
        //public DbSet<Domain.Orders.Order> Orders { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ignore domain events from the EF model - they are domain-only and not persisted as entities
            _ = modelBuilder.Ignore<DomainEvent>();

            // Auto-discovers IEntityTypeConfiguration classes
            _ = modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(ArticleConfiguration).Assembly);

            _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerConfiguration).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
