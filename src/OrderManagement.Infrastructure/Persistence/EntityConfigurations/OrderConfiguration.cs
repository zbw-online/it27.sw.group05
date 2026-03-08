using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            _ = builder.ToTable("Orders", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("OrdersHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });

                // // Mapping Attributes to Parent Attributes
                _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
            });

            _ = builder.HasKey(o => o.Id);

            _ = builder.Property(o => o.Id)
                .HasColumnName("OrderId")
                .HasConversion(id => id.Value, v => new OrderId(v))
                .ValueGeneratedOnAdd();


            _ = builder.Property(o => o.OrderNumber)
                .HasConversion(v => v.Value, v => OrderNumber.FromDb(v))
                .HasColumnName("OrderNumber")
                .HasMaxLength(20)
                .IsRequired();

            _ = builder.HasIndex(o => o.OrderNumber).IsUnique();

            _ = builder.Property(o => o.CustomerId)
                .HasColumnName("CustomerId")
                .HasConversion(id => id.Value, v => new CustomerId(v))
                .IsRequired();

            _ = builder.Property(o => o.OrderDate)
                .HasColumnName("OrderDate")
                .HasColumnType("datetime2")
                .IsRequired();

            // Total (Money) - owned, table-splitting into Orders
            _ = builder.OwnsOne(o => o.Total, m =>
            {
                _ = m.ToTable("Orders", tb =>
                {
                    _ = tb.IsTemporal(ttb =>
                    {
                        _ = ttb.UseHistoryTable("OrdersHistory");
                        _ = ttb.HasPeriodStart("RowValidFrom");
                        _ = ttb.HasPeriodEnd("RowValidUntil");
                    });

                    // Mapping Attributes to Parent Attributes
                    _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                    _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
                });

                _ = m.Property(x => x.Amount)
                    .HasColumnName("TotalAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                _ = m.Property(x => x.Currency)
                    .HasColumnName("TotalCurrency")
                    .HasColumnType("nchar(3)")
                    .IsRequired();
            });

            // DeliveryAddress (Address) - owned, table-splitting into Orders
            _ = builder.OwnsOne(o => o.DeliveryAddress, a =>
            {
                _ = a.ToTable("Orders", tb =>
                {
                    _ = tb.IsTemporal(ttb =>
                    {
                        _ = ttb.UseHistoryTable("OrdersHistory");
                        _ = ttb.HasPeriodStart("RowValidFrom");
                        _ = ttb.HasPeriodEnd("RowValidUntil");
                    });

                    _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                    _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
                });

                _ = a.Property(x => x.Street).HasColumnName("DeliveryStreet").HasMaxLength(200).IsRequired();
                _ = a.Property(x => x.Number).HasColumnName("DeliveryHouseNumber").HasMaxLength(20).IsRequired();
                _ = a.Property(x => x.PostalCode).HasColumnName("DeliveryPostalCode").HasMaxLength(20).IsRequired();
                _ = a.Property(x => x.City).HasColumnName("DeliveryCity").HasMaxLength(100).IsRequired();
                _ = a.Property(x => x.Country).HasColumnName("DeliveryCountryCode").HasColumnType("nchar(2)").IsRequired();
            });

            builder.Metadata.FindNavigation(nameof(Order.Lines))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);


            // Relational Connections

            // ERM: Order (1) -> (n) OrderLines
            _ = builder.HasMany(o => o.Lines)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);

            // ERM: Customer (1) -> (n) Orders
            _ = builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = builder.HasIndex(o => o.CustomerId);
        }
    }
}
