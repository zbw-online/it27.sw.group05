using OrderManagement.Application.Features.Customers.GetCustomerDetails;
using OrderManagement.Application.Tests.Fakes;
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
            var today = new DateOnly(2026, 8, 30);
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(
                queryRepository,
                new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

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
        public async Task ExecuteAsync_WithMultipleFutureAddresses_ShouldOrderFutureAddressesAscendingByValidFrom()
        {
            var today = new DateOnly(2026, 8, 30);
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(
                queryRepository,
                new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

            Customer customer = Customer.Create("CU00002", "Doe", "June", "june@example.com", null).EnsureValue();
            customer.ChangeAddress(today.AddMonths(-1), "Current Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(today.AddMonths(1), "First Future Street", "2", "8000", "Zurich", "CH").EnsureSuccess();
            customer.ChangeAddress(today.AddMonths(3), "Second Future Street", "3", "3000", "Bern", "CH").EnsureSuccess();
            _ = queryRepository.Seed(customer);

            Result<GetCustomerDetailsResponse> result = await useCase.ExecuteAsync(new GetCustomerDetailsQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, result.Value!.FutureAddresses.Count);
            Assert.AreEqual("First Future Street", result.Value.FutureAddresses[0].Street);
            Assert.AreEqual("Second Future Street", result.Value.FutureAddresses[1].Street);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithAddressValidFromToday_ShouldClassifyAsCurrent()
        {
            var today = new DateOnly(2026, 8, 30);
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(
                queryRepository,
                new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

            Customer customer = Customer.Create("CU00003", "Doe", "Jess", "jess@example.com", null).EnsureValue();
            customer.ChangeAddress(today, "Today Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            _ = queryRepository.Seed(customer);

            Result<GetCustomerDetailsResponse> result = await useCase.ExecuteAsync(new GetCustomerDetailsQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("Today Street", result.Value!.CurrentAddress!.Street);
            Assert.AreEqual(0, result.Value.FutureAddresses.Count);
            Assert.AreEqual(0, result.Value.PreviousAddresses.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithAddressValidFromTomorrow_ShouldClassifyAsFuture()
        {
            var today = new DateOnly(2026, 8, 30);
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(
                queryRepository,
                new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

            Customer customer = Customer.Create("CU00004", "Doe", "Sam", "sam@example.com", null).EnsureValue();
            customer.ChangeAddress(today.AddMonths(-1), "Current Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(today.AddDays(1), "Tomorrow Street", "2", "8000", "Zurich", "CH").EnsureSuccess();
            _ = queryRepository.Seed(customer);

            Result<GetCustomerDetailsResponse> result = await useCase.ExecuteAsync(new GetCustomerDetailsQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.FutureAddresses.Count);
            Assert.AreEqual("Tomorrow Street", result.Value.FutureAddresses[0].Street);
            Assert.AreEqual("Current Street", result.Value.CurrentAddress!.Street);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithAddressValidToYesterday_ShouldClassifyAsPrevious()
        {
            var today = new DateOnly(2026, 8, 30);
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(
                queryRepository,
                new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

            Customer customer = Customer.Create("CU00005", "Doe", "Alex", "alex@example.com", null).EnsureValue();
            customer.ChangeAddress(today.AddMonths(-2), "Old Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(today, "Current Street", "2", "8000", "Zurich", "CH").EnsureSuccess();
            _ = queryRepository.Seed(customer);

            Result<GetCustomerDetailsResponse> result = await useCase.ExecuteAsync(new GetCustomerDetailsQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.PreviousAddresses.Count);
            Assert.AreEqual("Old Street", result.Value.PreviousAddresses[0].Street);
            Assert.AreEqual(today.AddDays(-1), result.Value.PreviousAddresses[0].ValidTo);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithFutureAddress_ShouldNotReplaceCurrentAddressPrematurely()
        {
            var today = new DateOnly(2026, 8, 30);
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(
                queryRepository,
                new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)));

            Customer customer = Customer.Create("CU00006", "Doe", "Robin", "robin@example.com", null).EnsureValue();
            customer.ChangeAddress(today.AddMonths(-6), "Current Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(today.AddDays(1), "Future Street", "2", "8000", "Zurich", "CH").EnsureSuccess();
            _ = queryRepository.Seed(customer);

            Result<GetCustomerDetailsResponse> result = await useCase.ExecuteAsync(new GetCustomerDetailsQuery(customer.Id.Value));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsNotNull(result.Value!.CurrentAddress);
            Assert.AreEqual("Current Street", result.Value.CurrentAddress!.Street);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownCustomer_ShouldFail()
        {
            var queryRepository = new FakeCustomerQueryRepository();
            var useCase = new GetCustomerDetailsUseCase(queryRepository, new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<GetCustomerDetailsResponse> result = await useCase.ExecuteAsync(new GetCustomerDetailsQuery(999));

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
