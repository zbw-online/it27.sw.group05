using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public sealed class ArticleGroupConfiguration : IEntityTypeConfiguration<ArticleGroup>
    {
        public void Configure(EntityTypeBuilder<ArticleGroup> builder)
        {
            _ = builder.ToTable("ArticleGroups", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("ArticleGroupsHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });

                _ = tb.Property<DateTime>("RowValidFrom").HasColumnName("RowValidFrom");
                _ = tb.Property<DateTime>("RowValidUntil").HasColumnName("RowValidUntil");
            });

            _ = builder.HasKey(g => g.Id);

            _ = builder.Property(g => g.Id)
                .HasColumnName("ArticleGroupId")
                .HasConversion(id => id.Value, v => new ArticleGroupId(v))
                .ValueGeneratedNever();

            _ = builder.Property(g => g.Name)
                .HasMaxLength(150)
                .IsRequired();

            _ = builder.Property(g => g.ParentGroupId)
                .HasColumnName("ParentGroupId")
                .HasConversion(
                    p => p.HasValue ? p.Value.Value : (int?)null,
                    v => v.HasValue ? new ArticleGroupId(v.Value) : null);

            // Backing field mapping for Children
            builder.Metadata.FindNavigation(nameof(ArticleGroup.Children))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            _ = builder.HasOne<ArticleGroup>()       // Parent
                .WithMany(g => g.Children)       // Children
                .HasForeignKey(g => g.ParentGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            _ = builder.Property(g => g.Description)
                .HasMaxLength(500);

            _ = builder.Property(g => g.Status).IsRequired();

            _ = builder.HasIndex(g => g.ParentGroupId);
            _ = builder.HasIndex(g => g.Status);
            _ = builder.HasIndex(g => g.Name);
        }
    }
}
