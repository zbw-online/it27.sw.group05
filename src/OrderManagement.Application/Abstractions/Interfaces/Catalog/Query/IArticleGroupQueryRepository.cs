using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Catalog.Query
{
    public interface IArticleGroupQueryRepository : IQueryRepository<ArticleGroup, ArticleGroupId>
    {
        Task<ArticleGroup?> GetByIdWithChildrenAsync(
            ArticleGroupId id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleGroup>> GetByParentAsync(
            ArticleGroupId? parentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetHierarchyFromRootAsync(
            ArticleGroupId rootId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ArticleGroupHierarchyDto>> GetFullHierarchyAsync(
            CancellationToken cancellationToken = default);
    }

    // ACHTUNG ! DTOs sollten nicht hier gespeichert werden
    // DTOs sind später vor allem für die Kommunikation zwischen Application und Presentation Layer
    // DTOs sind ein "Spiegel" der Use Cases oder die Objekte / Information, das Presentation Layer effektiv braucht.  
    // Leiber die DTOs in dem IntegrationTests einfügen. Dafür sind ja tests da ;)
    public sealed record ArticleGroupHierarchyDto(
        int Id,
        string Name,
        int? ParentGroupId,
        int Level,
        string Path);
}
