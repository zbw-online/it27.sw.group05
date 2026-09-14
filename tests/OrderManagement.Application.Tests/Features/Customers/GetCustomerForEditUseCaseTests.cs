using OrderManagement.Application.Features.Customers.GetCustomerForEdit;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers
{
    [TestClass]
    public sealed class GetCustomerForEditUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingCustomer_ShouldReturnCurrentAddressAndDetails()
        {
            var today = new DateOnly(2026, 8, 30);
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerForEditUseCase(
                queryRepository,
                new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane.doe@example.com", null).EnsureValue();
            customer.ChangeAddress(today.AddMonths(-1), "Main Street", "1", "8000", "Zurich", "CH").EnsureSuccess();
            _ = queryRepository.Seed(customer);

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Main Street", result.Value!.Street);
            Assert.AreEqual("CU00001", result.Value.CustomerNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownCustomer_ShouldFail()
        {
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerForEditUseCase(queryRepository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(999));

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCurrentAndFutureAddress_ShouldSelectAddressValidOnTimeProviderToday()
        {
            var today = new DateOnly(2026, 8, 30);
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerForEditUseCase(
                queryRepository,
                new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

            Customer customer = Customer.Create("CU00002", "Doe", "June", "june@example.com", null).EnsureValue();
            customer.ChangeAddress(today.AddMonths(-1), "Current Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(today.AddMonths(1), "Future Street", "2", "8000", "Zurich", "CH").EnsureSuccess();
            _ = queryRepository.Seed(customer);

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Current Street", result.Value!.Street);
        }
    }
}
