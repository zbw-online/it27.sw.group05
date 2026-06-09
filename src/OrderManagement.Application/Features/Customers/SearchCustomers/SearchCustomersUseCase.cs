using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Application.Features.Customers.Shared;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.SearchCustomers
{
    public sealed class SearchCustomersUseCase(ICustomerQueryRepository customerQueryRepository) : ISearchCustomersUseCase
    {
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;

        public async Task<Result<IReadOnlyList<CustomerListItemDto>>> ExecuteAsync(
            SearchCustomersQuery query,
            CancellationToken cancellationToken = default)
        {
            string term = (query.SearchTerm ?? string.Empty).Trim();

            IReadOnlyList<Customer> customers = term.Length == 0
                ? await _customerQueryRepository.GetListAsync(cancellationToken)
                : await _customerQueryRepository.SearchByNameOrNumberAsync(term, cancellationToken);

            IReadOnlyList<CustomerListItemDto> result = [.. customers
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.SurName)
                .Select(Map)];

            return Results.Success(result);
        }

        private static CustomerListItemDto Map(Customer customer)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            CustomerAddress? address = customer.AddressAt(today) ?? customer.Addresses.OrderByDescending(a => a.ValidFrom).FirstOrDefault();

            return new CustomerListItemDto(
                customer.Id.Value,
                customer.CustomerNumber.Value,
                $"{customer.LastName} {customer.SurName}",
                customer.Email.Value,
                customer.Website,
                address?.Street ?? string.Empty,
                address?.HouseNumber ?? string.Empty,
                address?.PostalCode ?? string.Empty,
                address?.City ?? string.Empty,
                address?.CountryCode ?? string.Empty);
        }
    }
}
