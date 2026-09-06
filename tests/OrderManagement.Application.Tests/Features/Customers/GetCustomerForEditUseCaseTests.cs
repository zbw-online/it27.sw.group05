using OrderManagement.Application.Features.Customers.GetCustomerForEdit;
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
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerForEditUseCase(queryRepository);

            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane.doe@example.com", null).EnsureValue();
            customer.ChangeAddress(DateOnly.FromDateTime(DateTime.Today).AddMonths(-1), "Main Street", "1", "8000", "Zurich", "CH").EnsureSuccess();
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
            var useCase = new GetCustomerForEditUseCase(queryRepository);

            Result<GetCustomerForEditResponse> result = await useCase.ExecuteAsync(new GetCustomerForEditQuery(999));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
