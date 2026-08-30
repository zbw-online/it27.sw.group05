using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetTopSellingArticles
{
    public sealed class GetTopSellingArticlesUseCase(
        IOrderQueryRepository orderQueryRepository,
        IArticleQueryRepository articleQueryRepository) : IGetTopSellingArticlesUseCase
    {
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;

        public async Task<Result<IReadOnlyList<TopSellingArticleDto>>> ExecuteAsync(
            GetTopSellingArticlesQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Order> orders = await _orderQueryRepository.GetListAsync(cancellationToken);
            IReadOnlyList<Article> articles = await _articleQueryRepository.GetListAsync(cancellationToken);

            var articleNumberById = articles.ToDictionary(a => a.Id.Value, a => a.ArticleNumber.Value);

            IReadOnlyList<TopSellingArticleDto> result = [.. orders
                .SelectMany(o => o.Lines.Select(line => (OrderId: o.Id.Value, Line: line)))
                .GroupBy(x => x.Line.ArticleId.Value)
                .Select(g => new TopSellingArticleDto(
                    g.Key,
                    articleNumberById.TryGetValue(g.Key, out string? number) ? number : string.Empty,
                    g.First().Line.ArticleName,
                    g.Sum(x => x.Line.Quantity),
                    g.Select(x => x.OrderId).Distinct().Count()))
                .OrderByDescending(dto => dto.TotalQuantity)
                .Take(query.Limit)];

            return Results.Success(result);
        }
    }
}
