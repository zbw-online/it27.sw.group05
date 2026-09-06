using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.DeleteArticleGroup
{
    public sealed class DeleteArticleGroupUseCase(
        IArticleGroupCommandRepository articleGroupCommandRepository,
        IArticleQueryRepository articleQueryRepository,
        IUnitOfWork unitOfWork) : IDeleteArticleGroupUseCase
    {
        private readonly IArticleGroupCommandRepository _articleGroupCommandRepository = articleGroupCommandRepository;
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            DeleteArticleGroupCommand command,
            CancellationToken cancellationToken = default)
        {
            var groupId = new ArticleGroupId(command.ArticleGroupId);

            ArticleGroup? group = await _articleGroupCommandRepository.GetByIdWithChildrenAsync(
                groupId,
                cancellationToken);

            if (group is null)
            {
                return Result.Fail("Article group was not found.");
            }

            if (group.Children.Count > 0)
            {
                return Result.Fail("Cannot delete a group that has child groups.");
            }

            IReadOnlyList<Article> articles = await _articleQueryRepository.GetByGroupAsync(
                groupId,
                cancellationToken);

            if (articles.Count > 0)
            {
                return Result.Fail("Cannot delete a group that still contains articles.");
            }

            _articleGroupCommandRepository.Remove(group);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
