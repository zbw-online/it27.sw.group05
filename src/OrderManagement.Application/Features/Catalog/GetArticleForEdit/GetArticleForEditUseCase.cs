using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.GetArticleForEdit
{
    public sealed class GetArticleForEditUseCase(IArticleQueryRepository articleQueryRepository) : IGetArticleForEditUseCase
    {
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;

        public async Task<Result<GetArticleForEditResponse>> ExecuteAsync(
            GetArticleForEditQuery query,
            CancellationToken cancellationToken = default)
        {
            Article? article = await _articleQueryRepository.GetByIdAsync(
                new ArticleId(query.ArticleId),
                cancellationToken);

            return article is null
                ? Results.Fail<GetArticleForEditResponse>("Article was not found.")
                : Results.Success(new GetArticleForEditResponse(
                article.Id.Value,
                article.ArticleNumber.Value,
                article.Name,
                article.Price.Amount,
                article.Price.Currency,
                article.ArticleGroupId.Value,
                article.Stock,
                article.VatRate,
                article.Description,
                article.Status));
        }
    }
}
