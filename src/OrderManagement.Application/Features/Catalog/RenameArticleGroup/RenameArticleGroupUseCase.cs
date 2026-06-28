using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.RenameArticleGroup
{
    public sealed class RenameArticleGroupUseCase(
        IArticleGroupCommandRepository articleGroupCommandRepository,
        IUnitOfWork unitOfWork) : IRenameArticleGroupUseCase
    {
        private readonly IArticleGroupCommandRepository _articleGroupCommandRepository = articleGroupCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            RenameArticleGroupCommand command,
            CancellationToken cancellationToken = default)
        {
            ArticleGroup? group = await _articleGroupCommandRepository.GetByIdAsync(
                new ArticleGroupId(command.ArticleGroupId),
                cancellationToken);

            if (group is null)
            {
                return Result.Fail("Article group was not found.");
            }

            Result renameResult = group.Rename(command.Name);
            if (!renameResult.IsSuccess)
            {
                return renameResult;
            }

            _articleGroupCommandRepository.Update(group);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
