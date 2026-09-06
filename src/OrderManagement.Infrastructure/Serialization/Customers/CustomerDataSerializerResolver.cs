using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Serialization.Customers
{
    public sealed class CustomerDataSerializerResolver(IEnumerable<ICustomerDataSerializer> serializers) : ICustomerDataSerializerResolver
    {
        private readonly IReadOnlyList<ICustomerDataSerializer> _serializers = [.. serializers];

        public Result<ICustomerDataSerializer> Resolve(CustomerDataFormat format)
        {
            ICustomerDataSerializer? serializer = _serializers.FirstOrDefault(s => s.Format == format);

            return serializer is null
                ? Results.Fail<ICustomerDataSerializer>($"Unsupported customer data format: {format}.")
                : Results.Success(serializer);
        }
    }
}
