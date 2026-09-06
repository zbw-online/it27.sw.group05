using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query
{
    public sealed class CustomerTemporalQueryRepository(OrderManagementDbContext context) : ICustomerTemporalQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<IReadOnlyList<CustomerDataDto>> GetCustomersAsOfAsync(
            DateTime asOfUtc,
            DateOnly asOfBusinessDate,
            CancellationToken cancellationToken = default)
        {
            List<Customer> customers = await _context.Customers
                .TemporalAsOf(asOfUtc)
                .AsNoTracking()
                .OrderBy(c => c.CustomerNumber)
                .ToListAsync(cancellationToken);

            var addressRows = await _context.Set<CustomerAddress>()
                .TemporalAsOf(asOfUtc)
                .AsNoTracking()
                .Select(a => new
                {
                    CustomerId = EF.Property<CustomerId>(a, "CustomerId"),
                    a.ValidFrom,
                    a.ValidTo,
                    a.Street,
                    a.HouseNumber,
                    a.PostalCode,
                    a.City,
                    a.CountryCode,
                })
                .ToListAsync(cancellationToken);

            ILookup<CustomerId, CustomerAddressDataDto> addressesByCustomer = addressRows
                .Where(a => a.ValidFrom <= asOfBusinessDate && (a.ValidTo is null || a.ValidTo >= asOfBusinessDate))
                .ToLookup(
                    a => a.CustomerId,
                    a => new CustomerAddressDataDto(a.ValidFrom, a.Street, a.HouseNumber, a.PostalCode, a.City, a.CountryCode));

            return [.. customers.Select(customer => new CustomerDataDto(
                customer.CustomerNumber.Value,
                customer.LastName,
                customer.SurName,
                customer.Email.Value,
                customer.Website,
                addressesByCustomer[customer.Id]
                    .OrderByDescending(a => a.ValidFrom)
                    .FirstOrDefault()))];
        }
    }
}
