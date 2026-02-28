using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public class ArticleConfiguration : IEntityTypeConfiguration<Article>
    {
        public void Configure(EntityTypeBuilder<Article> builder)
        {
            // Primary key
            _ = builder.HasKey(a => a.Id);

            // Map strongly-typed ArticleId to int
            _ = builder.Property(a => a.Id)
                   .HasConversion(
                       id => id.Value,
                       v => new ArticleId(v))
                   .ValueGeneratedNever();

            // Map strongly-typed ArticleGroupId to int (FK)
            _ = builder.Property(a => a.ArticleGroupId)
                   .HasColumnName("ArticleGroupId")
                   .HasConversion(
                       gid => gid.Value,
                       v => new ArticleGroupId(v));

            // Value Object as Owned Type
            // Note: Temporal tables not enabled for Article because owned entities
            // sharing the same table have complex temporal configuration requirements
            _ = builder.OwnsOne(a => a.ArticleNumber, nb =>
            {
                _ = nb.Property(p => p.Value)  // Maps ArticleNumber.Value → varchar(20)
                  .HasColumnName("ArticleNumber")
                  .HasMaxLength(20)
                  .IsRequired();
            });

            // Money Value Object
            _ = builder.OwnsOne(a => a.Price, p =>
            {
                _ = p.Property(pm => pm.Amount)
                 .HasColumnName("PriceAmount")
                 .HasPrecision(18, 2);

                _ = p.Property(pm => pm.Currency)
                 .HasColumnName("PriceCurrency")
                 .HasMaxLength(3);
            });

            // Other properties
            _ = builder.Property(a => a.Name)
                   .HasMaxLength(200)
                   .IsRequired();

            _ = builder.Property(a => a.Stock);
            _ = builder.Property(a => a.VatRate)
                   .HasPrecision(5, 2);
            _ = builder.Property(a => a.Status);
            _ = builder.Property(a => a.Description)
                   .HasMaxLength(500);
        }
    }
}
