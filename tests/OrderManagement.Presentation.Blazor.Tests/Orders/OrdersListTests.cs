using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Application.Features.Orders.DeleteOrder;
using OrderManagement.Application.Features.Orders.SearchActiveOrders;
using OrderManagement.Application.Features.Orders.SearchArchivedOrders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

using OrdersListPage = OrderManagement.Presentation.Blazor.Components.Pages.Orders.OrdersList;

namespace OrderManagement.Presentation.Blazor.Tests.Orders
{
    [TestClass]
    public sealed class OrdersListTests : BunitContext
    {
        public OrdersListTests() => JSInterop.Mode = JSRuntimeMode.Loose;

        private static OrderSearchItemDto SampleItem(string orderNumber, OrderStatus status = OrderStatus.Open, bool isOverdue = false)
            => new(1, orderNumber, new DateTime(2026, 9, 1), new DateOnly(2026, 9, 10), 1, "CU00001", 1, 100m, "CHF", status, isOverdue);

        private static OrderSearchResultDto ResultOf(params OrderSearchItemDto[] items)
            => new(items, items.Length, 1, 15, items.Length == 0 ? 0 : 1);

        [TestMethod]
        public void Render_OnFirstRender_OnlyQueriesActiveOrders()
        {
            var active = new FakeSearchActiveOrdersUseCase { ResultToReturn = Results.Success(ResultOf(SampleItem("ORD-2026-001"))) };
            var archive = new FakeSearchArchivedOrdersUseCase();

            _ = RenderPage(active, archive);

            Assert.AreEqual(1, active.CallCount);
            Assert.AreEqual(0, archive.CallCount);
        }

        [TestMethod]
        public void ClickArchiveToggle_QueriesArchiveExactlyOnce()
        {
            var active = new FakeSearchActiveOrdersUseCase();
            var archive = new FakeSearchArchivedOrdersUseCase { ResultToReturn = Results.Success(ResultOf(SampleItem("ORD-2025-900", OrderStatus.Completed))) };

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);
            ArchiveToggleButton(cut).Click();

            Assert.AreEqual(1, archive.CallCount);
        }

        [TestMethod]
        public void ReopeningArchive_WithoutChangedFilters_DoesNotRefetch()
        {
            var active = new FakeSearchActiveOrdersUseCase();
            var archive = new FakeSearchArchivedOrdersUseCase { ResultToReturn = Results.Success(ResultOf(SampleItem("ORD-2025-900", OrderStatus.Completed))) };

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);
            ArchiveToggleButton(cut).Click();
            ArchiveToggleButton(cut).Click();
            ArchiveToggleButton(cut).Click();

            Assert.AreEqual(1, archive.CallCount, "Reopening the archive without changed filters must not issue another request.");
        }

        [TestMethod]
        public void LoadingState_IsVisibleWhileArchiveRequestIsInFlight()
        {
            var active = new FakeSearchActiveOrdersUseCase();
            var archive = new FakeSearchArchivedOrdersUseCase();
            var pending = new TaskCompletionSource<Result<OrderSearchResultDto>>();
            archive.PendingCompletion = pending;

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);

            _ = cut.InvokeAsync(() => ArchiveToggleButton(cut).Click());
            cut.WaitForState(() => cut.FindAll(".feedback-state-loading").Count > 0, TimeSpan.FromSeconds(2));

            Assert.IsTrue(cut.FindAll(".feedback-state-loading").Count > 0);

            pending.SetResult(Results.Success(ResultOf(SampleItem("ORD-2025-900", OrderStatus.Completed))));
            cut.WaitForState(() => cut.FindAll(".feedback-state-loading").Count == 0, TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        public void ArchiveData_AppearsAfterSuccessfulLoad()
        {
            var active = new FakeSearchActiveOrdersUseCase();
            var archive = new FakeSearchArchivedOrdersUseCase { ResultToReturn = Results.Success(ResultOf(SampleItem("ORD-2025-900", OrderStatus.Completed))) };

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);
            ArchiveToggleButton(cut).Click();

            StringAssert.Contains(cut.Markup, "ORD-2025-900");
        }

        [TestMethod]
        public void ArchiveEmptyResult_ShowsEmptyState()
        {
            var active = new FakeSearchActiveOrdersUseCase();
            var archive = new FakeSearchArchivedOrdersUseCase { ResultToReturn = Results.Success(ResultOf()) };

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);
            ArchiveToggleButton(cut).Click();

            Assert.IsTrue(cut.FindAll(".feedback-state-empty").Count > 0);
        }

        [TestMethod]
        public void ArchiveError_DoesNotAffectActiveList()
        {
            var active = new FakeSearchActiveOrdersUseCase { ResultToReturn = Results.Success(ResultOf(SampleItem("ORD-2026-001"))) };
            var archive = new FakeSearchArchivedOrdersUseCase { ResultToReturn = Results.Fail<OrderSearchResultDto>("Archiv-Fehler.") };

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);
            ArchiveToggleButton(cut).Click();

            Assert.IsTrue(cut.FindAll(".feedback-state-error").Count > 0);
            StringAssert.Contains(cut.Markup, "ORD-2026-001");
        }

        [TestMethod]
        public async Task DoubleClickArchiveToggle_WhileFirstRequestInFlight_DoesNotStartParallelRequest()
        {
            var active = new FakeSearchActiveOrdersUseCase();
            var archive = new FakeSearchArchivedOrdersUseCase();
            var pending = new TaskCompletionSource<Result<OrderSearchResultDto>>();
            archive.PendingCompletion = pending;

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);

            _ = cut.InvokeAsync(() => ArchiveToggleButton(cut).Click());
            cut.WaitForState(() => archive.CallCount > 0, TimeSpan.FromSeconds(2));
            await cut.InvokeAsync(() => ArchiveToggleButton(cut).Click());

            Assert.AreEqual(1, archive.CallCount, "A second click while the first request is still in flight must not start a parallel request.");

            pending.SetResult(Results.Success(ResultOf()));
        }

        [TestMethod]
        public void ActivePageNavigation_TriggersServerSideQueryForThatPage()
        {
            var active = new FakeSearchActiveOrdersUseCase
            {
                ResultToReturn = Results.Success(new OrderSearchResultDto([SampleItem("ORD-2026-001")], 30, 1, 15, 2)),
            };
            var archive = new FakeSearchArchivedOrdersUseCase();

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);

            IElement pageTwoButton = cut.FindAll(".pagination-page").Single(b => b.TextContent.Trim() == "2");
            pageTwoButton.Click();

            Assert.AreEqual(2, active.CallCount);
            Assert.AreEqual(2, active.LastQuery!.Page);
        }

        [TestMethod]
        public void ActiveSearchChange_ResetsPageToOne()
        {
            var active = new FakeSearchActiveOrdersUseCase
            {
                ResultToReturn = Results.Success(new OrderSearchResultDto([SampleItem("ORD-2026-001")], 30, 1, 15, 2)),
            };
            var archive = new FakeSearchArchivedOrdersUseCase();

            IRenderedComponent<OrdersListPage> cut = RenderPage(active, archive);

            cut.FindAll(".pagination-page").Single(b => b.TextContent.Trim() == "2").Click();
            Assert.AreEqual(2, active.LastQuery!.Page);

            IElement searchInput = cut.FindAll(".search-field-input")[0];
            searchInput.Input("ORD-2026-005");
            searchInput.KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.AreEqual(1, active.LastQuery!.Page);
            Assert.AreEqual("ORD-2026-005", active.LastQuery.SearchTerm);
        }

        private static IElement ArchiveToggleButton(IRenderedComponent<OrdersListPage> cut) =>
            cut.FindAll("button").Single(b => b.TextContent.Contains("Archivierte Aufträge", StringComparison.Ordinal));

        private IRenderedComponent<OrdersListPage> RenderPage(
            FakeSearchActiveOrdersUseCase activeUseCase,
            FakeSearchArchivedOrdersUseCase archiveUseCase,
            FakeDeleteOrderUseCase? deleteUseCase = null)
        {
            _ = Services.AddSingleton<ISearchActiveOrdersUseCase>(activeUseCase);
            _ = Services.AddSingleton<ISearchArchivedOrdersUseCase>(archiveUseCase);
            _ = Services.AddSingleton<IDeleteOrderUseCase>(deleteUseCase ?? new FakeDeleteOrderUseCase());

            return Render<OrdersListPage>();
        }

        private sealed class FakeSearchActiveOrdersUseCase : ISearchActiveOrdersUseCase
        {
            public int CallCount { get; private set; }
            public SearchActiveOrdersQuery? LastQuery { get; private set; }
            public Result<OrderSearchResultDto> ResultToReturn { get; set; } = Results.Success(new OrderSearchResultDto([], 0, 1, 15, 0));

            public Task<Result<OrderSearchResultDto>> ExecuteAsync(
                SearchActiveOrdersQuery query, CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastQuery = query;
                return Task.FromResult(ResultToReturn);
            }
        }

        private sealed class FakeSearchArchivedOrdersUseCase : ISearchArchivedOrdersUseCase
        {
            public int CallCount { get; private set; }
            public SearchArchivedOrdersQuery? LastQuery { get; private set; }
            public Result<OrderSearchResultDto> ResultToReturn { get; set; } = Results.Success(new OrderSearchResultDto([], 0, 1, 15, 0));
            public TaskCompletionSource<Result<OrderSearchResultDto>>? PendingCompletion { get; set; }

            public Task<Result<OrderSearchResultDto>> ExecuteAsync(
                SearchArchivedOrdersQuery query, CancellationToken cancellationToken = default)
            {
                CallCount++;
                LastQuery = query;
                return PendingCompletion?.Task ?? Task.FromResult(ResultToReturn);
            }
        }

        private sealed class FakeDeleteOrderUseCase : IDeleteOrderUseCase
        {
            public Task<Result> ExecuteAsync(DeleteOrderCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Success());
        }
    }
}
