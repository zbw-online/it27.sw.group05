using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Customers.AddCustomerAddress;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers
{
    [TestClass]
    public sealed class AddCustomerAddressUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_WithFutureValidFrom_ShouldAddAddressAndCommit()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddCustomerAddressUseCase(commandRepository, unitOfWork);

            Customer customer = commandRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane.doe@example.com", null).EnsureValue());
            customer.ChangeAddress(DateOnly.FromDateTime(DateTime.Today).AddMonths(-1), "Old Street", "1", "8000", "Zurich", "CH").EnsureSuccess();

            DateOnly futureValidFrom = DateOnly.FromDateTime(DateTime.Today).AddMonths(1);
            Result result = await useCase.ExecuteAsync(new AddCustomerAddressCommand(
                customer.Id.Value, futureValidFrom, "New Street", "2", "9000", "St. Gallen", "CH"));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, customer.Addresses.Count);
            Assert.AreEqual(1, commandRepository.Updated.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownCustomer_ShouldFail()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new AddCustomerAddressUseCase(commandRepository, unitOfWork);

            Result result = await useCase.ExecuteAsync(new AddCustomerAddressCommand(
                999, DateOnly.FromDateTime(DateTime.Today), "New Street", "2", "9000", "St. Gallen", "CH"));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
