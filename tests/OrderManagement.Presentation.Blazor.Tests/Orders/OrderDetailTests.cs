using AngleSharp.Dom;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Application.Features.Catalog.SearchArticles;
using OrderManagement.Application.Features.Orders.AddOrderLine;
using OrderManagement.Application.Features.Orders.CancelOrder;
using OrderManagement.Application.Features.Orders.CompleteOrder;
using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Application.Features.Orders.DeleteOrder;
using OrderManagement.Application.Features.Orders.GetOrderDetails;
using OrderManagement.Application.Features.Orders.RemoveOrderLine;
using OrderManagement.Application.Features.Orders.StartOrderProcessing;
using OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

using OrderDetailPage = OrderManagement.Presentation.Blazor.Components.Pages.Orders.OrderDetail;

namespace OrderManagement.Presentation.Blazor.Tests.Orders
{
    [TestClass]
    public sealed class OrderDetailTests : BunitContext
    {
        public OrderDetailTests() => JSInterop.Mode = JSRuntimeMode.Loose;

        [TestMethod]
        public void DeleteOrderButton_WhenClicked_ShowsSwissGermanConfirmationWithOrderNumber()
        {
            var deleteUseCase = new FakeDeleteOrderUseCase(Result.Success());
            IRenderedComponent<OrderDetailPage> cut = RenderPage(deleteUseCase: deleteUseCase);

            FindButtonByText(cut, "Auftrag löschen").Click();

            string dialogText = cut.Find(".modal, [role='dialog']").TextContent;
            StringAssert.Contains(dialogText, "«ORD-2026-001»");
            StringAssert.Contains(dialogText, "endgültig gelöscht werden");
            StringAssert.Contains(dialogText, "Lagerbestand wieder gutgeschrieben");
            StringAssert.Contains(dialogText, "kann nicht rückgängig gemacht werden");
        }

        [TestMethod]
        public void ConfirmDeleteOrder_WhenUseCaseSucceeds_NavigatesToOrdersList()
        {
            var deleteUseCase = new FakeDeleteOrderUseCase(Result.Success());
            IRenderedComponent<OrderDetailPage> cut = RenderPage(deleteUseCase: deleteUseCase);

            FindButtonByText(cut, "Auftrag löschen").Click();
            FindButtonByText(cut, "Löschen").Click();

            Assert.AreEqual(1, deleteUseCase.CallCount);
            var navigationManager = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
            StringAssert.EndsWith(navigationManager.Uri.TrimEnd('/'), "auftraege");
        }

        [TestMethod]
        public void ConfirmDeleteOrder_WhenUseCaseFails_ShowsErrorAndStaysOnPage()
        {
            var deleteUseCase = new FakeDeleteOrderUseCase(Result.Fail("Fehler beim Löschen."));
            IRenderedComponent<OrderDetailPage> cut = RenderPage(deleteUseCase: deleteUseCase);

            FindButtonByText(cut, "Auftrag löschen").Click();
            FindButtonByText(cut, "Löschen").Click();

            StringAssert.Contains(cut.Markup, "Fehler beim Löschen.");
            var navigationManager = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
            Assert.IsFalse(navigationManager.Uri.TrimEnd('/').EndsWith("auftraege", StringComparison.Ordinal));
        }

        [TestMethod]
        public void OpenOrder_ShowsStartProcessingAndCancelButtons_ButNotComplete()
        {
            IRenderedComponent<OrderDetailPage> cut = RenderPage(status: OrderStatus.Open);

            IElement[] headerButtons = [.. cut.FindAll(".page-header-actions button")];

            Assert.IsTrue(headerButtons.Any(b => b.TextContent.Trim() == "Bearbeitung starten"));
            Assert.IsTrue(headerButtons.Any(b => b.TextContent.Trim() == "Auftrag stornieren"));
            Assert.IsFalse(headerButtons.Any(b => b.TextContent.Trim() == "Auftrag abschliessen"));
        }

        [TestMethod]
        public void InProgressOrder_ShowsCompleteAndCancelButtons_ButNotStartProcessing()
        {
            IRenderedComponent<OrderDetailPage> cut = RenderPage(status: OrderStatus.InProgress);

            IElement[] headerButtons = [.. cut.FindAll(".page-header-actions button")];

            Assert.IsTrue(headerButtons.Any(b => b.TextContent.Trim() == "Auftrag abschliessen"));
            Assert.IsTrue(headerButtons.Any(b => b.TextContent.Trim() == "Auftrag stornieren"));
            Assert.IsFalse(headerButtons.Any(b => b.TextContent.Trim() == "Bearbeitung starten"));
        }

        [TestMethod]
        public void CompletedOrder_ShowsNoStatusActionButtons()
        {
            IRenderedComponent<OrderDetailPage> cut = RenderPage(status: OrderStatus.Completed);

            IElement[] headerButtons = [.. cut.FindAll(".page-header-actions button")];

            Assert.IsFalse(headerButtons.Any(b => b.TextContent.Trim() == "Bearbeitung starten"));
            Assert.IsFalse(headerButtons.Any(b => b.TextContent.Trim() == "Auftrag abschliessen"));
            Assert.IsFalse(headerButtons.Any(b => b.TextContent.Trim() == "Auftrag stornieren"));
        }

        [TestMethod]
        public void CancelledOrder_ShowsNoStatusActionButtons()
        {
            IRenderedComponent<OrderDetailPage> cut = RenderPage(status: OrderStatus.Cancelled);

            IElement[] headerButtons = [.. cut.FindAll(".page-header-actions button")];

            Assert.IsFalse(headerButtons.Any(b => b.TextContent.Trim() == "Bearbeitung starten"));
            Assert.IsFalse(headerButtons.Any(b => b.TextContent.Trim() == "Auftrag abschliessen"));
            Assert.IsFalse(headerButtons.Any(b => b.TextContent.Trim() == "Auftrag stornieren"));
        }

        [TestMethod]
        public void StartProcessingButton_WhenClicked_CallsUseCaseAndReloads()
        {
            var startUseCase = new FakeStartOrderProcessingUseCase(Result.Success());
            IRenderedComponent<OrderDetailPage> cut = RenderPage(status: OrderStatus.Open, startUseCase: startUseCase);

            FindButtonByText(cut, "Bearbeitung starten").Click();

            Assert.AreEqual(1, startUseCase.CallCount);
        }

        [TestMethod]
        public void CompleteButton_WhenClicked_CallsUseCase()
        {
            var completeUseCase = new FakeCompleteOrderUseCase(Result.Success());
            IRenderedComponent<OrderDetailPage> cut = RenderPage(status: OrderStatus.InProgress, completeUseCase: completeUseCase);

            FindButtonByText(cut, "Auftrag abschliessen").Click();

            Assert.AreEqual(1, completeUseCase.CallCount);
        }

        [TestMethod]
        public void CancelButton_WhenClicked_ShowsConfirmationExplainingStockReversal()
        {
            IRenderedComponent<OrderDetailPage> cut = RenderPage(status: OrderStatus.Open);

            FindButtonByText(cut, "Auftrag stornieren").Click();

            string dialogText = cut.Find(".modal, [role='dialog']").TextContent;
            StringAssert.Contains(dialogText, "«ORD-2026-001»");
            StringAssert.Contains(dialogText, "Lagerbestand");
        }

        [TestMethod]
        public void ConfirmCancel_WhenUseCaseSucceeds_ShowsSuccessMessage()
        {
            var cancelUseCase = new FakeCancelOrderUseCase(Result.Success());
            IRenderedComponent<OrderDetailPage> cut = RenderPage(status: OrderStatus.Open, cancelUseCase: cancelUseCase);

            FindButtonByText(cut, "Auftrag stornieren").Click();
            FindButtonByText(cut, "Stornieren").Click();

            Assert.AreEqual(1, cancelUseCase.CallCount);
            StringAssert.Contains(cut.Markup, "storniert");
        }

        private static IElement FindButtonByText(IRenderedComponent<OrderDetailPage> cut, string text) =>
            cut.FindAll("button").Single(b => b.TextContent.Trim() == text);

        private static GetOrderDetailsResponse SampleOrder(OrderStatus status) => new(
            OrderId: 1,
            OrderNumber: "ORD-2026-001",
            OrderDate: new DateTime(2026, 9, 1),
            DeliveryDate: new DateOnly(2026, 9, 5),
            CustomerReference: null,
            CustomerId: 1,
            CustomerNumber: "K-001",
            CustomerName: "Muster AG",
            BillingStreet: "Bahnhofstrasse",
            BillingHouseNumber: "1",
            BillingPostalCode: "8000",
            BillingCity: "Zürich",
            BillingCountryCode: "CH",
            BillingAddressSource: AddressSource.Automatic,
            DeliveryStreet: "Bahnhofstrasse",
            DeliveryHouseNumber: "1",
            DeliveryPostalCode: "8000",
            DeliveryCity: "Zürich",
            DeliveryCountryCode: "CH",
            DeliveryAddressSource: AddressSource.Automatic,
            TotalAmount: 10m,
            TotalCurrency: "CHF",
            Status: status,
            IsOverdue: false,
            Lines: [new OrderLineDto(1, 1, 1, "Widget", 10m, "CHF", 1, 10m, "CHF")]);

        private IRenderedComponent<OrderDetailPage> RenderPage(
            FakeDeleteOrderUseCase? deleteUseCase = null,
            FakeStartOrderProcessingUseCase? startUseCase = null,
            FakeCompleteOrderUseCase? completeUseCase = null,
            FakeCancelOrderUseCase? cancelUseCase = null,
            OrderStatus status = OrderStatus.Open)
        {
            _ = Services.AddSingleton<IGetOrderDetailsUseCase>(new FakeGetOrderDetailsUseCase(SampleOrder(status)));
            _ = Services.AddSingleton<IUpdateOrderLineQuantityUseCase>(new FakeUpdateOrderLineQuantityUseCase());
            _ = Services.AddSingleton<IRemoveOrderLineUseCase>(new FakeRemoveOrderLineUseCase());
            _ = Services.AddSingleton<IAddOrderLineUseCase>(new FakeAddOrderLineUseCase());
            _ = Services.AddSingleton<IDeleteOrderUseCase>(deleteUseCase ?? new FakeDeleteOrderUseCase(Result.Success()));
            _ = Services.AddSingleton<IStartOrderProcessingUseCase>(startUseCase ?? new FakeStartOrderProcessingUseCase(Result.Success()));
            _ = Services.AddSingleton<ICompleteOrderUseCase>(completeUseCase ?? new FakeCompleteOrderUseCase(Result.Success()));
            _ = Services.AddSingleton<ICancelOrderUseCase>(cancelUseCase ?? new FakeCancelOrderUseCase(Result.Success()));
            _ = Services.AddSingleton<ISearchArticlesUseCase>(new FakeSearchArticlesUseCase());

            return Render<OrderDetailPage>(parameters => parameters.Add(p => p.OrderId, 1));
        }

        private sealed class FakeGetOrderDetailsUseCase(GetOrderDetailsResponse response) : IGetOrderDetailsUseCase
        {
            public Task<Result<GetOrderDetailsResponse>> ExecuteAsync(
                GetOrderDetailsQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Success(response));
        }

        private sealed class FakeUpdateOrderLineQuantityUseCase : IUpdateOrderLineQuantityUseCase
        {
            public Task<Result> ExecuteAsync(
                UpdateOrderLineQuantityCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeRemoveOrderLineUseCase : IRemoveOrderLineUseCase
        {
            public Task<Result> ExecuteAsync(
                RemoveOrderLineCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeAddOrderLineUseCase : IAddOrderLineUseCase
        {
            public Task<Result> ExecuteAsync(
                AddOrderLineCommand command, CancellationToken cancellationToken = default)
                => Task.FromResult(Result.Fail("not used"));
        }

        private sealed class FakeDeleteOrderUseCase(Result result) : IDeleteOrderUseCase
        {
            public int CallCount { get; private set; }

            public Task<Result> ExecuteAsync(
                DeleteOrderCommand command, CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(result);
            }
        }

        private sealed class FakeStartOrderProcessingUseCase(Result result) : IStartOrderProcessingUseCase
        {
            public int CallCount { get; private set; }

            public Task<Result> ExecuteAsync(
                StartOrderProcessingCommand command, CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(result);
            }
        }

        private sealed class FakeCompleteOrderUseCase(Result result) : ICompleteOrderUseCase
        {
            public int CallCount { get; private set; }

            public Task<Result> ExecuteAsync(
                CompleteOrderCommand command, CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(result);
            }
        }

        private sealed class FakeCancelOrderUseCase(Result result) : ICancelOrderUseCase
        {
            public int CallCount { get; private set; }

            public Task<Result> ExecuteAsync(
                CancelOrderCommand command, CancellationToken cancellationToken = default)
            {
                CallCount++;
                return Task.FromResult(result);
            }
        }

        private sealed class FakeSearchArticlesUseCase : ISearchArticlesUseCase
        {
            public Task<Result<IReadOnlyList<ArticleListItemDto>>> ExecuteAsync(
                SearchArticlesQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Success<IReadOnlyList<ArticleListItemDto>>([]));
        }
    }
}
