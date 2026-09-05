using OrderManagement.Application.Features.Customers.DataExchange.Shared;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Abstractions.Serialization
{
    public interface ICustomerDataSerializer
    {
        CustomerDataFormat Format { get; }
        string FileExtension { get; }
        string MediaType { get; }

        Task SerializeAsync(
            IReadOnlyList<CustomerDataDto> customers,
            Stream destination,
            CancellationToken cancellationToken = default);

        Task<Result<IReadOnlyList<CustomerDataDto>>> DeserializeAsync(
            Stream source,
            CancellationToken cancellationToken = default);
    }
}
