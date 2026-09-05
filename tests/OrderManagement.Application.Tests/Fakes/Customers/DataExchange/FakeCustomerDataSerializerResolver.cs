using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.DataExchange.Shared;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Fakes.Customers.DataExchange
{
    public sealed class FakeCustomerDataSerializerResolver(params ICustomerDataSerializer[] serializers)
        : ICustomerDataSerializerResolver
    {
        public Result<ICustomerDataSerializer> Resolve(CustomerDataFormat format)
        {
            ICustomerDataSerializer? serializer = serializers.FirstOrDefault(s => s.Format == format);

            return serializer is null
                ? Results.Fail<ICustomerDataSerializer>($"Unsupported customer data format: {format}.")
                : Results.Success(serializer);
        }
    }
}
