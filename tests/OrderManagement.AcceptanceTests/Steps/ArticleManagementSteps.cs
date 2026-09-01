using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.AcceptanceTests.Support;
using OrderManagement.Application.Features.Catalog.CreateArticle;
using OrderManagement.Application.Features.Catalog.DeactivateArticle;
using OrderManagement.Application.Features.Catalog.DeleteArticle;
using OrderManagement.Application.Features.Catalog.GetArticleForEdit;
using OrderManagement.Application.Features.Catalog.SearchArticles;
using OrderManagement.Application.Features.Catalog.Shared;
using OrderManagement.Application.Features.Catalog.UpdateArticleStock;
using OrderManagement.Domain.Catalog.ValueObjects;

using Reqnroll;

using SharedKernel.Primitives;

namespace OrderManagement.AcceptanceTests.Steps
{
    [Binding]
    public sealed class ArticleManagementSteps(
        ICreateArticleUseCase createArticleUseCase,
        IUpdateArticleStockUseCase updateArticleStockUseCase,
        IDeleteArticleUseCase deleteArticleUseCase,
        IDeactivateArticleUseCase deactivateArticleUseCase,
        ISearchArticlesUseCase searchArticlesUseCase,
        IGetArticleForEditUseCase getArticleForEditUseCase,
        AcceptanceTestContext context)
    {
        private Result<CreateArticleResponse>? _lastCreateResult;
        private Result? _lastStockResult;
        private Result? _lastDeleteResult;
        private IReadOnlyList<ArticleListItemDto>? _lastSearchResult;

        [Given(@"article ""([^""]*)"" named ""([^""]*)"" already exists in group ""([^""]*)""")]
        public async Task GivenArticleAlreadyExistsInGroup(string articleNumber, string name, string groupName)
            => await CreateArticleAsync(articleNumber, name, groupName, 10.00m, 10);

        [Given(@"article ""([^""]*)"" named ""([^""]*)"" already exists in group ""([^""]*)"" with stock (\d+)")]
        public async Task GivenArticleAlreadyExistsInGroupWithStock(string articleNumber, string name, string groupName, int stock)
            => await CreateArticleAsync(articleNumber, name, groupName, 10.00m, stock);

        [Given(@"article ""([^""]*)"" named ""([^""]*)"" already exists in group ""([^""]*)"" priced at ([\d.]+) CHF")]
        public async Task GivenArticleAlreadyExistsInGroupPricedAt(string articleNumber, string name, string groupName, decimal price)
            => await CreateArticleAsync(articleNumber, name, groupName, price, 10);

        [When(@"I add article ""([^""]*)"" named ""([^""]*)"" to group ""([^""]*)"" priced at ([\d.]+) CHF with stock (\d+)")]
        public async Task WhenIAddArticleToGroup(string articleNumber, string name, string groupName, decimal price, int stock)
        {
            int groupId = context.ArticleGroupIdsByName[groupName];

            _lastCreateResult = await createArticleUseCase.ExecuteAsync(new CreateArticleCommand(
                articleNumber, name, price, "CHF", groupId, stock, 20, 7.7m, null));

            if (_lastCreateResult.Value.IsSuccess)
            {
                context.ArticleIdsByNumber[articleNumber] = _lastCreateResult.Value.Value!.ArticleId;
            }
        }

        [When(@"I adjust stock for article ""([^""]*)"" by (-?\d+)")]
        public async Task WhenIAdjustStockForArticleBy(string articleNumber, int delta)
        {
            int articleId = context.ArticleIdsByNumber[articleNumber];
            _lastStockResult = await updateArticleStockUseCase.ExecuteAsync(new UpdateArticleStockCommand(articleId, delta));
        }

        [When(@"I delete article ""([^""]*)""")]
        public async Task WhenIDeleteArticle(string articleNumber)
        {
            int articleId = context.ArticleIdsByNumber[articleNumber];
            _lastDeleteResult = await deleteArticleUseCase.ExecuteAsync(new DeleteArticleCommand(articleId));
        }

        [When(@"I deactivate article ""([^""]*)""")]
        public async Task WhenIDeactivateArticle(string articleNumber)
        {
            int articleId = context.ArticleIdsByNumber[articleNumber];
            Result result = await deactivateArticleUseCase.ExecuteAsync(new DeactivateArticleCommand(articleId));
            Assert.IsTrue(result.IsSuccess, result.Error);
        }

        [Then(@"article ""([^""]*)"" exists in group ""([^""]*)"" with stock (\d+)")]
        public async Task ThenArticleExistsInGroupWithStock(string articleNumber, string groupName, int expectedStock)
        {
            GetArticleForEditResponse article = await GetArticleAsync(articleNumber);
            Assert.AreEqual(context.ArticleGroupIdsByName[groupName], article.GroupId);
            Assert.AreEqual(expectedStock, article.Stock);
        }

        [When(@"I filter articles by category ""([^""]*)""")]
        public async Task WhenIFilterArticlesByCategory(string groupName)
        {
            int groupId = context.ArticleGroupIdsByName[groupName];
            Result<IReadOnlyList<ArticleListItemDto>> result = await searchArticlesUseCase.ExecuteAsync(
                new SearchArticlesQuery(null, groupId));

            Assert.IsTrue(result.IsSuccess, result.Error);
            _lastSearchResult = result.Value;
        }

        [Then(@"the filtered article list contains ""([^""]*)"" and ""([^""]*)""")]
        public void ThenTheFilteredArticleListContainsAnd(string firstArticleNumber, string secondArticleNumber)
        {
            Assert.IsTrue(_lastSearchResult!.Any(a => a.ArticleNumber == firstArticleNumber));
            Assert.IsTrue(_lastSearchResult!.Any(a => a.ArticleNumber == secondArticleNumber));
        }

        [Then(@"the article registration is rejected because the article number already exists")]
        public void ThenTheArticleRegistrationIsRejectedBecauseTheArticleNumberAlreadyExists()
        {
            Assert.IsFalse(_lastCreateResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCreateResult.Value.Error, "already exists");
        }

        [Then(@"article ""([^""]*)"" has stock (\d+)")]
        public async Task ThenArticleHasStock(string articleNumber, int expectedStock)
        {
            GetArticleForEditResponse article = await GetArticleAsync(articleNumber);
            Assert.AreEqual(expectedStock, article.Stock);
        }

        [Then(@"the stock adjustment is rejected because it would go below zero")]
        public void ThenTheStockAdjustmentIsRejectedBecauseItWouldGoBelowZero() => Assert.IsFalse(_lastStockResult!.Value.IsSuccess);

        [Then(@"article ""([^""]*)"" still has stock (\d+)")]
        public async Task ThenArticleStillHasStock(string articleNumber, int expectedStock)
            => await ThenArticleHasStock(articleNumber, expectedStock);

        [Then(@"the article deletion is rejected because it is referenced by an order")]
        public void ThenTheArticleDeletionIsRejectedBecauseItIsReferencedByAnOrder()
        {
            Assert.IsFalse(_lastDeleteResult!.Value.IsSuccess);
            Assert.AreEqual(DeleteArticleErrorCodes.ArticleInUse, _lastDeleteResult.Value.Error);
        }

        [Then(@"article ""([^""]*)"" is inactive")]
        public async Task ThenArticleIsInactive(string articleNumber)
        {
            GetArticleForEditResponse article = await GetArticleAsync(articleNumber);
            Assert.AreEqual(ArticleStatus.Inactive, article.Status);
        }

        [Then(@"article ""([^""]*)"" is excluded from the active article catalogue")]
        public async Task ThenArticleIsExcludedFromTheActiveArticleCatalogue(string articleNumber)
        {
            Result<IReadOnlyList<ArticleListItemDto>> result = await searchArticlesUseCase.ExecuteAsync(
                new SearchArticlesQuery(null, null, ArticleStatus.Active));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.Any(a => a.ArticleNumber == articleNumber));
        }

        [Then(@"article ""([^""]*)"" can no longer be found")]
        public async Task ThenArticleCanNoLongerBeFound(string articleNumber)
        {
            int articleId = context.ArticleIdsByNumber[articleNumber];
            Result<GetArticleForEditResponse> result = await getArticleForEditUseCase.ExecuteAsync(new GetArticleForEditQuery(articleId));
            Assert.IsFalse(result.IsSuccess);
        }

        private async Task CreateArticleAsync(string articleNumber, string name, string groupName, decimal price, int stock)
        {
            int groupId = context.ArticleGroupIdsByName[groupName];

            Result<CreateArticleResponse> result = await createArticleUseCase.ExecuteAsync(
                new CreateArticleCommand(articleNumber, name, price, "CHF", groupId, stock, 20, 7.7m, null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            context.ArticleIdsByNumber[articleNumber] = result.Value!.ArticleId;
        }

        private async Task<GetArticleForEditResponse> GetArticleAsync(string articleNumber)
        {
            int articleId = context.ArticleIdsByNumber[articleNumber];
            Result<GetArticleForEditResponse> result = await getArticleForEditUseCase.ExecuteAsync(new GetArticleForEditQuery(articleId));

            Assert.IsTrue(result.IsSuccess, result.Error);
            return result.Value!;
        }
    }
}
