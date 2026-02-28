using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.EntityConfigurations
{
    public class ArticleGroupConfiguration : IEntityTypeConfiguration<ArticleGroup>
    {
        public void Configure(EntityTypeBuilder<ArticleGroup> builder)
        {
            // Table + Temporal (mono-temporal / system time)
            _ = builder.ToTable("ArticleGroups", tb =>
            {
                _ = tb.IsTemporal(ttb =>
                {
                    _ = ttb.UseHistoryTable("ArticleGroupsHistory");
                    _ = ttb.HasPeriodStart("RowValidFrom");
                    _ = ttb.HasPeriodEnd("RowValidUntil");
                });
            });

            // Primary key
            _ = builder.HasKey(g => g.Id);

            // Map strongly-typed ArticleGroupId to int
            _ = builder.Property(g => g.Id)
                   .HasConversion(id => id.Value, v => new ArticleGroupId(v))
                   .ValueGeneratedNever();

            // Name property
            _ = builder.Property(g => g.Name)
                   .HasMaxLength(150)
                   .IsRequired();

            // ParentGroupId (self-referencing FK) - nullable
            _ = builder.Property(g => g.ParentGroupId)
                   .HasColumnName("ParentGroupId")
                   .HasConversion(
                       p => p.HasValue ? p.Value.Value : (int?)null,
                       v => v.HasValue ? new ArticleGroupId(v.Value) : null);

            // Self-referencing relationship (hierarchical)
            _ = builder.HasOne<ArticleGroup>()           // Parent
                   .WithMany(g => g.Children)       // Children collection
                   .HasForeignKey(g => g.ParentGroupId)
                   .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade delete

            // Optional: Articles relationship (many-to-many or owned)
            _ = builder.HasMany<Article>()
                   .WithOne()  // No navigation property needed
                   .HasForeignKey(a => a.ArticleGroupId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Other properties
            _ = builder.Property(g => g.Description)
                   .HasMaxLength(500);

            _ = builder.Property(g => g.Status);

            // Indexes for performance
            _ = builder.HasIndex(g => g.ParentGroupId);
            _ = builder.HasIndex(g => g.Status);
            _ = builder.HasIndex(g => g.Name);  // Fast name lookups
        }
    }
}
