namespace OrderManagement.Application.Features.Catalog.Shared
{
    public sealed record ArticleGroupListItemDto(
        int ArticleGroupId,
        string Name,
        int? ParentGroupId,
        string? ParentGroupName);
}
