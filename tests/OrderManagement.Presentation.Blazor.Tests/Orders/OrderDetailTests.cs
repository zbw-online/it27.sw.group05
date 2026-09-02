using AngleSharp.Dom;

using Bunit;
using Bunit.TestDoubles;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Catalog.SearchArticles;
using OrderManagement.Application.Features.Catalog.Shared;
using OrderManagement.Application.Features.Orders.AddOrderLine;
using OrderManagement.Application.Features.Orders.DeleteOrder;
using OrderManagement.Application.Features.Orders.GetOrderDetails;
using OrderManagement.Application.Features.Orders.RemoveOrderLine;
using OrderManagement.Application.Features.Orders.Shared;
using OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

using OrderDetailPage = OrderManagement.Presentation.Blazor.Components.Pages.Orders.OrderDetail;

namespace OrderManagement.Presentation.Blazor.Tests.Orders
{
    [TestClass]
    public sealed class OrderDetailTests : Bunit.TestContext
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
            var navigationManager = (FakeNavigationManager)Services.GetRequiredService<NavigationManager>();
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
            var navigationManager = (FakeNavigationManager)Services.GetRequiredService<NavigationManager>();
            Assert.IsFalse(navigationManager.Uri.TrimEnd('/').EndsWith("auftraege", StringComparison.Ordinal));
        }

        private static IElement FindButtonByText(IRenderedComponent<OrderDetailPage> cut, string text) =>
            cut.FindAll("button").Single(b => b.TextContent.Trim() == text);

        private static GetOrderDetailsResponse SampleOrder() => new(
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
            Lines: [new OrderLineDto(1, 1, 1, "Widget", 10m, "CHF", 1, 10m, "CHF")]);

        private IRenderedComponent<OrderDetailPage> RenderPage(FakeDeleteOrderUseCase? deleteUseCase = null)
        {
            _ = Services.AddSingleton<IGetOrderDetailsUseCase>(new FakeGetOrderDetailsUseCase(SampleOrder()));
            _ = Services.AddSingleton<IUpdateOrderLineQuantityUseCase>(new FakeUpdateOrderLineQuantityUseCase());
            _ = Services.AddSingleton<IRemoveOrderLineUseCase>(new FakeRemoveOrderLineUseCase());
            _ = Services.AddSingleton<IAddOrderLineUseCase>(new FakeAddOrderLineUseCase());
            _ = Services.AddSingleton<IDeleteOrderUseCase>(deleteUseCase ?? new FakeDeleteOrderUseCase(Result.Success()));
            _ = Services.AddSingleton<ISearchArticlesUseCase>(new FakeSearchArticlesUseCase());

            return RenderComponent<OrderDetailPage>(parameters => parameters.Add(p => p.OrderId, 1));
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

        private sealed class FakeSearchArticlesUseCase : ISearchArticlesUseCase
        {
            public Task<Result<IReadOnlyList<ArticleListItemDto>>> ExecuteAsync(
                SearchArticlesQuery query, CancellationToken cancellationToken = default)
                => Task.FromResult(Results.Success<IReadOnlyList<ArticleListItemDto>>([]));
        }
    }
}
