using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{        // Customer Table
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {

            // Table + Temporal (mono-temporal / system time)
            _ = builder.ToTable("Customers", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("CustomersHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });
            });

            // Primary Key
            _ = builder.HasKey(x => x.Id);

            // Map strongly-typed CustomerId to int
            _ = builder.Property(x => x.Id)
                .HasConversion(id => id.Value, v => new CustomerId(v))
                .ValueGeneratedNever();

            // Single-value Value Objects -> map as scalar columns via converters (avoids temporal table-splitting issues)
            _ = builder.Property(x => x.CustomerNumber)
                .HasConversion(v => v.Value, v => CustomerNumber.FromDb(v))
                .HasColumnName("CustomerNumber")
                .HasMaxLength(7)
                .IsRequired();

            _ = builder.HasIndex("CustomerNumber").IsUnique();

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

            //CustomerAddress
            // Backing field mapping
            builder.Metadata
                .FindNavigation(nameof(Customer.Addresses))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            _ = builder.HasMany(c => c.Addresses)
                .WithOne()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Cascade);

            // System time columns (shadow) - SQL Server populates
            _ = builder.Property<DateTime>("RowValidFrom")
                .HasColumnName("RowValidFrom")
                .ValueGeneratedOnAddOrUpdate();

            _ = builder.Property<DateTime>("RowValidUntil")
                .HasColumnName("RowValidUntil")
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}
