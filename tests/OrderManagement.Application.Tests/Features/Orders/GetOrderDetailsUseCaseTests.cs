using OrderManagement.Application.Features.Orders.GetOrderDetails;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class GetOrderDetailsUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingOrderAndLines_ShouldReturnDetailsWithCorrectTotal()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetOrderDetailsUseCase(orderQueryRepository, customerQueryRepository, new FakeTimeProvider(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero)));

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            Order order = Order.Create(
                "ORD-2026-001",
                customer.Id,
                new DateOnly(2026, 9, 6),
                Address.Create("Rechnungsweg", "2", "8001", "Zurich", "CH").EnsureValue(),
                AddressSource.Manual,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                "Projekt XY")
                .EnsureValue();

            _ = order.AddLine(new ArticleId(1), "Widget", Money.From(10m, "CHF").EnsureValue(), 3);
            _ = orderQueryRepository.Seed(order);

            Result<GetOrderDetailsResponse> result = await useCase.ExecuteAsync(new GetOrderDetailsQuery(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("ORD-2026-001", result.Value!.OrderNumber);
            Assert.AreEqual("CU00001", result.Value.CustomerNumber);
            Assert.AreEqual(1, result.Value.Lines.Count);
            Assert.AreEqual(30m, result.Value.TotalAmount);
            Assert.AreEqual(new DateOnly(2026, 9, 6), result.Value.DeliveryDate);
            Assert.AreEqual("Projekt XY", result.Value.CustomerReference);
            Assert.AreEqual("Rechnungsweg", result.Value.BillingStreet);
            Assert.AreEqual(AddressSource.Manual, result.Value.BillingAddressSource);
            Assert.AreEqual("Main Street", result.Value.DeliveryStreet);
            Assert.AreEqual(AddressSource.Automatic, result.Value.DeliveryAddressSource);
            Assert.AreEqual(OrderStatus.Open, result.Value.Status);
            Assert.IsTrue(result.Value.IsOverdue, "Delivery date is before the TimeProvider's 'today', so the still-open order must be overdue.");
        }

        [TestMethod]
        public async Task ExecuteAsync_WithFutureDeliveryDate_ShouldNotBeOverdue()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetOrderDetailsUseCase(orderQueryRepository, customerQueryRepository, new FakeTimeProvider(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero)));

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00002", "Doe", "Jane", "jane2@example.com", null).EnsureValue());

            Order order = Order.Create(
                "ORD-2026-002",
                customer.Id,
                new DateOnly(2026, 12, 1),
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic)
                .EnsureValue();
            _ = orderQueryRepository.Seed(order);

            Result<GetOrderDetailsResponse> result = await useCase.ExecuteAsync(new GetOrderDetailsQuery(order.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.IsOverdue);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownOrder_ShouldFail()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetOrderDetailsUseCase(orderQueryRepository, customerQueryRepository, new FakeTimeProvider(new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero)));

            Result<GetOrderDetailsResponse> result = await useCase.ExecuteAsync(new GetOrderDetailsQuery(999));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
