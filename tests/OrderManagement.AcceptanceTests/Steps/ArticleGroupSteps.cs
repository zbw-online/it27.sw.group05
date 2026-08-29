using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.AcceptanceTests.Support;
using OrderManagement.Application.DTOs.Catalog;
using OrderManagement.Application.Features.Catalog.CreateArticleGroup;
using OrderManagement.Application.Features.Catalog.DeleteArticleGroup;
using OrderManagement.Application.Features.Catalog.GetArticleGroupForEdit;
using OrderManagement.Application.Features.Catalog.GetArticleGroupHierarchy;
using OrderManagement.Application.Features.Catalog.RenameArticleGroup;
using OrderManagement.Application.Features.Catalog.SearchArticleGroups;
using OrderManagement.Application.Features.Catalog.Shared;

using Reqnroll;

using SharedKernel.Primitives;

namespace OrderManagement.AcceptanceTests.Steps
{
    [Binding]
    public sealed class ArticleGroupSteps(
        ICreateArticleGroupUseCase createArticleGroupUseCase,
        IRenameArticleGroupUseCase renameArticleGroupUseCase,
        IDeleteArticleGroupUseCase deleteArticleGroupUseCase,
        IGetArticleGroupForEditUseCase getArticleGroupForEditUseCase,
        ISearchArticleGroupsUseCase searchArticleGroupsUseCase,
        IGetArticleGroupHierarchyUseCase getArticleGroupHierarchyUseCase,
        AcceptanceTestContext context)
    {
        private Result<CreateArticleGroupResponse>? _lastCreateResult;
        private Result? _lastCommandResult;
        private IReadOnlyList<ArticleGroupHierarchyDto>? _lastHierarchy;

        [Given(@"the article group ""([^""]*)"" exists")]
        public async Task GivenTheArticleGroupExists(string groupName)
            => await CreateGroupIfMissingAsync(groupName, null);

        [Given(@"the article group ""([^""]*)"" exists under ""([^""]*)""")]
        public async Task GivenTheArticleGroupExistsUnder(string groupName, string parentName)
            => await CreateGroupIfMissingAsync(groupName, parentName);

        [When(@"I create the top-level article group ""([^""]*)""")]
        public async Task WhenICreateTheTopLevelArticleGroup(string groupName)
        {
            _lastCreateResult = await createArticleGroupUseCase.ExecuteAsync(new CreateArticleGroupCommand(groupName, null));

            if (_lastCreateResult.Value.IsSuccess)
            {
                context.ArticleGroupIdsByName[groupName] = _lastCreateResult.Value.Value!.ArticleGroupId;
            }
        }

        [When(@"I create the article group ""([^""]*)"" under ""([^""]*)""")]
        public async Task WhenICreateTheArticleGroupUnder(string groupName, string parentName)
        {
            int parentId = context.ArticleGroupIdsByName[parentName];
            _lastCreateResult = await createArticleGroupUseCase.ExecuteAsync(new CreateArticleGroupCommand(groupName, parentId));

            if (_lastCreateResult.Value.IsSuccess)
            {
                context.ArticleGroupIdsByName[groupName] = _lastCreateResult.Value.Value!.ArticleGroupId;
            }
        }

        [When(@"I rename article group ""([^""]*)"" to ""([^""]*)""")]
        public async Task WhenIRenameArticleGroupTo(string groupName, string newName)
        {
            int groupId = context.ArticleGroupIdsByName[groupName];
            _lastCommandResult = await renameArticleGroupUseCase.ExecuteAsync(new RenameArticleGroupCommand(groupId, newName));

            Assert.IsTrue(_lastCommandResult.Value.IsSuccess, _lastCommandResult.Value.Error);
            context.ArticleGroupIdsByName[newName] = groupId;
            _ = context.ArticleGroupIdsByName.Remove(groupName);
        }

        [When(@"I delete article group ""([^""]*)""")]
        public async Task WhenIDeleteArticleGroup(string groupName)
        {
            int groupId = context.ArticleGroupIdsByName[groupName];
            _lastCommandResult = await deleteArticleGroupUseCase.ExecuteAsync(new DeleteArticleGroupCommand(groupId));
        }

        [When(@"I view the article group hierarchy starting at ""([^""]*)""")]
        public async Task WhenIViewTheArticleGroupHierarchyStartingAt(string groupName)
        {
            int groupId = context.ArticleGroupIdsByName[groupName];
            Result<IReadOnlyList<ArticleGroupHierarchyDto>> result = await getArticleGroupHierarchyUseCase.ExecuteAsync(
                new GetArticleGroupHierarchyQuery(groupId));

            Assert.IsTrue(result.IsSuccess, result.Error);
            _lastHierarchy = result.Value;
        }

        [When(@"I view the full article group hierarchy")]
        public async Task WhenIViewTheFullArticleGroupHierarchy()
        {
            Result<IReadOnlyList<ArticleGroupHierarchyDto>> result = await getArticleGroupHierarchyUseCase.ExecuteAsync(
                new GetArticleGroupHierarchyQuery(null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            _lastHierarchy = result.Value;
        }

        [Then(@"the article group ""([^""]*)"" exists with no parent")]
        public async Task ThenTheArticleGroupExistsWithNoParent(string groupName)
        {
            GetArticleGroupForEditResponse group = await GetGroupAsync(groupName);
            Assert.IsNull(group.ParentGroupId);
        }

        [Then(@"the article group ""([^""]*)"" exists with parent ""([^""]*)""")]
        public async Task ThenTheArticleGroupExistsWithParent(string groupName, string parentName)
        {
            GetArticleGroupForEditResponse group = await GetGroupAsync(groupName);
            Assert.AreEqual(context.ArticleGroupIdsByName[parentName], group.ParentGroupId);
        }

        [Then(@"the article group ""([^""]*)"" exists")]
        public async Task ThenTheArticleGroupExists(string groupName)
            => Assert.IsTrue(await GroupExistsByNameAsync(groupName));

        [Then(@"the article group ""([^""]*)"" no longer exists")]
        public async Task ThenTheArticleGroupNoLongerExists(string groupName)
            => Assert.IsFalse(await GroupExistsByNameAsync(groupName));

        [Then(@"the deletion is rejected because the group still contains articles")]
        public void ThenTheDeletionIsRejectedBecauseTheGroupStillContainsArticles()
        {
            Assert.IsFalse(_lastCommandResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCommandResult.Value.Error, "articles");
        }

        [Then(@"the deletion is rejected because the group still has child groups")]
        public void ThenTheDeletionIsRejectedBecauseTheGroupStillHasChildGroups()
        {
            Assert.IsFalse(_lastCommandResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCommandResult.Value.Error, "child groups");
        }

        [Then(@"the hierarchy contains ""([^""]*)"", ""([^""]*)"" and ""([^""]*)"" in that parent order")]
        public void ThenTheHierarchyContainsInThatParentOrder(string first, string second, string third)
        {
            List<string> names = [.. _lastHierarchy!.Select(h => h.Name)];
            int firstIndex = names.IndexOf(first);
            int secondIndex = names.IndexOf(second);
            int thirdIndex = names.IndexOf(third);

            Assert.IsTrue(firstIndex >= 0 && secondIndex >= 0 && thirdIndex >= 0, "All three groups must be present in the hierarchy.");
            Assert.IsTrue(firstIndex < secondIndex && secondIndex < thirdIndex, "Groups must appear in parent-to-child order.");
        }

        [Then(@"""([^""]*)"" is (\d+) levels? below ""([^""]*)"" in the hierarchy")]
        public void ThenIsLevelsBelowInTheHierarchy(string descendantName, int expectedLevels, string ancestorName)
        {
            ArticleGroupHierarchyDto ancestor = _lastHierarchy!.Single(h => h.Name == ancestorName);
            ArticleGroupHierarchyDto descendant = _lastHierarchy!.Single(h => h.Name == descendantName);
            Assert.AreEqual(expectedLevels, descendant.Level - ancestor.Level);
        }

        [Then(@"the hierarchy contains both ""([^""]*)"" and ""([^""]*)"" as top-level groups")]
        public void ThenTheHierarchyContainsBothAsTopLevelGroups(string first, string second)
        {
            Assert.IsTrue(_lastHierarchy!.Any(h => h.Name == first && h.ParentGroupId is null));
            Assert.IsTrue(_lastHierarchy!.Any(h => h.Name == second && h.ParentGroupId is null));
        }

        private async Task CreateGroupIfMissingAsync(string groupName, string? parentName)
        {
            if (context.ArticleGroupIdsByName.ContainsKey(groupName))
            {
                return;
            }

            int? parentId = parentName is null ? null : context.ArticleGroupIdsByName[parentName];

            Result<CreateArticleGroupResponse> result = await createArticleGroupUseCase.ExecuteAsync(
                new CreateArticleGroupCommand(groupName, parentId));

            Assert.IsTrue(result.IsSuccess, result.Error);
            context.ArticleGroupIdsByName[groupName] = result.Value!.ArticleGroupId;
        }

        private async Task<GetArticleGroupForEditResponse> GetGroupAsync(string groupName)
        {
            int groupId = context.ArticleGroupIdsByName[groupName];
            Result<GetArticleGroupForEditResponse> result = await getArticleGroupForEditUseCase.ExecuteAsync(
                new GetArticleGroupForEditQuery(groupId));

            Assert.IsTrue(result.IsSuccess, result.Error);
            return result.Value!;
        }

        private async Task<bool> GroupExistsByNameAsync(string groupName)
        {
            Result<IReadOnlyList<ArticleGroupListItemDto>> result = await searchArticleGroupsUseCase.ExecuteAsync(
                new SearchArticleGroupsQuery(groupName, null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            return result.Value!.Any(g => g.Name == groupName);
        }
    }
}
