using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.SearchOrders;
using OrderManagement.Application.Features.Orders.Shared;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class SearchOrdersUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithoutSearchTerm_ShouldReturnAllOrdersNewestFirst()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var useCase = new SearchOrdersUseCase(orderQueryRepository, customerQueryRepository);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            _ = orderQueryRepository.Seed(ValidOrder(customer.Id, "ORD-2026-001"));
            _ = orderQueryRepository.Seed(ValidOrder(customer.Id, "ORD-2026-002"));

            Result<IReadOnlyList<OrderListItemDto>> result = await useCase.ExecuteAsync(new SearchOrdersQuery(null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, result.Value!.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithOrderNumberSearchTerm_ShouldFindOrderByItsNumber()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var useCase = new SearchOrdersUseCase(orderQueryRepository, customerQueryRepository);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            _ = orderQueryRepository.Seed(ValidOrder(customer.Id, "ORD-2026-001"));
            _ = orderQueryRepository.Seed(ValidOrder(customer.Id, "ORD-2026-002"));

            Result<IReadOnlyList<OrderListItemDto>> result = await useCase.ExecuteAsync(new SearchOrdersQuery("ORD-2026-002"));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ORD-2026-002", result.Value[0].OrderNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCustomerNumberSearchTerm_ShouldFindOrdersOfThatCustomer()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var useCase = new SearchOrdersUseCase(orderQueryRepository, customerQueryRepository);

            Customer first = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());
            Customer second = customerQueryRepository.Seed(
                Customer.Create("CU00002", "Smith", "John", "john@example.com", null).EnsureValue());

            _ = orderQueryRepository.Seed(ValidOrder(first.Id, "ORD-2026-001"));
            _ = orderQueryRepository.Seed(ValidOrder(second.Id, "ORD-2026-002"));

            Result<IReadOnlyList<OrderListItemDto>> result = await useCase.ExecuteAsync(new SearchOrdersQuery("CU00002"));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("ORD-2026-002", result.Value[0].OrderNumber);
        }

        private static Order ValidOrder(Domain.Customers.ValueObjects.CustomerId customerId, string orderNumber)
            => Order.Create(
                orderNumber,
                customerId,
                Address.Create("Main Street", "1", "8000", "Zurich", "CH").EnsureValue())
            .EnsureValue();
    }
}
