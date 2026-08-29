using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Customers.GetCustomerDetails;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers
{
    [TestClass]
    public sealed class GetCustomerDetailsUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithPreviousCurrentAndFutureAddresses_ShouldGroupThemByStatus()
        {
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(queryRepository);

            var today = DateOnly.FromDateTime(DateTime.Today);
            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane.doe@example.com", null).EnsureValue();
            customer.ChangeAddress(today.AddMonths(-3), "Previous Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(today.AddMonths(-1), "Current Street", "2", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(today.AddMonths(1), "Future Street", "3", "8000", "Zurich", "CH").EnsureSuccess();
            _ = queryRepository.Seed(customer);

            Result<GetCustomerDetailsResponse> result = await useCase.ExecuteAsync(new GetCustomerDetailsQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Current Street", result.Value!.CurrentAddress!.Street);
            Assert.AreEqual(1, result.Value.PreviousAddresses.Count);
            Assert.AreEqual(1, result.Value.FutureAddresses.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownCustomer_ShouldFail()
        {
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(queryRepository);

            Result<GetCustomerDetailsResponse> result = await useCase.ExecuteAsync(new GetCustomerDetailsQuery(999));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
