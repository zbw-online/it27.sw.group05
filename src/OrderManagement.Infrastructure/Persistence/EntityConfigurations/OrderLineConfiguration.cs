using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
    {
        public void Configure(EntityTypeBuilder<OrderLine> builder)
        {
            _ = builder.ToTable("OrderLines", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("OrderLinesHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });

                _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
            });

            _ = builder.HasKey(l => l.Id);

            _ = builder.Property(l => l.Id)
                .HasColumnName("OrderLineId")
                .HasConversion(id => id.Value, v => new OrderLineId(v))
                .ValueGeneratedOnAdd();

            // Shadow FK typed as OrderId VO (int in DB)
            _ = builder.Property<OrderId>("OrderId")
                .HasColumnName("OrderId")
                .HasConversion(id => id.Value, v => new OrderId(v))
                .IsRequired();

            _ = builder.Property(l => l.LineNumber).HasColumnName("LineNumber").IsRequired();

            _ = builder.Property(l => l.ArticleId)
                .HasColumnName("ArticleId")
                .HasConversion(id => id.Value, v => new ArticleId(v))
                .IsRequired();

            _ = builder.Property(l => l.ArticleName).HasColumnName("ArticleName").HasMaxLength(200).IsRequired();

            _ = builder.Property(l => l.Quantity).HasColumnName("Quantity").IsRequired();

            _ = builder.OwnsOne(l => l.UnitPrice, m =>
            {
                _ = m.ToTable("OrderLines", tb =>
                {
                    _ = tb.IsTemporal(ttb =>
                    {
                        _ = ttb.UseHistoryTable("OrderLinesHistory");
                        _ = ttb.HasPeriodStart("RowValidFrom");
                        _ = ttb.HasPeriodEnd("RowValidUntil");
                    });


                    // Mapping Attributes to Parent Attributes
                    _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                    _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
                });

                _ = m.Property(x => x.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 2).IsRequired();
                _ = m.Property(x => x.Currency).HasColumnName("UnitPriceCurrency").HasColumnType("nchar(3)").IsRequired();
            });

            _ = builder.OwnsOne(l => l.LineTotal, m =>
            {
                _ = m.ToTable("OrderLines", tb =>
                {
                    _ = tb.IsTemporal(ttb =>
                    {
                        _ = ttb.UseHistoryTable("OrderLinesHistory");
                        _ = ttb.HasPeriodStart("RowValidFrom");
                        _ = ttb.HasPeriodEnd("RowValidUntil");
                    });

                    // Mapping Attributes to Parent Attributes
                    _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                    _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
                });

                _ = m.Property(x => x.Amount).HasColumnName("LineTotalAmount").HasPrecision(18, 2).IsRequired();
                _ = m.Property(x => x.Currency).HasColumnName("LineTotalCurrency").HasColumnType("nchar(3)").IsRequired();
            });

            _ = builder.HasIndex("OrderId");
            _ = builder.HasIndex("OrderId", "LineNumber").IsUnique();

            // Relational Conections

            // OrderLines -> Articles (FK)
            _ = builder.HasOne<Article>()
                .WithMany()
                .HasForeignKey(l => l.ArticleId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = builder.HasIndex(l => l.ArticleId);
        }
    }
}
