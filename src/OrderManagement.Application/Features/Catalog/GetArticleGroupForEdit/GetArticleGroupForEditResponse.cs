namespace OrderManagement.Application.Features.Catalog.GetArticleGroupForEdit
{
    public sealed record GetArticleGroupForEditResponse(
        int ArticleGroupId,
        string Name,
        int? ParentGroupId);
}
