namespace OrderManagement.Application.Features.Catalog.RenameArticleGroup
{
    public sealed record RenameArticleGroupCommand(int ArticleGroupId, string Name);
}
