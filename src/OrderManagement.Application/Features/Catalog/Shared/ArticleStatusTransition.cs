using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.Shared
{
    internal static class ArticleStatusTransition
    {
        public static async Task<Result> ExecuteAsync(
            IArticleCommandRepository articleCommandRepository,
            IUnitOfWork unitOfWork,
            int articleId,
            Func<Article, Result> transition,
            CancellationToken cancellationToken)
        {
            Article? article = await articleCommandRepository.GetByIdAsync(new ArticleId(articleId), cancellationToken);
            if (article is null)
            {
                return Result.Fail("Article was not found.");
            }

            Result transitionResult = transition(article);
            if (!transitionResult.IsSuccess)
            {
                return transitionResult;
            }

            articleCommandRepository.Update(article);

            return await unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
