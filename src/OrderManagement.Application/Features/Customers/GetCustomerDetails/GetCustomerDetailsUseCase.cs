using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.GetCustomerDetails
{
    public sealed class GetCustomerDetailsUseCase(ICustomerQueryRepository customerQueryRepository) : IGetCustomerDetailsUseCase
    {
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;

        public async Task<Result<GetCustomerDetailsResponse>> ExecuteAsync(
            GetCustomerDetailsQuery query,
            CancellationToken cancellationToken = default)
        {
            Customer? customer = await _customerQueryRepository.GetByIdAsync(
                new CustomerId(query.CustomerId),
                cancellationToken);

            if (customer is null)
            {
                return Results.Fail<GetCustomerDetailsResponse>("Customer was not found.");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);

            IReadOnlyList<CustomerAddressDto> addresses = [.. customer.Addresses
                .OrderBy(a => a.ValidFrom)
                .Select(a => MapAddress(a, today))];

            CustomerAddressDto? currentAddress = addresses.FirstOrDefault(a => a.Status == "Current");

            IReadOnlyList<CustomerAddressDto> previousAddresses = [.. addresses
                .Where(a => a.Status == "Previous")
                .OrderByDescending(a => a.ValidFrom)];

            IReadOnlyList<CustomerAddressDto> futureAddresses = [.. addresses
                .Where(a => a.Status == "Future")
                .OrderBy(a => a.ValidFrom)];

            var response = new GetCustomerDetailsResponse(
                customer.Id.Value,
                customer.CustomerNumber.Value,
                $"{customer.LastName} {customer.SurName}",
                customer.Email.Value,
                customer.Website,
                currentAddress,
                previousAddresses,
                futureAddresses);

            return Results.Success(response);
        }

        private static CustomerAddressDto MapAddress(CustomerAddress address, DateOnly today)
        {
            string status = address.ValidFrom > today
                ? "Future"
                : address.ValidTo is not null && address.ValidTo.Value < today
                    ? "Previous"
                    : "Current";

            return new CustomerAddressDto(
                address.Id,
                address.ValidFrom,
                address.ValidTo,
                address.Street,
                address.HouseNumber,
                address.PostalCode,
                address.City,
                address.CountryCode,
                status);
        }
    }
}
