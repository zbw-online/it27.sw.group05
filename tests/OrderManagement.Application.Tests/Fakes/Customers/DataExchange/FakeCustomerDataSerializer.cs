using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Fakes.Customers.DataExchange
{
    public sealed class FakeCustomerDataSerializer(CustomerDataFormat format) : ICustomerDataSerializer
    {
        public CustomerDataFormat Format => format;
        public string FileExtension => format.ToString().ToLowerInvariant();
        public string MediaType => "application/x-fake";

        public Result<IReadOnlyList<CustomerDataDto>> DeserializeResult { get; set; }
            = Results.Success<IReadOnlyList<CustomerDataDto>>([]);

        public List<CustomerDataDto> SerializedCustomers { get; } = [];

        public Task SerializeAsync(
            IReadOnlyList<CustomerDataDto> customers,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            SerializedCustomers.AddRange(customers);
            return Task.CompletedTask;
        }

        public Task<Result<IReadOnlyList<CustomerDataDto>>> DeserializeAsync(
            Stream source,
            CancellationToken cancellationToken = default)
            => Task.FromResult(DeserializeResult);
    }
}
