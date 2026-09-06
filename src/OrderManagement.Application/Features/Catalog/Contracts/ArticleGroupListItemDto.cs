namespace OrderManagement.Application.Features.Catalog.Contracts
{
    public sealed record ArticleGroupListItemDto(
        int ArticleGroupId,
        string Name,
        int? ParentGroupId,
        string? ParentGroupName);
}
