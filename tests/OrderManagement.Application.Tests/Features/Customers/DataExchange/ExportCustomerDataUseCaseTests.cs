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

        private static ExportCustomerDataUseCase CreateUseCase(
            out FakeCustomerCurrentQueryRepository currentRepository,
            out FakeCustomerTemporalQueryRepository temporalRepository,
            out FakeCustomerDataSerializer serializer,
            DateTimeOffset? utcNow = null)
        {
            currentRepository = new FakeCustomerCurrentQueryRepository();
            temporalRepository = new FakeCustomerTemporalQueryRepository();
            serializer = new FakeCustomerDataSerializer(CustomerDataFormat.Json);
            return new ExportCustomerDataUseCase(
                currentRepository,
                temporalRepository,
                new FakeCustomerDataSerializerResolver(serializer),
                new FakeTimeProvider(utcNow ?? DateTimeOffset.UtcNow));
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCurrentMode_ShouldNotUseTemporalRepository()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out FakeCustomerCurrentQueryRepository currentRepository,
                out FakeCustomerTemporalQueryRepository temporalRepository,
                out FakeCustomerDataSerializer _);

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(ExportCustomerDataQuery.Current(CustomerDataFormat.Json));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, currentRepository.CallCount);
            Assert.IsNull(temporalRepository.CapturedAsOfUtc);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCurrentMode_ShouldQueryUsingTodaysSwissBusinessDate()
        {
            var fixedUtcNow = new DateTimeOffset(2026, 3, 10, 23, 30, 0, TimeSpan.Zero);
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out FakeCustomerCurrentQueryRepository currentRepository,
                out _,
                out FakeCustomerDataSerializer _,
                fixedUtcNow);

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(ExportCustomerDataQuery.Current(CustomerDataFormat.Json));

            Assert.IsTrue(result.IsSuccess, result.Error);
            // 2026-03-10 23:30 UTC is already 2026-03-11 00:30 Swiss local time.
            Assert.AreEqual(new DateOnly(2026, 3, 11), currentRepository.CapturedAsOfBusinessDate);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCurrentMode_ShouldPassCustomersFromRepositoryToSerializer()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out FakeCustomerCurrentQueryRepository currentRepository,
                out _,
                out FakeCustomerDataSerializer serializer);
            currentRepository.CustomersToReturn = [Customer("CU00001"), Customer("CU00002")];

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(ExportCustomerDataQuery.Current(CustomerDataFormat.Json));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(2, serializer.SerializedCustomers.Count);
            Assert.AreEqual("CU00001", serializer.SerializedCustomers[0].CustomerNumber);
            Assert.AreEqual("CU00002", serializer.SerializedCustomers[1].CustomerNumber);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithHistoricalModeAndWinterStichtag_ShouldConvertSwissLocalTimeToUtc()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out _,
                out FakeCustomerTemporalQueryRepository temporalRepository,
                out FakeCustomerDataSerializer _,
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                ExportCustomerDataQuery.Historical(CustomerDataFormat.Json, new DateTime(2026, 1, 15, 18, 30, 0)));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(new DateTime(2026, 1, 15, 17, 30, 0, DateTimeKind.Utc), temporalRepository.CapturedAsOfUtc);
            Assert.AreEqual(new DateOnly(2026, 1, 15), temporalRepository.CapturedAsOfBusinessDate);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithHistoricalModeAndSummerStichtag_ShouldApplyDaylightSavingOffset()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out _,
                out FakeCustomerTemporalQueryRepository temporalRepository,
                out FakeCustomerDataSerializer _,
                new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                ExportCustomerDataQuery.Historical(CustomerDataFormat.Json, new DateTime(2026, 7, 15, 18, 30, 0)));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(new DateTime(2026, 7, 15, 16, 30, 0, DateTimeKind.Utc), temporalRepository.CapturedAsOfUtc);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithHistoricalModeShortlyAfterSwissMidnight_ShouldUseSwissLocalBusinessDate()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out _,
                out FakeCustomerTemporalQueryRepository temporalRepository,
                out FakeCustomerDataSerializer _,
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

            // 2026-01-01 00:30 Swiss local time is still 2025-12-31 23:30 UTC.
            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                ExportCustomerDataQuery.Historical(CustomerDataFormat.Json, new DateTime(2026, 1, 1, 0, 30, 0)));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(new DateTime(2025, 12, 31, 23, 30, 0, DateTimeKind.Utc), temporalRepository.CapturedAsOfUtc);
            Assert.AreEqual(new DateOnly(2026, 1, 1), temporalRepository.CapturedAsOfBusinessDate);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithHistoricalModeAndNoStichtag_ShouldFail()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(out _, out _, out FakeCustomerDataSerializer _);

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                new ExportCustomerDataQuery(CustomerDataFormat.Json, CustomerExportMode.Historical, null));

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithHistoricalModeAndFutureStichtag_ShouldFail()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out _,
                out _,
                out FakeCustomerDataSerializer _,
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                ExportCustomerDataQuery.Historical(CustomerDataFormat.Json, new DateTime(2026, 6, 1, 12, 0, 0)));

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithHistoricalModeAndInvalidLocalTimeDuringSpringForward_ShouldFail()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out _,
                out _,
                out FakeCustomerDataSerializer _,
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

            // Swiss clocks jump from 02:00 to 03:00 on the last Sunday in March; 02:30 does not exist.
            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                ExportCustomerDataQuery.Historical(CustomerDataFormat.Json, new DateTime(2026, 3, 29, 2, 30, 0)));

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithHistoricalModeAndAmbiguousLocalTimeDuringFallBack_ShouldFail()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out _,
                out _,
                out FakeCustomerDataSerializer _,
                new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero));

            // Swiss clocks fall back from 03:00 to 02:00 on the last Sunday in October; 02:30 occurs twice.
            Result<CustomerDataFile> result = await useCase.ExecuteAsync(
                ExportCustomerDataQuery.Historical(CustomerDataFormat.Json, new DateTime(2026, 10, 25, 2, 30, 0)));

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task ExecuteAsync_ShouldGenerateSafeFileNameFromCurrentTime()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out _,
                out _,
                out _,
                new DateTimeOffset(2026, 9, 5, 16, 30, 0, TimeSpan.Zero));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(ExportCustomerDataQuery.Current(CustomerDataFormat.Json));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("kundendaten-20260905-1830.json", result.Value!.SafeFileName);
            Assert.AreEqual("application/x-fake", result.Value.MediaType);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnsupportedFormat_ShouldFail()
        {
            var currentRepository = new FakeCustomerCurrentQueryRepository();
            var temporalRepository = new FakeCustomerTemporalQueryRepository();
            var useCase = new ExportCustomerDataUseCase(
                currentRepository,
                temporalRepository,
                new FakeCustomerDataSerializerResolver(),
                new FakeTimeProvider(DateTimeOffset.UtcNow));

            Result<CustomerDataFile> result = await useCase.ExecuteAsync(ExportCustomerDataQuery.Current(CustomerDataFormat.Json));

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCancelledTokenInCurrentMode_ShouldThrow()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(out _, out _, out FakeCustomerDataSerializer _);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => useCase.ExecuteAsync(ExportCustomerDataQuery.Current(CustomerDataFormat.Json), cts.Token));
        }

        [TestMethod]
        public async Task ExecuteAsync_WithCancelledTokenInHistoricalMode_ShouldThrow()
        {
            ExportCustomerDataUseCase useCase = CreateUseCase(
                out _,
                out _,
                out FakeCustomerDataSerializer _,
                new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero));
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            _ = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
                () => useCase.ExecuteAsync(
                    ExportCustomerDataQuery.Historical(CustomerDataFormat.Json, new DateTime(2026, 9, 5, 18, 30, 0)),
                    cts.Token));
        }
    }
}
