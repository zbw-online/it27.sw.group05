using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Application.Features.Customers.ExportCustomerData;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Customers.DataExchange;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Customers.DataExchange
{
    [TestClass]
    public sealed class ExportCustomerDataUseCaseTests
    {
        private static CustomerDataDto Customer(string customerNumber)
            => new(customerNumber, "Muster", "Hans", "hans@example.ch", "www.example.ch",
                new CustomerAddressDataDto(new DateOnly(2026, 1, 1), "Musterstrasse", "10", "8000", "Zürich", "CH"));

        [TestMethod]
        public async Task ExecuteAsync_WithWinterStichtag_ShouldConvertSwissLocalTimeToUtc()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json);
            var temporalRepository = new FakeCustomerTemporalQueryRepository();
            var useCase = new ExportCustomerDataUseCase(
                temporalRepository,
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                new ExportCustomerDataQuery(CustomerDataFormat.Json, new DateTime(2026, 1, 15, 18, 30, 0)));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(new DateTime(2026, 1, 15, 17, 30, 0, DateTimeKind.Utc), temporalRepository.CapturedAsOfUtc);
            Assert.AreEqual(new DateOnly(2026, 1, 15), temporalRepository.CapturedAsOfBusinessDate);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithSummerStichtag_ShouldApplyDaylightSavingOffset()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json);
            var temporalRepository = new FakeCustomerTemporalQueryRepository();
            var useCase = new ExportCustomerDataUseCase(
                temporalRepository,
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                new ExportCustomerDataQuery(CustomerDataFormat.Json, new DateTime(2026, 7, 15, 18, 30, 0)));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(new DateTime(2026, 7, 15, 16, 30, 0, DateTimeKind.Utc), temporalRepository.CapturedAsOfUtc);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithStichtagShortlyAfterSwissMidnight_ShouldUseSwissLocalBusinessDate()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json);
            var temporalRepository = new FakeCustomerTemporalQueryRepository();
            var useCase = new ExportCustomerDataUseCase(
                temporalRepository,
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeTimeProvider(DateTimeOffset.UtcNow));

            // 2026-01-01 00:30 Swiss local time is still 2025-12-31 23:30 UTC.
            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                new ExportCustomerDataQuery(CustomerDataFormat.Json, new DateTime(2026, 1, 1, 0, 30, 0)));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(new DateTime(2025, 12, 31, 23, 30, 0, DateTimeKind.Utc), temporalRepository.CapturedAsOfUtc);
            Assert.AreEqual(new DateOnly(2026, 1, 1), temporalRepository.CapturedAsOfBusinessDate);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithoutStichtag_ShouldDefaultToTimeProviderNow()
        {
            var fixedUtcNow = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json);
            var temporalRepository = new FakeCustomerTemporalQueryRepository();
            var useCase = new ExportCustomerDataUseCase(
                temporalRepository,
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeTimeProvider(fixedUtcNow));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                new ExportCustomerDataQuery(CustomerDataFormat.Json, null));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(fixedUtcNow.UtcDateTime, temporalRepository.CapturedAsOfUtc);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldGenerateSafeFileNameFromStichtag()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json);
            var temporalRepository = new FakeCustomerTemporalQueryRepository();
            var useCase = new ExportCustomerDataUseCase(
                temporalRepository,
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                new ExportCustomerDataQuery(CustomerDataFormat.Json, new DateTime(2026, 9, 5, 18, 30, 0)));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("kundendaten-20260905-1830.json", result.Value!.SafeFileName);
            Assert.AreEqual("application/x-fake", result.Value.MediaType);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnsupportedFormat_ShouldFail()
        {
            var temporalRepository = new FakeCustomerTemporalQueryRepository();
            var useCase = new ExportCustomerDataUseCase(
                temporalRepository,
                new FakeCustomerDataSerializerResolver(),
                new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                new ExportCustomerDataQuery(CustomerDataFormat.Json, new DateTime(2026, 9, 5, 18, 30, 0)));

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldPassCustomersFromRepositoryToSerializer()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json);
            var temporalRepository = new FakeCustomerTemporalQueryRepository
            {
                CustomersToReturn = [Customer("CU00001"), Customer("CU00002")],
            };
            var useCase = new ExportCustomerDataUseCase(
                temporalRepository,
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                new ExportCustomerDataQuery(CustomerDataFormat.Json, new DateTime(2026, 9, 5, 18, 30, 0)));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, jsonSerializer.SerializedCustomers.Count);
            Assert.AreEqual("CU00001", jsonSerializer.SerializedCustomers[0].CustomerNumber);
            Assert.AreEqual("CU00002", jsonSerializer.SerializedCustomers[1].CustomerNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCancelledToken_ShouldThrow()
        {
            var jsonSerializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json);
            var temporalRepository = new FakeCustomerTemporalQueryRepository();
            var useCase = new ExportCustomerDataUseCase(
                temporalRepository,
                new FakeCustomerDataSerializerResolver(jsonSerializer),
                new FakeTimeProvider(DateTimeOffset.UtcNow));
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => useCase.ExecuteAsync(new ExportCustomerDataQuery(CustomerDataFormat.Json, new DateTime(2026, 9, 5, 18, 30, 0)), cts.Token));
        }
    }
}
