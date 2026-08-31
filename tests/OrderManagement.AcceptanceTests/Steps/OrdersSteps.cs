using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.AcceptanceTests.Support;
using OrderManagement.Application.Features.Orders.CreateOrder;
using OrderManagement.Application.Features.Orders.DeleteOrder;
using OrderManagement.Application.Features.Orders.GetOrderDetails;
using OrderManagement.Application.Features.Orders.SearchOrders;
using OrderManagement.Application.Features.Orders.Shared;
using OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity;
using OrderManagement.Domain.Orders.ValueObjects;

using Reqnroll;

using SharedKernel.Primitives;

namespace OrderManagement.AcceptanceTests.Steps
{
    [Binding]
    public sealed class OrdersSteps(
        ICreateOrderUseCase createOrderUseCase,
        ISearchOrdersUseCase searchOrdersUseCase,
        IGetOrderDetailsUseCase getOrderDetailsUseCase,
        IUpdateOrderLineQuantityUseCase updateOrderLineQuantityUseCase,
        IDeleteOrderUseCase deleteOrderUseCase,
        AcceptanceTestContext context)
    {
        private static readonly DateOnly DefaultDeliveryDate = new(2026, 9, 1);
        private static readonly AddressOverrideInput DefaultAddress = new("Main Street", "1", "8000", "Zurich", "CH");

        private Result<CreateOrderResponse>? _lastCreateResult;
        private IReadOnlyList<OrderListItemDto>? _lastSearchResult;

        [Given(@"order ""([^""]*)"" already exists for customer ""([^""]*)"" with lines:")]
        public async Task GivenOrderAlreadyExistsForCustomerWithLines(string orderNumber, string customerNumber, Table linesTable)
            => await CreateOrderAsync(orderNumber, customerNumber, linesTable);

        [When(@"I create order ""([^""]*)"" for customer ""([^""]*)"" with lines:")]
        public async Task WhenICreateOrderForCustomerWithLines(string orderNumber, string customerNumber, Table linesTable)
            => await CreateOrderAsync(orderNumber, customerNumber, linesTable);

        [When(@"I create order ""([^""]*)"" for an unknown customer with lines:")]
        public async Task WhenICreateOrderForAnUnknownCustomerWithLines(string orderNumber, Table linesTable)
        {
            IReadOnlyList<CreateOrderLineInput> lines = ToLineInputs(linesTable);
            _lastCreateResult = await createOrderUseCase.ExecuteAsync(
                new CreateOrderCommand(orderNumber, 999_999, DefaultDeliveryDate, null, DefaultAddress, DefaultAddress, lines));
        }

        [When(@"I create order ""([^""]*)"" for customer ""([^""]*)"" with an unknown article and quantity (\d+)")]
        public async Task WhenICreateOrderForCustomerWithAnUnknownArticleAndQuantity(string orderNumber, string customerNumber, int quantity)
        {
            int customerId = context.CustomerIdsByNumber[customerNumber];
            _lastCreateResult = await createOrderUseCase.ExecuteAsync(new CreateOrderCommand(
                orderNumber, customerId, DefaultDeliveryDate, null, DefaultAddress, DefaultAddress,
                [new CreateOrderLineInput(999_999, quantity)]));
        }

        [When(@"I change the quantity of article ""([^""]*)"" on order ""([^""]*)"" to (\d+)")]
        public async Task WhenIChangeTheQuantityOfArticleOnOrderTo(string articleNumber, string orderNumber, int quantity)
        {
            int orderId = context.OrderIdsByNumber[orderNumber];
            Result<GetOrderDetailsResponse> details = await getOrderDetailsUseCase.ExecuteAsync(new GetOrderDetailsQuery(orderId));
            Assert.IsTrue(details.IsSuccess, details.Error);

            int articleId = context.ArticleIdsByNumber[articleNumber];
            OrderLineDto line = details.Value!.Lines.Single(l => l.ArticleId == articleId);

            Result result = await updateOrderLineQuantityUseCase.ExecuteAsync(
                new UpdateOrderLineQuantityCommand(orderId, line.OrderLineId, quantity));

            Assert.IsTrue(result.IsSuccess, result.Error);
        }

        [When(@"I create order ""([^""]*)"" for customer ""([^""]*)"" with delivery date ""([^""]*)"" and lines:")]
        public async Task WhenICreateOrderForCustomerWithDeliveryDateAndLines(
            string orderNumber, string customerNumber, string deliveryDate, Table linesTable)
            => await CreateOrderAsync(orderNumber, customerNumber, DateOnly.Parse(deliveryDate, CultureInfo.InvariantCulture), null, null, linesTable);

        [When(@"I create order ""([^""]*)"" for customer ""([^""]*)"" with delivery date ""([^""]*)"", billing address ""([^""]*)"" and delivery address ""([^""]*)"" and lines:")]
        public async Task WhenICreateOrderWithOverriddenAddressesAndLines(
            string orderNumber, string customerNumber, string deliveryDate, string billingAddress, string deliveryAddress, Table linesTable)
        {
            var billing = AddressText.Parse(billingAddress);
            var delivery = AddressText.Parse(deliveryAddress);

            await CreateOrderAsync(
                orderNumber,
                customerNumber,
                DateOnly.Parse(deliveryDate, CultureInfo.InvariantCulture),
                new AddressOverrideInput(billing.Street, billing.HouseNumber, billing.PostalCode, billing.City, billing.CountryCode),
                new AddressOverrideInput(delivery.Street, delivery.HouseNumber, delivery.PostalCode, delivery.City, delivery.CountryCode),
                linesTable);
        }

        [When(@"I search orders for ""([^""]*)""")]
        public async Task WhenISearchOrdersFor(string searchTerm)
        {
            Result<IReadOnlyList<OrderListItemDto>> result = await searchOrdersUseCase.ExecuteAsync(new SearchOrdersQuery(searchTerm));
            Assert.IsTrue(result.IsSuccess, result.Error);
            _lastSearchResult = result.Value;
        }

        [When(@"I list all orders")]
        public async Task WhenIListAllOrders()
        {
            Result<IReadOnlyList<OrderListItemDto>> result = await searchOrdersUseCase.ExecuteAsync(new SearchOrdersQuery(null));
            Assert.IsTrue(result.IsSuccess, result.Error);
            _lastSearchResult = result.Value;
        }

        [When(@"I delete order ""([^""]*)""")]
        public async Task WhenIDeleteOrder(string orderNumber)
        {
            int orderId = context.OrderIdsByNumber[orderNumber];
            Result result = await deleteOrderUseCase.ExecuteAsync(new DeleteOrderCommand(orderId));
            Assert.IsTrue(result.IsSuccess, result.Error);
        }

        [Then(@"order ""([^""]*)"" is created successfully")]
        public void ThenOrderIsCreatedSuccessfully(string orderNumber)
        {
            Assert.IsTrue(_lastCreateResult!.Value.IsSuccess, _lastCreateResult.Value.Error);
            Assert.AreEqual(orderNumber, _lastCreateResult.Value.Value!.OrderNumber);
        }

        [Then(@"order ""([^""]*)"" has (\d+) order lines?")]
        public async Task ThenOrderHasOrderLines(string orderNumber, int expectedCount)
        {
            GetOrderDetailsResponse details = await GetDetailsAsync(orderNumber);
            Assert.AreEqual(expectedCount, details.Lines.Count);
        }

        [Then(@"the total for order ""([^""]*)"" is ([\d.]+) CHF")]
        public async Task ThenTheTotalForOrderIs(string orderNumber, decimal expectedTotal)
        {
            GetOrderDetailsResponse details = await GetDetailsAsync(orderNumber);
            Assert.AreEqual(expectedTotal, details.TotalAmount);
            Assert.AreEqual("CHF", details.TotalCurrency);
        }

        [Then(@"the order creation is rejected because the quantity must be positive")]
        public void ThenTheOrderCreationIsRejectedBecauseTheQuantityMustBePositive()
        {
            Assert.IsFalse(_lastCreateResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCreateResult.Value.Error, "Quantity must be positive");
        }

        [Then(@"the order creation is rejected because the customer was not found")]
        public void ThenTheOrderCreationIsRejectedBecauseTheCustomerWasNotFound()
        {
            Assert.IsFalse(_lastCreateResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCreateResult.Value.Error, "Customer");
        }

        [Then(@"the order creation is rejected because the article was not found")]
        public void ThenTheOrderCreationIsRejectedBecauseTheArticleWasNotFound()
        {
            Assert.IsFalse(_lastCreateResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCreateResult.Value.Error, "Article");
        }

        [Then(@"the order creation is rejected because the order number already exists")]
        public void ThenTheOrderCreationIsRejectedBecauseTheOrderNumberAlreadyExists()
        {
            Assert.IsFalse(_lastCreateResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCreateResult.Value.Error, "already exists");
        }

        [Then(@"the order search returns exactly order ""([^""]*)""")]
        public void ThenTheOrderSearchReturnsExactlyOrder(string orderNumber)
        {
            Assert.AreEqual(1, _lastSearchResult!.Count);
            Assert.AreEqual(orderNumber, _lastSearchResult[0].OrderNumber);
        }

        [Then(@"the order list contains ""([^""]*)"" and ""([^""]*)""")]
        public void ThenTheOrderListContainsAnd(string firstOrderNumber, string secondOrderNumber)
        {
            Assert.IsTrue(_lastSearchResult!.Any(o => o.OrderNumber == firstOrderNumber));
            Assert.IsTrue(_lastSearchResult!.Any(o => o.OrderNumber == secondOrderNumber));
        }

        [Then(@"order ""([^""]*)"" can no longer be found")]
        public async Task ThenOrderCanNoLongerBeFound(string orderNumber)
        {
            int orderId = context.OrderIdsByNumber[orderNumber];
            Result<GetOrderDetailsResponse> result = await getOrderDetailsUseCase.ExecuteAsync(new GetOrderDetailsQuery(orderId));
            Assert.IsFalse(result.IsSuccess);
        }

        [Then(@"order ""([^""]*)"" has billing address ""([^""]*)""")]
        public async Task ThenOrderHasBillingAddress(string orderNumber, string expectedAddress)
        {
            GetOrderDetailsResponse details = await GetDetailsAsync(orderNumber);
            var actual = new AddressText(details.BillingStreet, details.BillingHouseNumber, details.BillingPostalCode, details.BillingCity, details.BillingCountryCode);
            Assert.AreEqual(expectedAddress, actual.ToDisplayString());
        }

        [Then(@"order ""([^""]*)"" has delivery address ""([^""]*)""")]
        public async Task ThenOrderHasDeliveryAddress(string orderNumber, string expectedAddress)
        {
            GetOrderDetailsResponse details = await GetDetailsAsync(orderNumber);
            var actual = new AddressText(details.DeliveryStreet, details.DeliveryHouseNumber, details.DeliveryPostalCode, details.DeliveryCity, details.DeliveryCountryCode);
            Assert.AreEqual(expectedAddress, actual.ToDisplayString());
        }

        [Then(@"the billing address for order ""([^""]*)"" is automatic")]
        public async Task ThenTheBillingAddressForOrderIsAutomatic(string orderNumber)
            => Assert.AreEqual(AddressSource.Automatic, (await GetDetailsAsync(orderNumber)).BillingAddressSource);

        [Then(@"the billing address for order ""([^""]*)"" is manual")]
        public async Task ThenTheBillingAddressForOrderIsManual(string orderNumber)
            => Assert.AreEqual(AddressSource.Manual, (await GetDetailsAsync(orderNumber)).BillingAddressSource);

        [Then(@"the delivery address for order ""([^""]*)"" is automatic")]
        public async Task ThenTheDeliveryAddressForOrderIsAutomatic(string orderNumber)
            => Assert.AreEqual(AddressSource.Automatic, (await GetDetailsAsync(orderNumber)).DeliveryAddressSource);

        [Then(@"the delivery address for order ""([^""]*)"" is manual")]
        public async Task ThenTheDeliveryAddressForOrderIsManual(string orderNumber)
            => Assert.AreEqual(AddressSource.Manual, (await GetDetailsAsync(orderNumber)).DeliveryAddressSource);

        [Then(@"order ""([^""]*)"" can not be found by search")]
        public async Task ThenOrderCanNotBeFoundBySearch(string orderNumber)
        {
            Result<IReadOnlyList<OrderListItemDto>> result = await searchOrdersUseCase.ExecuteAsync(new SearchOrdersQuery(orderNumber));
            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.Any(o => o.OrderNumber == orderNumber));
        }

        private async Task CreateOrderAsync(string orderNumber, string customerNumber, Table linesTable)
            => await CreateOrderAsync(orderNumber, customerNumber, DefaultDeliveryDate, DefaultAddress, DefaultAddress, linesTable);

        private async Task CreateOrderAsync(
            string orderNumber,
            string customerNumber,
            DateOnly deliveryDate,
            AddressOverrideInput? billingAddressOverride,
            AddressOverrideInput? deliveryAddressOverride,
            Table linesTable)
        {
            int customerId = context.CustomerIdsByNumber[customerNumber];
            IReadOnlyList<CreateOrderLineInput> lines = ToLineInputs(linesTable);

            _lastCreateResult = await createOrderUseCase.ExecuteAsync(
                new CreateOrderCommand(orderNumber, customerId, deliveryDate, null, billingAddressOverride, deliveryAddressOverride, lines));

            if (_lastCreateResult.Value.IsSuccess)
            {
                context.OrderIdsByNumber[orderNumber] = _lastCreateResult.Value.Value!.OrderId;
            }
        }

        private IReadOnlyList<CreateOrderLineInput> ToLineInputs(Table linesTable)
            => [.. linesTable.Rows.Select(row => new CreateOrderLineInput(
                context.ArticleIdsByNumber[row["ArticleNumber"]],
                int.Parse(row["Quantity"], CultureInfo.InvariantCulture)))];

        private async Task<GetOrderDetailsResponse> GetDetailsAsync(string orderNumber)
        {
            int orderId = context.OrderIdsByNumber[orderNumber];
            Result<GetOrderDetailsResponse> result = await getOrderDetailsUseCase.ExecuteAsync(new GetOrderDetailsQuery(orderId));
            Assert.IsTrue(result.IsSuccess, result.Error);
            return result.Value!;
        }
    }
}
