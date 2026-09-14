using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Application.Features.Catalog.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.ReactivateArticle
{
    public sealed class ReactivateArticleUseCase(
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : IReactivateArticleUseCase
    {
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            ReactivateArticleCommand command,
            CancellationToken cancellationToken = default)
            => await ArticleStatusTransition.ExecuteAsync(
                _articleCommandRepository,
                _unitOfWork,
                command.ArticleId,
                static article => article.Reactivate(),
                cancellationToken);
    }
}
