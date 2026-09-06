using System.Text;

using Microsoft.Extensions.Options;

using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Infrastructure.Serialization.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Serialization.Customers
{
    [TestClass]
    public sealed class XmlCustomerDataSerializerTests
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
        public void Format_ShouldExposeXmlIdentifiers()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());

            Assert.AreEqual(CustomerDataFormat.Xml, serializer.Format);
            Assert.AreEqual("xml", serializer.FileExtension);
            Assert.AreEqual("application/xml", serializer.MediaType);
        }

        [TestMethod]
        public async Task SerializeAsync_WithSingleCustomer_ShouldWriteExpectedXmlStructure()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            await serializer.SerializeAsync([SampleCustomer()], stream);

            string xml = Encoding.UTF8.GetString(stream.ToArray());

            StringAssert.Contains(xml, "<Kunden");
            StringAssert.Contains(xml, "<Kunde CustomerNumber=\"CU00001\"");
            StringAssert.Contains(xml, "<LastName>Muster</LastName>");
            StringAssert.Contains(xml, "<SurName>Hans</SurName>");
            StringAssert.Contains(xml, "<Email>hans.muster@example.ch</Email>");
            StringAssert.Contains(xml, "<Website>www.example.ch</Website>");
            StringAssert.Contains(xml, "<ValidFrom>2026-01-01</ValidFrom>");
            StringAssert.Contains(xml, "<CountryCode>CH</CountryCode>");
        }

        [TestMethod]
        public async Task SerializeAsync_WithUnorderedCustomers_ShouldWriteThemOrderedByCustomerNumber()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            await serializer.SerializeAsync(
                [SampleCustomer("CU00003"), SampleCustomer("CU00001"), SampleCustomer("CU00002")],
                stream);

            string xml = Encoding.UTF8.GetString(stream.ToArray());

            int i1 = xml.IndexOf("CU00001", StringComparison.Ordinal);
            int i2 = xml.IndexOf("CU00002", StringComparison.Ordinal);
            int i3 = xml.IndexOf("CU00003", StringComparison.Ordinal);

            Assert.IsTrue(i1 < i2 && i2 < i3, "Customers must be written in ascending customer number order.");
        }

        [TestMethod]
        public async Task SerializeAsync_WithNullWebsiteAndNullAddress_ShouldOmitOptionalElements()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            await serializer.SerializeAsync(
                [new CustomerDataDto("CU00001", "Müller", "Änne", "a@b.ch", null, null)],
                stream);

            string xml = Encoding.UTF8.GetString(stream.ToArray());

            Assert.IsFalse(xml.Contains("<Website>", StringComparison.Ordinal));
            Assert.IsFalse(xml.Contains("<Address>", StringComparison.Ordinal));
        }

        [TestMethod]
        public async Task SerializeAsync_WithUnicodeAndSpecialCharacters_ShouldRoundTripCorrectly()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            CustomerDataDto original = new(
                "CU00009",
                "Müller & <Sohn>",
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
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <Kunden>
                  <Kunde CustomerNumber="CU00001">
                    <LastName>Muster</LastName>
                    <SurName>Hans</SurName>
                    <Email>hans.muster@example.ch</Email>
                    <Website>www.example.ch</Website>
                    <Address>
                      <ValidFrom>2026-01-01</ValidFrom>
                      <Street>Musterstrasse</Street>
                      <HouseNumber>10</HouseNumber>
                      <PostalCode>8000</PostalCode>
                      <City>Zürich</City>
                      <CountryCode>CH</CountryCode>
                    </Address>
                  </Kunde>
                </Kunden>
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            CustomerDataDto dto = result.Value[0];
            Assert.AreEqual("CU00001", dto.CustomerNumber);
            Assert.AreEqual("hans.muster@example.ch", dto.Email);
            Assert.IsNotNull(dto.Address);
            Assert.AreEqual(new DateOnly(2026, 1, 1), dto.Address!.ValidFrom);
            Assert.AreEqual("Zürich", dto.Address.City);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithoutOptionalWebsiteAndAddress_ShouldReturnNulls()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """
                <Kunden>
                  <Kunde CustomerNumber="CU00001">
                    <LastName>A</LastName>
                    <SurName>B</SurName>
                    <Email>a@b.ch</Email>
                  </Kunde>
                </Kunden>
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsNull(result.Value![0].Website);
            Assert.IsNull(result.Value[0].Address);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithMalformedXml_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<Kunden><Kunde></Kunden>"));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithWrongRootElement_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """<Kundenliste><Kunde CustomerNumber="CU00001"/></Kundenliste>""";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithUnexpectedElement_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """
                <Kunden>
                  <Kunde CustomerNumber="CU00001">
                    <LastName>A</LastName>
                    <SurName>B</SurName>
                    <Email>a@b.ch</Email>
                    <Unexpected>x</Unexpected>
                  </Kunde>
                </Kunden>
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithUnexpectedAttribute_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """
                <Kunden>
                  <Kunde CustomerNumber="CU00001" Extra="x">
                    <LastName>A</LastName>
                    <SurName>B</SurName>
                    <Email>a@b.ch</Email>
                  </Kunde>
                </Kunden>
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithMissingRootCollection_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """<Kunde CustomerNumber="CU00001"/>""";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithMalformedDate_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """
                <Kunden>
                  <Kunde CustomerNumber="CU00001">
                    <LastName>A</LastName>
                    <SurName>B</SurName>
                    <Email>a@b.ch</Email>
                    <Address>
                      <ValidFrom>01/01/2026</ValidFrom>
                      <Street>S</Street>
                      <HouseNumber>1</HouseNumber>
                      <PostalCode>8000</PostalCode>
                      <City>Z</City>
                      <CountryCode>CH</CountryCode>
                    </Address>
                  </Kunde>
                </Kunden>
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithDoctypeDeclaration_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """
                <?xml version="1.0"?>
                <!DOCTYPE Kunden [<!ENTITY foo "bar">]>
                <Kunden>
                  <Kunde CustomerNumber="CU00001">
                    <LastName>&foo;</LastName>
                    <SurName>B</SurName>
                    <Email>a@b.ch</Email>
                  </Kunde>
                </Kunden>
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithExternalEntity_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            const string xml = """
                <?xml version="1.0"?>
                <!DOCTYPE Kunden [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
                <Kunden>
                  <Kunde CustomerNumber="CU00001">
                    <LastName>&xxe;</LastName>
                    <SurName>B</SurName>
                    <Email>a@b.ch</Email>
                  </Kunde>
                </Kunden>
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_WithEmptyStream_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(DefaultOptions());
            using var stream = new MemoryStream();

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeserializeAsync_ExceedingMaxCharacters_ShouldFail()
        {
            var serializer = new XmlCustomerDataSerializer(Options.Create(new CustomerDataExchangeOptions { MaxXmlCharacters = 200 }));
            const string xml = """
                <Kunden>
                  <Kunde CustomerNumber="CU00001">
                    <LastName>ThisIsAVeryLongLastNameThatShouldExceedTheConfiguredMaximumCharacterLimitForThisXmlDocumentOnPurpose</LastName>
                    <SurName>B</SurName>
                    <Email>a@b.ch</Email>
                  </Kunde>
                </Kunden>
                """;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            Result<IReadOnlyList<CustomerDataDto>> result = await serializer.DeserializeAsync(stream);

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
