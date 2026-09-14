using OrderManagement.Application.Features.Customers.DeleteCustomer;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers
{
    [TestClass]
    public sealed class DeleteCustomerUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithExistingCustomer_ShouldRemoveAndCommit()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteCustomerUseCase(commandRepository, unitOfWork);

            Customer customer = commandRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane.doe@example.com", null).EnsureValue());

            Result result = await useCase.ExecuteAsync(new DeleteCustomerCommand(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, commandRepository.Removed.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownCustomer_ShouldFailAndNotCommit()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new DeleteCustomerUseCase(commandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new DeleteCustomerCommand(999));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
