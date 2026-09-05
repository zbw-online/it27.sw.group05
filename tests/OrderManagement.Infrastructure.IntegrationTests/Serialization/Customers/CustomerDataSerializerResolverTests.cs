using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.DataExchange.Shared;
using OrderManagement.Infrastructure.Serialization.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Serialization.Customers
{
    [TestClass]
    public sealed class CustomerDataSerializerResolverTests
    {
        private static IOptions<CustomerDataExchangeOptions> DefaultOptions()
            => Options.Create(new CustomerDataExchangeOptions());

        private static CustomerDataSerializerResolver CreateResolver()
            => new([new JsonCustomerDataSerializer(DefaultOptions()), new XmlCustomerDataSerializer(DefaultOptions())]);

        [TestMethod]
        public void Resolve_WithJsonFormat_ShouldReturnJsonSerializer()
        {
            CustomerDataSerializerResolver resolver = CreateResolver();

            Result<ICustomerDataSerializer> result = resolver.Resolve(CustomerDataFormat.Json);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsInstanceOfType<JsonCustomerDataSerializer>(result.Value);
        }

        [TestMethod]
        public void Resolve_WithXmlFormat_ShouldReturnXmlSerializer()
        {
            CustomerDataSerializerResolver resolver = CreateResolver();

            Result<ICustomerDataSerializer> result = resolver.Resolve(CustomerDataFormat.Xml);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsInstanceOfType<XmlCustomerDataSerializer>(result.Value);
        }

        [TestMethod]
        public void Resolve_WithUnsupportedFormat_ShouldFail()
        {
            var resolver = new CustomerDataSerializerResolver([new JsonCustomerDataSerializer(DefaultOptions())]);

            Result<ICustomerDataSerializer> result = resolver.Resolve(CustomerDataFormat.Xml);

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
