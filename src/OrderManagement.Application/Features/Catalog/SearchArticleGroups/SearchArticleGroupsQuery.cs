namespace OrderManagement.Application.Features.Catalog.SearchArticleGroups
{
    public sealed record SearchArticleGroupsQuery(string? SearchTerm, int? ParentGroupId);
}
