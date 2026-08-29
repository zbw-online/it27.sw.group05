using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Customers.UpdateCustomer;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers
{
    [TestClass]
    public sealed class UpdateCustomerUseCaseTests
    {
        private static UpdateCustomerCommand CommandFor(Customer customer, string email = "jane.doe@example.com")
            => new(customer.Id.Value, "Doe", "Jane", email, "example.com",
                DateOnly.FromDateTime(DateTime.Today).AddMonths(-1), "Main Street", "1", "8000", "Zurich", "CH");

        [TestMethod]
        public async Task ExecuteAsync_WithValidChanges_ShouldUpdateAndCommit()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var queryRepository = new FakeCustomerQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateCustomerUseCase(commandRepository, queryRepository, unitOfWork);

            Customer customer = commandRepository.Seed(
                Customer.Create("CU00001", "Old", "Name", "old@example.com", null).EnsureValue());

            Result result = await useCase.ExecuteAsync(CommandFor(customer));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Doe", customer.LastName);
            Assert.AreEqual(1, commandRepository.Updated.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithEmailBelongingToAnotherCustomer_ShouldFail()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var queryRepository = new FakeCustomerQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateCustomerUseCase(commandRepository, queryRepository, unitOfWork);

            Customer customer = Customer.Create("CU00001", "Old", "Name", "old@example.com", null).EnsureValue();
            _ = commandRepository.Seed(customer);
            _ = queryRepository.Seed(customer);
            _ = queryRepository.Seed(
                Customer.Create("CU00002", "Other", "Person", "taken@example.com", null).EnsureValue());

            Result result = await useCase.ExecuteAsync(CommandFor(customer, email: "taken@example.com"));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "already exists");
            Assert.AreEqual(0, commandRepository.Updated.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownCustomer_ShouldFail()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var queryRepository = new FakeCustomerQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new UpdateCustomerUseCase(commandRepository, queryRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new UpdateCustomerCommand(
                999, "Doe", "Jane", "jane.doe@example.com", null,
                DateOnly.FromDateTime(DateTime.Today), "Main Street", "1", "8000", "Zurich", "CH"));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
