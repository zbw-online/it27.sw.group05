using System.Text;

using Microsoft.Extensions.Options;

using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Infrastructure.Serialization.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Serialization.Customers
{
    [TestClass]
    public sealed class JsonCustomerDataSerializerTests
    {
        private static IOptions<CustomerDataExchangeOptions> DefaultOptions()
            => Options.Create(new CustomerDataExchangeOptions());

        private static CustomerDataDto SampleCustomer(
            string customerNumber = "CU00001",
            string? website = "www.example.ch",
            CustomerAddressDataDto? address = null)
            => new(
                customerNumber,
                "Muster",
                "Hans",
                "hans.muster@example.ch",
                website,
                address ?? new CustomerAddressDataDto(
                    new DateOnly(2026, 1, 1),
                    "Musterstrasse",
                    "10",
                    "8000",
                    "Zürich",
                    "CH"));

        [TestMethod]
        public void Format_ShouldExposeJsonIdentifiers()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());

            Assert.AreEqual(CustomerDataFormat.Json, serializer.Format);
            Assert.AreEqual("json", serializer.FileExtension);
            Assert.AreEqual("application/json", serializer.MediaType);
        }

        [TestMethod]
        public async Task SerializeAsync_WithSingleCustomer_ShouldWriteDeterministicCamelCaseJson()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            await serializer.SerializeAsync([SampleCustomer()], stream);

            string json = Encoding.UTF8.GetString(stream.ToArray());

            StringAssert.Contains(json, "\"customerNumber\": \"CU00001\"");
            StringAssert.Contains(json, "\"lastName\": \"Muster\"");
            StringAssert.Contains(json, "\"surName\": \"Hans\"");
            StringAssert.Contains(json, "\"email\": \"hans.muster@example.ch\"");
            StringAssert.Contains(json, "\"website\": \"www.example.ch\"");
            StringAssert.Contains(json, "\"validFrom\": \"2026-01-01\"");
            StringAssert.Contains(json, "\"countryCode\": \"CH\"");
        }

        [TestMethod]
        public async Task SerializeAsync_WithUnorderedCustomers_ShouldWriteThemOrderedByCustomerNumber()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            await serializer.SerializeAsync(
                [SampleCustomer("CU00003"), SampleCustomer("CU00001"), SampleCustomer("CU00002")],
                stream);

            string json = Encoding.UTF8.GetString(stream.ToArray());

            int i1 = json.IndexOf("CU00001", StringComparison.Ordinal);
            int i2 = json.IndexOf("CU00002", StringComparison.Ordinal);
            int i3 = json.IndexOf("CU00003", StringComparison.Ordinal);

            Assert.IsTrue(i1 < i2 && i2 < i3, "Customers must be written in ascending customer number order.");
        }

        [TestMethod]
        public async Task SerializeAsync_WithNullWebsiteAndNullAddress_ShouldWriteNullValues()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            await serializer.SerializeAsync(
                [new CustomerDataDto("CU00001", "Müller", "Änne", "a@b.ch", null, null)],
                stream);

            string json = Encoding.UTF8.GetString(stream.ToArray());

            StringAssert.Contains(json, "\"website\": null");
            StringAssert.Contains(json, "\"address\": null");
        }

        [TestMethod]
        public async Task SerializeAsync_WithUnicodeAndSpecialCharacters_ShouldRoundTripCorrectly()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            CustomerDataDto original = new(
                "CU00009",
                "Müller \"Quote\"",
                "Ängström",
                "a@b.ch",
                null,
                new CustomerAddressDataDto(new DateOnly(2026, 1, 1), "Bäckerstraße", "1a", "8000", "Zürich", "CH"));

            await serializer.SerializeAsync([original], stream);
            stream.Position = 0;

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(original, result.Value![0]);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithValidDocument_ShouldReturnMappedCustomers()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [
                  {
                    "customerNumber": "CU00001",
                    "lastName": "Muster",
                    "surName": "Hans",
                    "email": "hans.muster@example.ch",
                    "website": "www.example.ch",
                    "address": {
                      "validFrom": "2026-01-01",
                      "street": "Musterstrasse",
                      "houseNumber": "10",
                      "postalCode": "8000",
                      "city": "Zürich",
                      "countryCode": "CH"
                    }
                  }
                ]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            CustomerDataDto dto = result.Value[0];
            Assert.AreEqual("CU00001", dto.CustomerNumber);
            Assert.AreEqual("Muster", dto.LastName);
            Assert.AreEqual("Hans", dto.SurName);
            Assert.AreEqual("hans.muster@example.ch", dto.Email);
            Assert.AreEqual("www.example.ch", dto.Website);
            Assert.IsNotNull(dto.Address);
            Assert.AreEqual(new DateOnly(2026, 1, 1), dto.Address!.ValidFrom);
            Assert.AreEqual("Zürich", dto.Address.City);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithNullAddress_ShouldReturnCustomerWithNullAddress()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":"CU00001","lastName":"Muster","surName":"Hans","email":"a@b.ch","website":null,"address":null}]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsNull(result.Value![0].Address);
            Assert.IsNull(result.Value[0].Website);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithMalformedJson_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{ not valid json ]"));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithMissingRootArray_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """{"customerNumber":"CU00001"}""";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithNullCustomerEntry_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":"CU00001","lastName":"A","surName":"B","email":"a@b.ch"}, null]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithUnexpectedProperty_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":"CU00001","lastName":"A","surName":"B","email":"a@b.ch","unexpected":"x"}]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithUnexpectedNestedAddressProperty_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":"CU00001","lastName":"A","surName":"B","email":"a@b.ch","address":{"validFrom":"2026-01-01","street":"S","houseNumber":"1","postalCode":"8000","city":"Z","countryCode":"CH","extra":"x"}}]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithDuplicateProperty_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":"CU00001","customerNumber":"CU00002","lastName":"A","surName":"B","email":"a@b.ch"}]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithDuplicateNestedAddressProperty_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":"CU00001","lastName":"A","surName":"B","email":"a@b.ch","address":{"validFrom":"2026-01-01","validFrom":"2026-02-01","street":"S","houseNumber":"1","postalCode":"8000","city":"Z","countryCode":"CH"}}]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithWrongTypeForString_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":12345,"lastName":"A","surName":"B","email":"a@b.ch"}]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithMalformedDate_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":"CU00001","lastName":"A","surName":"B","email":"a@b.ch","address":{"validFrom":"01/01/2026","street":"S","houseNumber":"1","postalCode":"8000","city":"Z","countryCode":"CH"}}]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithExcessiveNestingDepth_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            var builder = new StringBuilder();
            for (int i = 0; i < 128; i++)
            {
                _ = builder.Append('[');
            }

            for (int i = 0; i < 128; i++)
            {
                _ = builder.Append(']');
            }

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithConfiguredMaxDepthExceededByShallowDocument_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(Options.Create(new CustomerDataExchangeOptions { MaxJsonDepth = 1 }));
            const string json = /*lang=json,strict*/ """
                [{"customerNumber":"CU00001","lastName":"A","surName":"B","email":"a@b.ch","address":{"validFrom":"2026-01-01","street":"S","houseNumber":"1","postalCode":"8000","city":"Z","countryCode":"CH"}}]
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithEmptyStream_ShouldFail()
        {
            var serializer = new JsonCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
