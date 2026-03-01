using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query
{
    public sealed class ArticleGroupHierarchyQueryRepository(OrderManagementDbContext context) : IArticleGroupHierarchyQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        // primitive row for reliable materialization
        private sealed record ArticleGroupHierarchyRow(
            int Id,
            string Name,
            int? ParentGroupId,
            int Level,
            string Path);

        public async Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetHierarchyFromRootAsync(
            ArticleGroupId rootId,
            CancellationToken ct = default)
        {
            FormattableString sql = $@"
WITH ArticleGroupHierarchy AS
(
    SELECT 
        ArticleGroupId AS Id,
        Name,
        ParentGroupId,
        0 AS Level,
        CAST(Name AS NVARCHAR(4000)) AS Path
    FROM ArticleGroups
    WHERE ArticleGroupId = {rootId.Value}

    UNION ALL

    SELECT 
        ag.ArticleGroupId AS Id,
        ag.Name,
        ag.ParentGroupId,
        agh.Level + 1,
        CAST(agh.Path + ' > ' + ag.Name AS NVARCHAR(4000)) AS Path
    FROM ArticleGroups ag
    INNER JOIN ArticleGroupHierarchy agh ON ag.ParentGroupId = agh.Id
)
SELECT 
    Id,
    Name,
    ParentGroupId,
    Level,
    Path
FROM ArticleGroupHierarchy
ORDER BY Path;";

            List<ArticleGroupHierarchyRow> rows = await _context.Database
                .SqlQuery<ArticleGroupHierarchyRow>(sql)
                .ToListAsync(ct);

            return [.. rows.Select(Map)];
        }

        public async Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetFullHierarchyAsync(
            CancellationToken ct = default)
        {
            FormattableString sql = $@"
WITH ArticleGroupHierarchy AS
(
    SELECT 
        ArticleGroupId AS Id,
        Name,
        ParentGroupId,
        0 AS Level,
        CAST(Name AS NVARCHAR(4000)) AS Path
    FROM ArticleGroups
    WHERE ParentGroupId IS NULL

    UNION ALL

    SELECT 
        ag.ArticleGroupId AS Id,
        ag.Name,
        ag.ParentGroupId,
        agh.Level + 1,
        CAST(agh.Path + ' > ' + ag.Name AS NVARCHAR(4000)) AS Path
    FROM ArticleGroups ag
    INNER JOIN ArticleGroupHierarchy agh ON ag.ParentGroupId = agh.Id
)
SELECT 
    Id,
    Name,
    ParentGroupId,
    Level,
    Path
FROM ArticleGroupHierarchy
ORDER BY Path;";

            List<ArticleGroupHierarchyRow> rows = await _context.Database
                .SqlQuery<ArticleGroupHierarchyRow>(sql)
                .ToListAsync(ct);

            return [.. rows.Select(Map)];
        }

        private static ArticleGroupHierarchyDto Map(ArticleGroupHierarchyRow r)
            => new(
                new ArticleGroupId(r.Id),
                r.Name,
                r.ParentGroupId is null ? null : new ArticleGroupId(r.ParentGroupId.Value),
                r.Level,
                r.Path);
    }
}
