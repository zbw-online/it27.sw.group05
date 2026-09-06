using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.PreviewAddressForDate
{
    public sealed class PreviewAddressForDateUseCase(ICustomerQueryRepository customerQueryRepository) : IPreviewAddressForDateUseCase
    {
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;

        public async Task<Result<PreviewAddressForDateResponse>> ExecuteAsync(
            PreviewAddressForDateQuery query,
            CancellationToken cancellationToken = default)
        {
            Customer? customer = await _customerQueryRepository.GetByIdAsync(
                new CustomerId(query.CustomerId),
                cancellationToken);

            if (customer is null)
            {
                return Results.Fail<PreviewAddressForDateResponse>("Customer was not found.");
            }

            CustomerAddress? address = customer.AddressAt(query.Date);

            PreviewAddressForDateResponse response = address is null
                ? new PreviewAddressForDateResponse(false, null, null, null, null, null, null, null)
                : new PreviewAddressForDateResponse(
                    true,
                    address.Street,
                    address.HouseNumber,
                    address.PostalCode,
                    address.City,
                    address.CountryCode,
                    address.ValidFrom,
                    address.ValidTo);

            return Results.Success(response);
        }
    }
}
