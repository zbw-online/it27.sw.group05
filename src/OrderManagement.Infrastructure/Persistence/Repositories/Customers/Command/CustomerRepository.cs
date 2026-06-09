
using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Features.Customers.CreateCustomer;
using OrderManagement.Domain.Customers;


namespace OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command
{
    public sealed class CustomerRepository(OrderManagementDbContext context) : ICustomerRepository
    {
        private readonly OrderManagementDbContext _context = context;

        public void Add(Customer customer) => _ = _context.Customers.Add(customer);

        public async Task<bool> ExistsWithCustomerNumberAsync(
            string customerNumber,
            CancellationToken cancellationToken = default)
        {
            string normalized = customerNumber.Trim().ToUpperInvariant();

            return await _context.Customers
                .AsNoTracking()
                .AnyAsync(c => c.CustomerNumber.Value == normalized, cancellationToken);
        }

        public async Task<bool> ExistsWithEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            string normalized = email.Trim().ToLowerInvariant();

            return await _context.Customers
                .AsNoTracking()
                .AnyAsync(c => c.Email.Value == normalized, cancellationToken);
        }
    }
}
