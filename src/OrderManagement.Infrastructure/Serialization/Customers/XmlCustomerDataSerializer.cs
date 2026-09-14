using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.Extensions.Options;

using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Infrastructure.Serialization.Customers.Xml;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Serialization.Customers
{
    public sealed class XmlCustomerDataSerializer(IOptions<CustomerDataExchangeOptions> options) : ICustomerDataSerializer
    {
        private const string DateFormat = "yyyy-MM-dd";

        private readonly long _maxDocumentCharacters = options.Value.MaxXmlCharacters;

        public CustomerDataFormat Format => CustomerDataFormat.Xml;
        public string FileExtension => "xml";
        public string MediaType => "application/xml";

        public async Task SerializeAsync(
            IReadOnlyList<CustomerDataDto> customers,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            var document = new XmlCustomerDataDocument
            {
                Kunde = [.. customers
                    .OrderBy(c => c.CustomerNumber, StringComparer.Ordinal)
                    .Select(ToEntry)],
            };

            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false),
                Async = true,
            };

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            var serializer = new XmlSerializer(typeof(XmlCustomerDataDocument));
            await using var writer = XmlWriter.Create(destination, settings);
            serializer.Serialize(writer, document, namespaces);
            await writer.FlushAsync();
        }

        public Task<Result<IReadOnlyList<CustomerDataDto>>> DeserializeAsync(
            Stream source,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var buffer = new MemoryStream();
            source.CopyTo(buffer);

            if (buffer.Length == 0)
            {
                return Task.FromResult(Results.Fail<IReadOnlyList<CustomerDataDto>>("The file is empty."));
            }

            buffer.Position = 0;

            var readerSettings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = _maxDocumentCharacters,
                IgnoreWhitespace = true,
                IgnoreComments = true,
            };

            var unexpectedNodes = new List<string>();

            try
            {
                using var reader = XmlReader.Create(buffer, readerSettings);

                var serializer = new XmlSerializer(typeof(XmlCustomerDataDocument));
                serializer.UnknownElement += (_, e) => unexpectedNodes.Add($"element '{e.Element.Name}'");
                serializer.UnknownAttribute += (_, e) => unexpectedNodes.Add($"attribute '{e.Attr.Name}'");

                XmlCustomerDataDocument document = (XmlCustomerDataDocument?)serializer.Deserialize(reader)
                    ?? new XmlCustomerDataDocument();

                if (unexpectedNodes.Count > 0)
                {
                    return Task.FromResult(Results.Fail<IReadOnlyList<CustomerDataDto>>(
                        $"Unexpected XML content: {string.Join(", ", unexpectedNodes)}."));
                }

                var mapped = new List<CustomerDataDto>();
                foreach (XmlCustomerDataEntry entry in document.Kunde)
                {
                    Result<CustomerDataDto> mapResult = ToDto(entry);
                    if (!mapResult.IsSuccess)
                    {
                        return Task.FromResult(Results.Fail<IReadOnlyList<CustomerDataDto>>(mapResult.Error!));
                    }

                    mapped.Add(mapResult.Value!);
                }

                return Task.FromResult(Results.Success<IReadOnlyList<CustomerDataDto>>(mapped));
            }
            catch (XmlException)
            {
                return Task.FromResult(Results.Fail<IReadOnlyList<CustomerDataDto>>("Malformed XML content."));
            }
            catch (InvalidOperationException)
            {
                return Task.FromResult(Results.Fail<IReadOnlyList<CustomerDataDto>>("Malformed XML content."));
            }
        }

        private static XmlCustomerDataEntry ToEntry(CustomerDataDto dto) => new()
        {
            CustomerNumber = dto.CustomerNumber,
            LastName = dto.LastName,
            SurName = dto.SurName,
            Email = dto.Email,
            Website = dto.Website,
            Address = dto.Address is null ? null : new XmlCustomerAddressEntry
            {
                ValidFrom = dto.Address.ValidFrom.ToString(DateFormat, CultureInfo.InvariantCulture),
                Street = dto.Address.Street,
                HouseNumber = dto.Address.HouseNumber,
                PostalCode = dto.Address.PostalCode,
                City = dto.Address.City,
                CountryCode = dto.Address.CountryCode,
            },
        };

        private static Result<CustomerDataDto> ToDto(XmlCustomerDataEntry entry)
        {
            CustomerAddressDataDto? address = null;

            if (entry.Address is not null)
            {
                if (!DateOnly.TryParseExact(entry.Address.ValidFrom, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly validFrom))
                {
                    return Results.Fail<CustomerDataDto>("Date value must use the format yyyy-MM-dd.");
                }

                address = new CustomerAddressDataDto(
                    validFrom,
                    entry.Address.Street,
                    entry.Address.HouseNumber,
                    entry.Address.PostalCode,
                    entry.Address.City,
                    entry.Address.CountryCode);
            }

            return Results.Success(new CustomerDataDto(
                entry.CustomerNumber,
                entry.LastName,
                entry.SurName,
                entry.Email,
                entry.Website,
                address));
        }
    }
}
