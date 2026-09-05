using System.Globalization;

using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.DataExchange.Shared;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.ExportCustomerData
{
    public sealed class ExportCustomerDataUseCase(
        ICustomerTemporalQueryRepository temporalQueryRepository,
        ICustomerDataSerializerResolver serializerResolver,
        TimeProvider timeProvider) : IExportCustomerDataUseCase
    {
        private static readonly TimeZoneInfo ZurichTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

        public async Task<Result<CustomerDataFile>> ExecuteAsync(
            ExportCustomerDataQuery query,
            CancellationToken cancellationToken = default)
        {
            Result<ICustomerDataSerializer> resolveResult = serializerResolver.Resolve(query.Format);
            if (!resolveResult.IsSuccess)
            {
                return Results.Fail<CustomerDataFile>("Das Dateiformat wird nicht unterstützt.");
            }

            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, ZurichTimeZone);
            DateTime stichtagLocal = query.Stichtag ?? nowLocal;
            DateTime stichtagUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(stichtagLocal, DateTimeKind.Unspecified),
                ZurichTimeZone);

            var stichtagBusinessDate = DateOnly.FromDateTime(stichtagLocal);
            IReadOnlyList<CustomerDataDto> customers = await temporalQueryRepository.GetCustomersAsOfAsync(
                stichtagUtc,
                stichtagBusinessDate,
                cancellationToken);

            ICustomerDataSerializer serializer = resolveResult.Value!;
            using var stream = new MemoryStream();
            await serializer.SerializeAsync(customers, stream, cancellationToken);

            string fileName =
                $"kundendaten-{stichtagLocal.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}" +
                $"-{stichtagLocal.ToString("HHmm", CultureInfo.InvariantCulture)}.{serializer.FileExtension}";

            return Results.Success(new CustomerDataFile(fileName, serializer.Format, serializer.MediaType, stream.ToArray()));
        }
    }
}
