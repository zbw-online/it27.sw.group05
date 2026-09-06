using OrderManagement.Application.Features.Customers.CreateCustomer;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers
{
    [TestClass]
    public sealed class CreateCustomerUseCaseTests
    {
        private static CreateCustomerCommand ValidCommand(string customerNumber = "CU00001", string email = "jane.doe@example.com")
            => new(customerNumber, "Doe", "Jane", email, "example.com",
                DateOnly.FromDateTime(DateTime.Today), "Main Street", "1", "8000", "Zurich", "CH");

        [TestMethod]
        public async Task ExecuteAsync_WithValidCommand_ShouldPersistCustomerAndCommit()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var queryRepository = new FakeCustomerQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateCustomerUseCase(commandRepository, queryRepository, unitOfWork);

            Result<CreateCustomerResponse> result = await useCase.ExecuteAsync(ValidCommand());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("CU00001", result.Value!.CustomerNumber);
            Assert.AreEqual("Doe Jane", result.Value.FullName);
            Assert.AreEqual(1, commandRepository.Added.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDuplicateCustomerNumber_ShouldFailAndNotCommit()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var queryRepository = new FakeCustomerQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateCustomerUseCase(commandRepository, queryRepository, unitOfWork);

            _ = queryRepository.Seed(Customer.Create("CU00001", "Existing", "Customer", "existing@example.com", null).EnsureValue());

            Result<CreateCustomerResponse> result = await useCase.ExecuteAsync(ValidCommand());

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "already exists");
            Assert.AreEqual(0, commandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDuplicateEmail_ShouldFailAndNotCommit()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var queryRepository = new FakeCustomerQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateCustomerUseCase(commandRepository, queryRepository, unitOfWork);

            _ = queryRepository.Seed(Customer.Create("CU00002", "Existing", "Customer", "jane.doe@example.com", null).EnsureValue());

            Result<CreateCustomerResponse> result = await useCase.ExecuteAsync(ValidCommand());

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "Email");
            Assert.AreEqual(0, commandRepository.Added.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithInvalidCustomerNumberFormat_ShouldFailBeforeTouchingRepositories()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var queryRepository = new FakeCustomerQueryRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateCustomerUseCase(commandRepository, queryRepository, unitOfWork);

            Result<CreateCustomerResponse> result = await useCase.ExecuteAsync(ValidCommand(customerNumber: "not-a-number"));

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, commandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WhenCommitFails_ShouldReturnFailure()
        {
            var commandRepository = new FakeCustomerCommandRepository();
            var queryRepository = new FakeCustomerQueryRepository();
            var unitOfWork = new FakeUnitOfWork { FailureMessage = "Database unavailable." };
            var useCase = new CreateCustomerUseCase(commandRepository, queryRepository, unitOfWork);

            Result<CreateCustomerResponse> result = await useCase.ExecuteAsync(ValidCommand());

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Database unavailable.", result.Error);
        }
    }
}
