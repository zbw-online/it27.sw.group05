namespace OrderManagement.Application.Features.Catalog.Contracts
{
    public sealed record ArticleGroupHierarchyDto(
        int Id,
        string Name,
        int? ParentGroupId,
        int Level,
        string Path);
}
