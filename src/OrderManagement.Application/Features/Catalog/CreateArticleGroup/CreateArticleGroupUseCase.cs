using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.CreateArticleGroup
{
    public sealed class CreateArticleGroupUseCase(
        IArticleGroupCommandRepository articleGroupCommandRepository,
        IUnitOfWork unitOfWork) : ICreateArticleGroupUseCase
    {
        private readonly IArticleGroupCommandRepository _articleGroupCommandRepository = articleGroupCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<CreateArticleGroupResponse>> ExecuteAsync(
            CreateArticleGroupCommand command,
            CancellationToken cancellationToken = default)
        {
            ArticleGroupId? parentGroupId = command.ParentGroupId.HasValue
                ? new ArticleGroupId(command.ParentGroupId.Value)
                : null;

            if (parentGroupId.HasValue)
            {
                ArticleGroup? parent = await _articleGroupCommandRepository.GetByIdAsync(
                    parentGroupId.Value,
                    cancellationToken);

                if (parent is null)
                {
                    return Results.Fail<CreateArticleGroupResponse>("Parent group was not found.");
                }
            }

            Result<ArticleGroup> groupResult = ArticleGroup.Create(command.Name, parentGroupId);
            if (!groupResult.IsSuccess)
            {
                return Results.Fail<CreateArticleGroupResponse>(groupResult.Error!);
            }

            ArticleGroup group = groupResult.Value!;

            _articleGroupCommandRepository.Add(group);

            Result commitResult = await _unitOfWork.CommitAsync(cancellationToken);
            return !commitResult.IsSuccess
                ? Results.Fail<CreateArticleGroupResponse>(commitResult.Error!)
                : Results.Success(new CreateArticleGroupResponse(
                    group.Id.Value,
                    group.Name,
                    group.ParentGroupId?.Value));
        }
    }
}
