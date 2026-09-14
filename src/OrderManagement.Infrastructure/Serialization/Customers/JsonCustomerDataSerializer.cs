using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Infrastructure.Serialization.Customers.Json;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Serialization.Customers
{
    public sealed class JsonCustomerDataSerializer(IOptions<CustomerDataExchangeOptions> options) : ICustomerDataSerializer
    {
        private readonly JsonSerializerOptions _serializerOptions = CreateSerializerOptions(options.Value.MaxJsonDepth);
        private readonly int _maxDepth = options.Value.MaxJsonDepth;

        public CustomerDataFormat Format => CustomerDataFormat.Json;
        public string FileExtension => "json";
        public string MediaType => "application/json";

        public async Task SerializeAsync(
            IReadOnlyList<CustomerDataDto> customers,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            List<CustomerJsonContract> ordered = [.. customers
                .OrderBy(c => c.CustomerNumber, StringComparer.Ordinal)
                .Select(ToContract)];

            await JsonSerializer.SerializeAsync(destination, ordered, _serializerOptions, cancellationToken);
        }

        public async Task<Result<IReadOnlyList<CustomerDataDto>>> DeserializeAsync(
            Stream source,
            CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, cancellationToken);
            byte[] bytes = buffer.ToArray();

            if (bytes.Length == 0)
            {
                return Results.Fail<IReadOnlyList<CustomerDataDto>>("The file is empty.");
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = _maxDepth });
            }
            catch (JsonException)
            {
                return Results.Fail<IReadOnlyList<CustomerDataDto>>("Malformed JSON content.");
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return Results.Fail<IReadOnlyList<CustomerDataDto>>("The JSON document must have an array as its root.");
                }

                string? duplicateError = FindDuplicateProperty(document.RootElement);
                if (duplicateError is not null)
                {
                    return Results.Fail<IReadOnlyList<CustomerDataDto>>(duplicateError);
                }

                int index = 0;
                foreach (JsonElement element in document.RootElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Null)
                    {
                        return Results.Fail<IReadOnlyList<CustomerDataDto>>($"Customer entry at index {index} must not be null.");
                    }

                    if (element.ValueKind != JsonValueKind.Object)
                    {
                        return Results.Fail<IReadOnlyList<CustomerDataDto>>($"Customer entry at index {index} must be an object.");
                    }

                    index++;
                }

                List<CustomerJsonContract>? contracts;
                try
                {
                    contracts = JsonSerializer.Deserialize<List<CustomerJsonContract>>(bytes, _serializerOptions);
                }
                catch (JsonException)
                {
                    return Results.Fail<IReadOnlyList<CustomerDataDto>>("Malformed JSON content.");
                }

                IReadOnlyList<CustomerDataDto> mapped = [.. (contracts ?? []).Select(ToDto)];
                return Results.Success(mapped);
            }
        }

        private static string? FindDuplicateProperty(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (!seen.Add(property.Name))
                    {
                        return $"Duplicate property '{property.Name}' is not allowed.";
                    }

                    string? nested = FindDuplicateProperty(property.Value);
                    if (nested is not null)
                    {
                        return nested;
                    }
                }

                return null;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in element.EnumerateArray())
                {
                    string? nested = FindDuplicateProperty(item);
                    if (nested is not null)
                    {
                        return nested;
                    }
                }
            }

            return null;
        }

        private static CustomerJsonContract ToContract(CustomerDataDto dto) => new()
        {
            CustomerNumber = dto.CustomerNumber,
            LastName = dto.LastName,
            SurName = dto.SurName,
            Email = dto.Email,
            Website = dto.Website,
            Address = dto.Address is null ? null : new CustomerAddressJsonContract
            {
                ValidFrom = dto.Address.ValidFrom,
                Street = dto.Address.Street,
                HouseNumber = dto.Address.HouseNumber,
                PostalCode = dto.Address.PostalCode,
                City = dto.Address.City,
                CountryCode = dto.Address.CountryCode,
            },
        };

        private static CustomerDataDto ToDto(CustomerJsonContract contract) => new(
            contract.CustomerNumber,
            contract.LastName,
            contract.SurName,
            contract.Email,
            contract.Website,
            contract.Address is null ? null : new CustomerAddressDataDto(
                contract.Address.ValidFrom,
                contract.Address.Street,
                contract.Address.HouseNumber,
                contract.Address.PostalCode,
                contract.Address.City,
                contract.Address.CountryCode));

        private static JsonSerializerOptions CreateSerializerOptions(int maxDepth)
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                MaxDepth = maxDepth,
            };

            serializerOptions.Converters.Add(new DateOnlyJsonConverter());
            return serializerOptions;
        }
    }
}
