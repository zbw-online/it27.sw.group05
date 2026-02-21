using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {

        // Customer Table
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
            });

            _ = builder.HasKey(x => x.Id);

            _ = builder.Property(x => x.Id)
                .HasConversion(id => id.Value, v => new CustomerId(v))
                .ValueGeneratedNever();

            _ = builder.OwnsOne(x => x.CustomerNumber, nb =>
            {
                _ = nb.Property(p => p.Value)
                .HasColumnName("CustomerNumber")
                .HasMaxLength(7)
                .IsRequired();
            });

            _ = builder.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            _ = builder.Property(x => x.SurName)
                .HasMaxLength(100)
                .IsRequired();

            _ = builder.OwnsOne(x => x.Email, eb =>
            {
                _ = eb.Property(p => p.Value)
                .HasColumnName("Email")
                .HasMaxLength(255)
                .IsRequired();
            });

            _ = builder.Property(x => x.Website)
                .HasMaxLength(255)
                .IsRequired();

            //CustomerAddress
            // Backing field mapping

            _ = builder.HasMany(typeof(CustomerAddress), "_addresses")
                .WithOne()
                .HasForeignKey("CustomerId")
                .OnDelete(DeleteBehavior.Cascade);

            _ = builder.Navigation(x => x.Addresses)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // System time columns (shadow) - SQL Server populates
            _ = builder.Property<DateTime>("RowValidFrom")
                .HasColumnName("RowValidFrom")
                .ValueGeneratedOnAddOrUpdate();

            _ = builder.Property<DateTime>("RowValidUntil")
                .HasColumnName("RowValidUntil")
                .ValueGeneratedOnAddOrUpdate();

            // Uniqueness
            _ = builder.HasIndex("Customernumber").IsUnique();
            _ = builder.HasIndex("Email").IsUnique();
        }


        // CustomerAddresses Table
        public static void Configure(EntityTypeBuilder<CustomerAddress> builder)
        {
            _ = builder.ToTable("CustomerAddresses", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("CustomerAddressHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });
            });

            _ = builder.HasKey(x => x.Id);
            _ = builder.Property(x => x.Id).ValueGeneratedOnAdd();

            _ = builder.Property<int>("CustomerId").IsRequired();

            // Application Temporal structure
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


            // System time columns (shadow)
            _ = builder.Property<DateTime>("RowValidFrom")
                   .HasColumnName("RowValidFrom")
                   .ValueGeneratedOnAddOrUpdate();

            _ = builder.Property<DateTime>("RowValidUntil")
                   .HasColumnName("RowValidUntil")
                   .ValueGeneratedOnAddOrUpdate();

            _ = builder.HasIndex("CustomerId");
            _ = builder.HasIndex(x => new { x.ValidFrom, x.ValidTo });
        }
    }
}
