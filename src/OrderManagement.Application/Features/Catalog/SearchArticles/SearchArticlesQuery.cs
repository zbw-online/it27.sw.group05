using OrderManagement.Domain.Catalog.ValueObjects;

namespace OrderManagement.Application.Features.Catalog.SearchArticles
{
    public sealed record SearchArticlesQuery(string? SearchTerm, int? GroupId, ArticleStatus? StatusFilter = null);
}
