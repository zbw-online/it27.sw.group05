using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
    {
        public void Configure(EntityTypeBuilder<Article> builder)
        {
            _ = builder.ToTable("Articles", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("ArticlesHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });

                _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
            });

            _ = builder.HasKey(a => a.Id);

            _ = builder.Property(a => a.Id)
                .HasColumnName("ArticleId")
                .HasConversion(id => id.Value, v => new ArticleId(v))
                .ValueGeneratedNever();

            _ = builder.Property(a => a.ArticleGroupId)
                .HasColumnName("ArticleGroupId")
                .HasConversion(gid => gid.Value, v => new ArticleGroupId(v))
                .IsRequired();

            _ = builder.HasIndex(a => a.ArticleGroupId);

            // ERM: ArticleGroups (1) -> (n) Articles
            _ = builder.HasOne<ArticleGroup>()
                .WithMany()
                .HasForeignKey(a => a.ArticleGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // ArticleNumber VO (temporal workaround)
            _ = builder.OwnsOne(a => a.ArticleNumber, nb =>
            {
                _ = nb.ToTable("Articles", tb =>
                {
                    _ = tb.IsTemporal(ttb =>
                    {
                        _ = ttb.UseHistoryTable("ArticlesHistory");
                        _ = ttb.HasPeriodStart("RowValidFrom");
                        _ = ttb.HasPeriodEnd("RowValidUntil");
                    });

                    _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                    _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
                });

                _ = nb.Property(p => p.Value)
                    .HasColumnName("ArticleNumber")
                    .HasMaxLength(20)
                    .IsRequired();
            });

            // Price Money VO (temporal workaround)
            _ = builder.OwnsOne(a => a.Price, p =>
            {
                _ = p.ToTable("Articles", tb =>
                {
                    _ = tb.IsTemporal(ttb =>
                    {
                        _ = ttb.UseHistoryTable("ArticlesHistory");
                        _ = ttb.HasPeriodStart("RowValidFrom");
                        _ = ttb.HasPeriodEnd("RowValidUntil");
                    });

                    _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                    _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
                });

                _ = p.Property(pm => pm.Amount)
                    .HasColumnName("PriceAmount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                _ = p.Property(pm => pm.Currency)
                    .HasColumnName("PriceCurrency")
                    .HasColumnType("nchar(3)")
                    .IsRequired();
            });

            _ = builder.Property(a => a.Name)
                .HasMaxLength(200)
                .IsRequired();

            _ = builder.Property(a => a.Stock).IsRequired();

            _ = builder.Property(a => a.VatRate)
                .HasPrecision(5, 2)
                .IsRequired();

            _ = builder.Property(a => a.Description)
                .HasColumnType("nvarchar(max)");

            _ = builder.Property(a => a.Status).IsRequired();

            _ = builder.HasIndex(a => a.Name);
        }
    }
}
