using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.GetNextOrderNumber;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class GetNextOrderNumberUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithNoOrdersThisYear_ShouldReturnFirstNumber()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var useCase = new GetNextOrderNumberUseCase(orderQueryRepository);

            Result<string> result = await useCase.ExecuteAsync(new GetNextOrderNumberQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            string expectedPrefix = $"ORD-{DateTime.UtcNow.Year}-001";
            Assert.AreEqual(expectedPrefix, result.Value);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithExistingOrdersThisYear_ShouldIncrementHighestSuffix()
        {
            var orderQueryRepository = new FakeOrderQueryRepository();
            var useCase = new GetNextOrderNumberUseCase(orderQueryRepository);

            int year = DateTime.UtcNow.Year;
            _ = orderQueryRepository.Seed(ValidOrder($"ORD-{year}-001"));
            _ = orderQueryRepository.Seed(ValidOrder($"ORD-{year}-007"));

            Result<string> result = await useCase.ExecuteAsync(new GetNextOrderNumberQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual($"ORD-{year}-008", result.Value);
        }

        private static Order ValidOrder(string orderNumber)
            => Order.Create(
                orderNumber,
                new CustomerId(1),
                new DateOnly(2026, 9, 1),
                Address.Create("Main", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Main", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic)
            .EnsureValue();
    }
}
