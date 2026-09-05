using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.DeleteArticle
{
    public sealed class DeleteArticleUseCase(
        IArticleCommandRepository articleCommandRepository,
        IOrderQueryRepository orderQueryRepository,
        IUnitOfWork unitOfWork) : IDeleteArticleUseCase
    {
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            DeleteArticleCommand command,
            CancellationToken cancellationToken = default)
        {
            Article? article = await _articleCommandRepository.GetByIdAsync(
                new ArticleId(command.ArticleId),
                cancellationToken);

            if (article is null)
            {
                return Result.Fail("Article was not found.");
            }

            bool isReferenced = await _orderQueryRepository.ExistsOrderLineForArticleAsync(article.Id, cancellationToken);
            if (isReferenced)
            {
                return Result.Fail(DeleteArticleErrorCodes.ArticleInUse);
            }

            _articleCommandRepository.Remove(article);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
