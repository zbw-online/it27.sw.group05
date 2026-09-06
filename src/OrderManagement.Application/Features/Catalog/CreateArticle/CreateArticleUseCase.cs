using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.CreateArticle
{
    public sealed class CreateArticleUseCase(
        IArticleCommandRepository articleCommandRepository,
        IArticleQueryRepository articleQueryRepository,
        IUnitOfWork unitOfWork) : ICreateArticleUseCase
    {
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<CreateArticleResponse>> ExecuteAsync(
            CreateArticleCommand command,
            CancellationToken cancellationToken = default)
        {
            Result<ArticleNumber> numberResult = ArticleNumber.Create(command.ArticleNumber);
            if (!numberResult.IsSuccess)
            {
                return Results.Fail<CreateArticleResponse>(numberResult.Error!);
            }

            Article? existing = await _articleQueryRepository.GetByNumberAsync(
                numberResult.Value!,
                cancellationToken);

            if (existing is not null)
            {
                return Results.Fail<CreateArticleResponse>(
                    $"Article number '{command.ArticleNumber}' already exists.");
            }

            Result<Article> articleResult = Article.Create(
                command.ArticleNumber,
                command.Name,
                command.PriceAmount,
                command.PriceCurrency,
                new ArticleGroupId(command.GroupId),
                command.Stock,
                command.ReorderPoint,
                command.VatRate,
                command.Description);

            if (!articleResult.IsSuccess)
            {
                return Results.Fail<CreateArticleResponse>(articleResult.Error!);
            }

            Article article = articleResult.Value!;

            _articleCommandRepository.Add(article);

            Result commitResult = await _unitOfWork.CommitAsync(cancellationToken);
            return !commitResult.IsSuccess
                ? Results.Fail<CreateArticleResponse>(commitResult.Error!)
                : Results.Success(new CreateArticleResponse(
                    article.Id.Value,
                    article.ArticleNumber.Value,
                    article.Name,
                    article.Price.Amount,
                    article.Price.Currency));
        }
    }
}
