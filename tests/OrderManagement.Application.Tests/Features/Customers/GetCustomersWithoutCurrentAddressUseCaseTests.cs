using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Customers.GetCustomersWithoutCurrentAddress;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers
{
    [TestClass]
    public sealed class GetCustomersWithoutCurrentAddressUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_ShouldOnlyReturnCustomersWithoutAnActiveAddressToday()
        {
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomersWithoutCurrentAddressUseCase(customerQueryRepository);

            Customer withoutAddress = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            Customer withAddress = Customer.Create("CU00002", "Smith", "John", "john@example.com", null).EnsureValue();
            _ = withAddress.ChangeAddress(DateOnly.FromDateTime(DateTime.Today), "Main", "1", "8000", "Zurich", "CH");
            _ = customerQueryRepository.Seed(withAddress);

            Result<IReadOnlyList<CustomerWithoutAddressDto>> result = await useCase.ExecuteAsync(new GetCustomersWithoutCurrentAddressQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual(withoutAddress.Id.Value, result.Value[0].CustomerId);
        }
    }
}
