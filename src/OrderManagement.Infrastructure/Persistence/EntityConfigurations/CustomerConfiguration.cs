using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            _ = builder.ToTable("Customers", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("CustomersHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });

                _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
            });

            _ = builder.HasKey(x => x.Id);

            _ = builder.Property(x => x.Id)
                .HasColumnName("CustomerId")
                .HasConversion(id => id.Value, v => new CustomerId(v))
                .ValueGeneratedNever();

            _ = builder.Property(x => x.CustomerNumber)
                .HasConversion(v => v.Value, v => CustomerNumber.FromDb(v))
                .HasColumnName("CustomerNumber")
                .HasMaxLength(7)
                .IsRequired();

            _ = builder.HasIndex(x => x.CustomerNumber).IsUnique();

            _ = builder.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            _ = builder.Property(x => x.SurName)
                .HasMaxLength(100)
                .IsRequired();

            _ = builder.Property(x => x.Email)
                .HasConversion(v => v.Value, v => Email.FromDb(v))
                .HasColumnName("Email")
                .HasMaxLength(255)
                .IsRequired();

            _ = builder.HasIndex("Email").IsUnique();

            _ = builder.Property(x => x.Website)
                .HasMaxLength(255)
                .IsRequired(false);

            _ = builder.Property(x => x.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            // Backing field mapping for Addresses
            builder.Metadata.FindNavigation(nameof(Customer.Addresses))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            // ERM: Customer (1) -> (n) CustomerAddresses
            _ = builder.HasMany(c => c.Addresses)
                .WithOne()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
