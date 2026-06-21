namespace OrderManagement.Application.Features.Catalog.CreateArticleGroup
{
    public sealed record CreateArticleGroupResponse(
        int ArticleGroupId,
        string Name,
        int? ParentGroupId);
}
