using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query
{
    public class ArticleGroupQueryRepository(OrderManagementDbContext context) : IArticleGroupQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<ArticleGroup?> GetByIdAsync(
            ArticleGroupId id,
            CancellationToken ct = default)
            => await _context.Set<ArticleGroup>()
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id, ct);

        public async Task<IReadOnlyList<ArticleGroup>> GetListAsync(
            CancellationToken ct = default)
            => await _context.Set<ArticleGroup>()
                .AsNoTracking()
                .ToListAsync(ct);

        public async Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default)
            => await _context.Set<ArticleGroup>()
                .AsNoTracking()
                .Include(g => g.Children)
                .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        public async Task<IReadOnlyList<ArticleGroup>> GetByParentAsync(
            ArticleGroupId? parentId,
            CancellationToken cancellationToken = default)
            => await _context.Set<ArticleGroup>()
                .AsNoTracking()
                .Where(g => g.ParentGroupId == parentId)
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetHierarchyFromRootAsync(
            ArticleGroupId rootId,
            CancellationToken cancellationToken = default)
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
                ORDER BY Path";

            return await _context.Database
                .SqlQuery<ArticleGroupHierarchyDto>(sql)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetFullHierarchyAsync(
            CancellationToken cancellationToken = default)
        {
            FormattableString sql = $@"
                WITH ArticleGroupHierarchy AS
                (
                    SELECT 
                        ArticleGroupId,
                        Name,
                        ParentGroupId,
                        0 AS Level,
                        CAST(Name AS NVARCHAR(4000)) AS Path
                    FROM ArticleGroups
                    WHERE ParentGroupId IS NULL

                    UNION ALL

                    SELECT 
                        ag.ArticleGroupId,
                        ag.Name,
                        ag.ParentGroupId,
                        agh.Level + 1,
                        CAST(agh.Path + ' > ' + ag.Name AS NVARCHAR(4000)) AS Path
                    FROM ArticleGroups ag
                    INNER JOIN ArticleGroupHierarchy agh ON ag.ParentGroupId = agh.ArticleGroupId
                )
                SELECT 
                    ArticleGroupId AS Id,
                    Name,
                    ParentGroupId,
                    Level,
                    Path
                FROM ArticleGroupHierarchy
                ORDER BY Path";

            return await _context.Database
                .SqlQuery<ArticleGroupHierarchyDto>(sql)
                .ToListAsync(cancellationToken);
        }
    }
}
