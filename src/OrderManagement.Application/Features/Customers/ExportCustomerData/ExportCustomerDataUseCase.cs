using System.Globalization;

using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.ExportCustomerData
{
    public sealed class ExportCustomerDataUseCase(
        ICustomerCurrentQueryRepository currentQueryRepository,
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

            Result<(IReadOnlyList<CustomerDataDto>, DateTime)> loadResult = query.Mode switch
            {
                CustomerExportMode.Current => await LoadCurrentCustomersAsync(cancellationToken),
                CustomerExportMode.Historical => await LoadHistoricalCustomersAsync(query.Stichtag, cancellationToken),
                _ => Results.Fail<(IReadOnlyList<CustomerDataDto>, DateTime)>("Unbekannter Exportmodus."),
            };

            if (!loadResult.IsSuccess)
            {
                return Results.Fail<CustomerDataFile>(loadResult.Error!);
            }

            (IReadOnlyList<CustomerDataDto> customers, DateTime stichtagLocal) = loadResult.Value;

            ICustomerDataSerializer serializer = resolveResult.Value!;
            using var stream = new MemoryStream();
            await serializer.SerializeAsync(customers, stream, cancellationToken);

            string fileName =
                $"kundendaten-{stichtagLocal.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}" +
                $"-{stichtagLocal.ToString("HHmm", CultureInfo.InvariantCulture)}.{serializer.FileExtension}";

            return Results.Success(new CustomerDataFile(fileName, serializer.Format, serializer.MediaType, stream.ToArray()));
        }

        private async Task<Result<(IReadOnlyList<CustomerDataDto>, DateTime)>> LoadCurrentCustomersAsync(
            CancellationToken cancellationToken)
        {
            DateTime nowLocal = TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, ZurichTimeZone);
            var today = DateOnly.FromDateTime(nowLocal);

            IReadOnlyList<CustomerDataDto> customers = await currentQueryRepository.GetAllCurrentAsync(today, cancellationToken);
            return Results.Success((customers, nowLocal));
        }

        private async Task<Result<(IReadOnlyList<CustomerDataDto>, DateTime)>> LoadHistoricalCustomersAsync(
            DateTime? stichtag,
            CancellationToken cancellationToken)
        {
            if (stichtag is null)
            {
                return Results.Fail<(IReadOnlyList<CustomerDataDto>, DateTime)>(
                    "Für den historischen Export muss ein Stichtag angegeben werden.");
            }

            DateTime stichtagLocal = stichtag.Value;
            var stichtagUnspecified = DateTime.SpecifyKind(stichtagLocal, DateTimeKind.Unspecified);

            if (ZurichTimeZone.IsInvalidTime(stichtagUnspecified))
            {
                return Results.Fail<(IReadOnlyList<CustomerDataDto>, DateTime)>(
                    "Der gewählte Stichtag existiert wegen der Umstellung auf die Sommerzeit nicht. Bitte wählen Sie einen anderen Zeitpunkt.");
            }

            if (ZurichTimeZone.IsAmbiguousTime(stichtagUnspecified))
            {
                return Results.Fail<(IReadOnlyList<CustomerDataDto>, DateTime)>(
                    "Der gewählte Stichtag ist wegen der Umstellung auf die Winterzeit mehrdeutig. Bitte wählen Sie einen anderen Zeitpunkt.");
            }

            DateTime stichtagUtc = TimeZoneInfo.ConvertTimeToUtc(stichtagUnspecified, ZurichTimeZone);

            if (stichtagUtc > timeProvider.GetUtcNow().UtcDateTime)
            {
                return Results.Fail<(IReadOnlyList<CustomerDataDto>, DateTime)>("Der Stichtag darf nicht in der Zukunft liegen.");
            }

            var stichtagBusinessDate = DateOnly.FromDateTime(stichtagLocal);
            IReadOnlyList<CustomerDataDto> customers = await temporalQueryRepository.GetCustomersAsOfAsync(
                stichtagUtc,
                stichtagBusinessDate,
                cancellationToken);

            return Results.Success((customers, stichtagLocal));
        }
    }
}
