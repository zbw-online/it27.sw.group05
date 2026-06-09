namespace OrderManagement.Application.DTOs.Catalog
{
    public sealed record ArticleGroupHierarchyDto(
        int Id,
        string Name,
        int? ParentGroupId,
        int Level,
        string Path);
}
