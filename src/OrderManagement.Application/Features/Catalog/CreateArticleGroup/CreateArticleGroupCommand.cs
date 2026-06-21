namespace OrderManagement.Application.Features.Catalog.CreateArticleGroup
{
    public sealed record CreateArticleGroupCommand(
        string Name,
        int? ParentGroupId);
}
