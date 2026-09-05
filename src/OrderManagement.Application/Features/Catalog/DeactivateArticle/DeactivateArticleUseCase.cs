using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Application.Features.Catalog.Shared;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.DeactivateArticle
{
    public sealed class DeactivateArticleUseCase(
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : IDeactivateArticleUseCase
    {
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            DeactivateArticleCommand command,
            CancellationToken cancellationToken = default)
            => await ArticleStatusTransition.ExecuteAsync(
                _articleCommandRepository,
                _unitOfWork,
                command.ArticleId,
                static article => article.Deactivate(),
                cancellationToken);
    }
}
