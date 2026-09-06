using AngleSharp.Dom;
using AngleSharp.Html.Dom;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Application.Features.Catalog.CreateArticle;
using OrderManagement.Application.Features.Catalog.CreateArticleGroup;
using OrderManagement.Application.Features.Catalog.DeactivateArticle;
using OrderManagement.Application.Features.Catalog.DeleteArticle;
using OrderManagement.Application.Features.Catalog.DeleteArticleGroup;
using OrderManagement.Application.Features.Catalog.GetArticleForEdit;
using OrderManagement.Application.Features.Catalog.GetArticleGroupForEdit;
using OrderManagement.Application.Features.Catalog.GetArticleGroupHierarchy;
using OrderManagement.Application.Features.Catalog.ReactivateArticle;
using OrderManagement.Application.Features.Catalog.RenameArticleGroup;
using OrderManagement.Application.Features.Catalog.SearchArticleGroups;
using OrderManagement.Application.Features.Catalog.SearchArticles;
using OrderManagement.Application.Features.Catalog.UpdateArticle;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

using ArticlesPage = OrderManagement.Presentation.Blazor.Components.Pages.Articles.Articles;

namespace OrderManagement.Presentation.Blazor.Tests.Articles
{
    [TestClass]
    public sealed class ArticlesTests : BunitContext
    {
        public ArticlesTests() => JSInterop.Mode = JSRuntimeMode.Loose;

        [TestMethod]
        public void SearchResults_ShowStockLevelBadgeMatchingEachArticlesDerivedClassification()
        {
            ArticleListItemDto[] articles =
            [
                Article(1, "ART-001", stock: 50, reorderPoint: 20),
                Article(2, "ART-002", stock: 10, reorderPoint: 20),
                Article(3, "ART-003", stock: 0, reorderPoint: 20)
            ];

            IRenderedComponent<ArticlesPage> cut = RenderPage(articles);

            IElement[] rows = [.. cut.FindAll("tbody tr")];
            Assert.AreEqual(3, rows.Length);

            StringAssert.Contains(rows[0].TextContent, "Verfügbar");
            StringAssert.Contains(rows[1].TextContent, "Tiefer Bestand");
            StringAssert.Contains(rows[2].TextContent, "Nicht an Lager");
        }

        [TestMethod]
        public void SearchResults_TwoArticlesWithSameStockButDifferentReorderPoints_ShowDifferentBadges()
        {
            ArticleListItemDto[] articles =
            [
                Article(1, "ART-001", stock: 10, reorderPoint: 5),
                Article(2, "ART-002", stock: 10, reorderPoint: 15)
            ];

            IRenderedComponent<ArticlesPage> cut = RenderPage(articles);

            IElement[] rows = [.. cut.FindAll("tbody tr")];
            StringAssert.Contains(rows[0].TextContent, "Verfügbar");
            StringAssert.Contains(rows[1].TextContent, "Tiefer Bestand");
        }

        [TestMethod]
        public void StockFilter_NichtAmLager_ShowsOnlyOutOfStockArticles()
        {
            ArticleListItemDto[] articles =
            [
                Article(1, "ART-001", stock: 50, reorderPoint: 20),
                Article(2, "ART-002", stock: 0, reorderPoint: 20)
            ];

            IRenderedComponent<ArticlesPage> cut = RenderPage(articles);

            cut.FindAll("select")[0].Change("nichtAmLager");

            IElement[] rows = [.. cut.FindAll("tbody tr")];
            Assert.AreEqual(1, rows.Length);
            StringAssert.Contains(rows[0].TextContent, "ART-002");
        }

        [TestMethod]
        public void StockFilter_Tief_ShowsOnlyLowStockArticles()
        {
            ArticleListItemDto[] articles =
            [
                Article(1, "ART-001", stock: 50, reorderPoint: 20),
                Article(2, "ART-002", stock: 10, reorderPoint: 20),
                Article(3, "ART-003", stock: 0, reorderPoint: 20)
            ];

            IRenderedComponent<ArticlesPage> cut = RenderPage(articles);

            cut.FindAll("select")[0].Change("tief");

            IElement[] rows = [.. cut.FindAll("tbody tr")];
            Assert.AreEqual(1, rows.Length);
            StringAssert.Contains(rows[0].TextContent, "ART-002");
        }

        [TestMethod]
        public void CreateArticleForm_WithNegativeReorderPoint_ShowsSwissGermanValidationMessageAndDoesNotSubmit()
        {
            var createUseCase = new FakeCreateArticleUseCase();
            IRenderedComponent<ArticlesPage> cut = RenderPage([], createUseCase);

            FindButtonByText(cut, "Neuer Artikel").Click();
            cut.Find("#art-number").Change("ART-100");
            cut.Find("#art-name").Change("Widget");
            cut.Find("#art-reorder-point").Change("-1");

            cut.FindAll("form")[0].Submit();

            StringAssert.Contains(cut.Markup, "Der Meldebestand darf nicht negativ sein.");
            Assert.AreEqual(0, createUseCase.CallCount);
        }

        [TestMethod]
        public void EditArticleForm_PrefillsReorderPointFromExistingArticle()
        {
            ArticleListItemDto[] articles = [Article(1, "ART-001", stock: 10, reorderPoint: 7)];
            var editResponse = new GetArticleForEditResponse(1, "ART-001", "Widget", 9.99m, "CHF", 1, 10, 7, 7.7m, null, ArticleStatus.Active);

            IRenderedComponent<ArticlesPage> cut = RenderPage(articles, getArticleForEditUseCase: new FakeGetArticleForEditUseCase(editResponse));

            cut.Find("tbody tr").Click();
            FindButtonByText(cut, "Bearbeiten").Click();

            var reorderPointInput = (IHtmlInputElement)cut.Find("#art-reorder-point");
            Assert.AreEqual("7", reorderPointInput.Value);
        }

        [TestMethod]
        public void CreateArticleForm_CategoryPicker_RequiresDeliberateApply_ThenShowsChosenPath()
        {
            IRenderedComponent<ArticlesPage> cut = RenderPage([]);

            FindButtonByText(cut, "Neuer Artikel").Click();

            StringAssert.Contains(cut.Markup, "Kategorie wählen");
            StringAssert.Contains(cut.Markup, "Bitte eine Kategorie wählen.");

            FindButtonByText(cut, "Kategorie wählen").Click();
            Assert.AreEqual(1, cut.FindAll(".category-tree-picker").Count);

            cut.Find(".category-tree-label").Click();
            FindButtonByText(cut, "Übernehmen").Click();

            Assert.AreEqual(0, cut.FindAll(".category-tree-picker").Count);
            StringAssert.Contains(cut.Markup, "Kategorie ändern");
            Assert.IsFalse(cut.Markup.Contains("Bitte eine Kategorie wählen.", StringComparison.Ordinal));
        }

        [TestMethod]
        public void Toolbar_HasArticlesToolbarClass_AndCategorySelectorPrecedesSearchFieldInDom()
        {
            IRenderedComponent<ArticlesPage> cut = RenderPage([]);

            IElement toolbar = cut.Find(".articles-toolbar");
            string toolbarHtml = toolbar.OuterHtml;

            int categoryIndex = toolbarHtml.IndexOf("category-flyout-trigger", StringComparison.Ordinal);
            int searchIndex = toolbarHtml.IndexOf("search-field-input", StringComparison.Ordinal);

            Assert.IsTrue(categoryIndex >= 0, "Expected the category selector inside the articles toolbar.");
            Assert.IsTrue(searchIndex >= 0, "Expected the search field inside the articles toolbar.");
            Assert.IsTrue(categoryIndex < searchIndex, "The category selector must appear before the search field in the DOM.");
        }

        private static IElement FindButtonByText(IRenderedComponent<ArticlesPage> cut, string text) =>
            cut.FindAll("button").Single(b => b.TextContent.Contains(text, StringComparison.Ordinal));

        private static ArticleListItemDto Article(int id, string number, int stock, int reorderPoint) =>
            new(id, number, $"Artikel {id}", 9.99m, "CHF", 1, "Gruppe", stock, reorderPoint, StockLevelFor(stock, reorderPoint), 7.7m, ArticleStatus.Active);

        private static StockLevel StockLevelFor(int stock, int reorderPoint) => stock == 0
            ? StockLevel.OutOfStock
            : stock <= reorderPoint
                ? StockLevel.Low
                : StockLevel.Available;

        private IRenderedComponent<ArticlesPage> RenderPage(
            ArticleListItemDto[] articles,
            ICreateArticleUseCase? createArticleUseCase = null,
            IGetArticleForEditUseCase? getArticleForEditUseCase = null)
        {
            _ = Services.AddSingleton<ISearchArticlesUseCase>(new FakeSearchArticlesUseCase(articles));
            _ = Services.AddSingleton(createArticleUseCase ?? new FakeCreateArticleUseCase());
            _ = Services.AddSingleton(getArticleForEditUseCase ?? new FakeGetArticleForEditUseCase(null));
            _ = Services.AddSingleton<IUpdateArticleUseCase>(new FakeUpdateArticleUseCase());
            _ = Services.AddSingleton<IDeleteArticleUseCase>(new FakeDeleteArticleUseCase());
            _ = Services.AddSingleton<IDeactivateArticleUseCase>(new FakeDeactivateArticleUseCase());
            _ = Services.AddSingleton<IReactivateArticleUseCase>(new FakeReactivateArticleUseCase());
            _ = Services.AddSingleton<IGetArticleGroupHierarchyUseCase>(new FakeGetArticleGroupHierarchyUseCase());
            _ = Services.AddSingleton<ISearchArticleGroupsUseCase>(new FakeSearchArticleGroupsUseCase());
            _ = Services.AddSingleton<ICreateArticleGroupUseCase>(new FakeCreateArticleGroupUseCase());
            _ = Services.AddSingleton<IGetArticleGroupForEditUseCase>(new FakeGetArticleGroupForEditUseCase());
            _ = Services.AddSingleton<IRenameArticleGroupUseCase>(new FakeRenameArticleGroupUseCase());
            _ = Services.AddSingleton<IDeleteArticleGroupUseCase>(new FakeDeleteArticleGroupUseCase());

            return Render<ArticlesPage>();
        }

        private sealed class FakeSearchArticlesUseCase(ArticleListItemDto[] articles) : ISearchArticlesUseCase
        {
            public Task<Result<IReadOnlyList<ArticleListItemDto>>> ExecuteAsync(
                SearchArticlesQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Success<IReadOnlyList<ArticleListItemDto>>(articles));
        }

        private sealed class FakeCreateArticleUseCase : ICreateArticleUseCase
        {
            public int CallCount { get; private set; }

            public Task<Result<CreateArticleResponse>> ExecuteAsync(
                CreateArticleCommand command, CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(Results.Fail<CreateArticleResponse>("not used"));
            }
        }

        private sealed class FakeGetArticleForEditUseCase(GetArticleForEditResponse? response) : IGetArticleForEditUseCase
        {
            public Task<Result<GetArticleForEditResponse>> ExecuteAsync(
                GetArticleForEditQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(response is null
                    ? Results.Fail<GetArticleForEditResponse>("not used")
                    : Results.Success(response));
        }

        private sealed class FakeUpdateArticleUseCase : IUpdateArticleUseCase
        {
            public Task<Result> ExecuteAsync(
                UpdateArticleCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeDeleteArticleUseCase : IDeleteArticleUseCase
        {
            public Task<Result> ExecuteAsync(
                DeleteArticleCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeDeactivateArticleUseCase : IDeactivateArticleUseCase
        {
            public Task<Result> ExecuteAsync(
                DeactivateArticleCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeReactivateArticleUseCase : IReactivateArticleUseCase
        {
            public Task<Result> ExecuteAsync(
                ReactivateArticleCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeGetArticleGroupHierarchyUseCase : IGetArticleGroupHierarchyUseCase
        {
            public Task<Result<IReadOnlyList<ArticleGroupHierarchyDto>>> ExecuteAsync(
                GetArticleGroupHierarchyQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Success<IReadOnlyList<ArticleGroupHierarchyDto>>(
                    [new ArticleGroupHierarchyDto(1, "Gruppe", null, 0, "Gruppe")]));
        }

        private sealed class FakeSearchArticleGroupsUseCase : ISearchArticleGroupsUseCase
        {
            public Task<Result<IReadOnlyList<ArticleGroupListItemDto>>> ExecuteAsync(
                SearchArticleGroupsQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Success<IReadOnlyList<ArticleGroupListItemDto>>([]));
        }

        private sealed class FakeCreateArticleGroupUseCase : ICreateArticleGroupUseCase
        {
            public Task<Result<CreateArticleGroupResponse>> ExecuteAsync(
                CreateArticleGroupCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Fail<CreateArticleGroupResponse>("not used"));
        }

        private sealed class FakeGetArticleGroupForEditUseCase : IGetArticleGroupForEditUseCase
        {
            public Task<Result<GetArticleGroupForEditResponse>> ExecuteAsync(
                GetArticleGroupForEditQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Fail<GetArticleGroupForEditResponse>("not used"));
        }

        private sealed class FakeRenameArticleGroupUseCase : IRenameArticleGroupUseCase
        {
            public Task<Result> ExecuteAsync(
                RenameArticleGroupCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeDeleteArticleGroupUseCase : IDeleteArticleGroupUseCase
        {
            public Task<Result> ExecuteAsync(
                DeleteArticleGroupCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }
    }
}
