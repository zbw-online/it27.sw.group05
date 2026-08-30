using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.GetCustomersWithoutCurrentAddress
{
    public sealed class GetCustomersWithoutCurrentAddressUseCase(
        ICustomerQueryRepository customerQueryRepository) : IGetCustomersWithoutCurrentAddressUseCase
    {
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;

        public async Task<Result<IReadOnlyList<CustomerWithoutAddressDto>>> ExecuteAsync(
            GetCustomersWithoutCurrentAddressQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Customer> customers = await _customerQueryRepository.GetListAsync(cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.Today);

            IReadOnlyList<CustomerWithoutAddressDto> result = [.. customers
                .Where(c => c.AddressAt(today) is null)
                .Select(c => new CustomerWithoutAddressDto(c.Id.Value, c.CustomerNumber.Value, $"{c.LastName} {c.SurName}"))];

            return Results.Success(result);
        }
    }
}
