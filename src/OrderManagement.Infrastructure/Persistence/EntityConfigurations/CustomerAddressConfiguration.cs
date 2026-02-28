using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    // CustomerAddresses Table
    public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
    {
        public void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            // Table + Temporal (system time)
            _ = builder.ToTable("CustomerAddresses", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("CustomerAddressHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });
            });

            // Primary key
            _ = builder.HasKey(x => x.Id);

            // Address id is generated (you create with id: 0)
            _ = builder.Property(x => x.Id).ValueGeneratedOnAdd();

            // Shadow FK to Cusomer
            _ = builder.Property<CustomerId>("CustomerId").IsRequired()
                .HasConversion(id => id.Value, v => new CustomerId(v))
                .IsRequired();
            _ = builder.HasIndex("CustomerId");

            // Application Temporal structure (bi-temporal requirement)
            _ = builder.Property(x => x.ValidFrom)
                .HasColumnType("date")
                .IsRequired();

            _ = builder.Property(x => x.ValidTo)
                .HasColumnType("date")
                .IsRequired(false);


            // Address Fields

            _ = builder.Property(x => x.Street)
                .HasMaxLength(200)
                .IsRequired();

            _ = builder.Property(x => x.HouseNumber)
                .HasMaxLength(20)
                .IsRequired();

            _ = builder.Property(x => x.PostalCode)
                .HasMaxLength(20)
                .IsRequired();

            _ = builder.Property(x => x.City)
                .HasMaxLength(100)
                .IsRequired();

            _ = builder.Property(x => x.CountryCode)
                .HasColumnType("nchar(2)")
                .IsRequired();


            // System time columns (shadow)
            _ = builder.Property<DateTime>("RowValidFrom")
                   .HasColumnName("RowValidFrom")
                   .ValueGeneratedOnAddOrUpdate();

            _ = builder.Property<DateTime>("RowValidUntil")
                   .HasColumnName("RowValidUntil")
                   .ValueGeneratedOnAddOrUpdate();
        }
    }
}
