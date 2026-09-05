using OrderManagement.Application.Features.Customers.DataExchange.Shared;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Abstractions.Serialization
{
    public interface ICustomerDataSerializerResolver
    {
        Result<ICustomerDataSerializer> Resolve(CustomerDataFormat format);
    }
}
