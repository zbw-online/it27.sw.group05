using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.GetCustomerForEdit
{
    public sealed class GetCustomerForEditUseCase(ICustomerQueryRepository customerQueryRepository) : IGetCustomerForEditUseCase
    {
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;

        public async Task<Result<GetCustomerForEditResponse>> ExecuteAsync(
            GetCustomerForEditQuery query,
            CancellationToken cancellationToken = default)
        {
            Customer? customer = await _customerQueryRepository.GetByIdAsync(
                new CustomerId(query.CustomerId),
                cancellationToken);

            if (customer is null)
            {
                return Results.Fail<GetCustomerForEditResponse>("Customer was not found.");
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            CustomerAddress? address = customer.AddressAt(today) ?? customer.Addresses.OrderByDescending(a => a.ValidFrom).FirstOrDefault();

            return Results.Success(new GetCustomerForEditResponse(
                customer.Id.Value,
                customer.CustomerNumber.Value,
                customer.LastName,
                customer.SurName,
                customer.Email.Value,
                customer.Website,
                address?.ValidFrom ?? today,
                address?.Street ?? string.Empty,
                address?.HouseNumber ?? string.Empty,
                address?.PostalCode ?? string.Empty,
                address?.City ?? string.Empty,
                address?.CountryCode ?? "CH"));
        }
    }
}
