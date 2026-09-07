using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Domain.Customers;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query
{
    public sealed class CustomerCurrentQueryRepository(OrderManagementDbContext context) : ICustomerCurrentQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<IReadOnlyList<CustomerDataDto>> GetAllCurrentAsync(
            DateOnly asOfBusinessDate,
            CancellationToken cancellationToken = default)
        {
            List<Customer> customers = await _context.Customers
                .AsNoTracking()
                .Include(c => c.Addresses)
                .OrderBy(c => c.CustomerNumber)
                .ToListAsync(cancellationToken);

            return [.. customers.Select(customer =>
            {
                CustomerAddress? address = customer.AddressAt(asOfBusinessDate);

                return new CustomerDataDto(
                    customer.CustomerNumber.Value,
                    customer.LastName,
                    customer.SurName,
                    customer.Email.Value,
                    customer.Website,
                    address is null
                        ? null
                        : new CustomerAddressDataDto(address.ValidFrom, address.Street, address.HouseNumber, address.PostalCode, address.City, address.CountryCode));
            })];
        }
    }
}
