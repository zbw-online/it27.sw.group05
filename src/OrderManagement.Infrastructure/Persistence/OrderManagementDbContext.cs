using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;

using SharedKernel.SeedWork;

namespace OrderManagement.Infrastructure.Persistence
{
    public class OrderManagementDbContext(DbContextOptions<OrderManagementDbContext> options) : DbContext(options)
    {

        // DbSets for aggregate roots (optional - Set<T>() works without them)
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<ArticleGroup> ArticleGroups { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<CustomerAddress> CustomerAddresses { get; set; } = null!;

        public DbSet<Order> Orders { get; set; } = null!;

        public DbSet<OrderLine> OrderLines { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ignore domain events from the EF model - they are domain-only and not persisted as entities
            _ = modelBuilder.Ignore<DomainEvent>();

            // Auto-discovers IEntityTypeConfiguration classes
            _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderManagementDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (EntityEntry<Article> entry in ChangeTracker.Entries<Article>())
            {
                if (entry.State == EntityState.Modified)
                {
                    int currentVersion = (int)entry.Property("RowVersion").CurrentValue!;
                    entry.Property("RowVersion").CurrentValue = currentVersion + 1;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
