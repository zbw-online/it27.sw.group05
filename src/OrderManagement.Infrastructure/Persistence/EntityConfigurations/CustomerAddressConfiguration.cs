using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
    {
        public void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            _ = builder.ToTable("CustomerAddresses", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("CustomerAddressesHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });

                _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
            });

            _ = builder.HasKey(x => x.Id);

            _ = builder.Property(x => x.Id)
                .HasColumnName("CustomerAddressId")
                .ValueGeneratedOnAdd();

            _ = builder.Property<CustomerId>("CustomerId")
                .HasColumnName("CustomerId")
                .HasConversion(id => id.Value, v => new CustomerId(v))
                .IsRequired();

            _ = builder.HasIndex("CustomerId");

            // ERM relationship constraint
            _ = builder.HasOne<Customer>()
                .WithMany(c => c.Addresses)
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Cascade);

            // Application temporal (ValidFrom/ValidTo)
            _ = builder.Property(x => x.ValidFrom)
                .HasColumnType("date")
                .IsRequired();

            _ = builder.Property(x => x.ValidTo)
                .HasColumnType("date")
                .IsRequired(false);

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
        }
    }
}
