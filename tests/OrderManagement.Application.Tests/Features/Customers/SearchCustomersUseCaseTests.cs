using OrderManagement.Application.Features.Customers.Contracts;
using OrderManagement.Application.Features.Customers.SearchCustomers;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers
{
    [TestClass]
    public sealed class SearchCustomersUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithEmptySearchTerm_ShouldReturnAllCustomersOrderedByName()
        {
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new SearchCustomersUseCase(queryRepository);

            _ = queryRepository.Seed(Customer.Create("CU00002", "Zimmer", "Bob", "bob@example.com", null).EnsureValue());
            _ = queryRepository.Seed(Customer.Create("CU00001", "Adams", "Alice", "alice@example.com", null).EnsureValue());

            Result<IReadOnlyList<CustomerListItemDto>> result = await useCase.ExecuteAsync(new SearchCustomersQuery(null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, result.Value!.Count);
            Assert.AreEqual("Adams", result.Value[0].FullName.Split(' ')[0]);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithSearchTerm_ShouldDelegateToNameOrNumberSearch()
        {
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new SearchCustomersUseCase(queryRepository);

            _ = queryRepository.Seed(Customer.Create("CU00001", "Adams", "Alice", "alice@example.com", null).EnsureValue());
            _ = queryRepository.Seed(Customer.Create("CU00002", "Zimmer", "Bob", "bob@example.com", null).EnsureValue());

            Result<IReadOnlyList<CustomerListItemDto>> result = await useCase.ExecuteAsync(new SearchCustomersQuery("Adams"));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            StringAssert.Contains(result.Value[0].FullName, "Adams");
        }
    }
}
