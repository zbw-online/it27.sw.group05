using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Persistence.Customers.Command;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command
{
    public sealed class CustomerCommandRepository(OrderManagementDbContext context) : ICustomerCommandRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<Customer?> GetByIdAsync(
            CustomerId id,
            CancellationToken cancellationToken = default)
            => await _context.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public void Add(Customer customer)
            => _context.Customers.Add(customer);

        public void Update(Customer customer)
            => _context.Customers.Update(customer);

        public void Remove(Customer customer)
            => _context.Customers.Remove(customer);
    }
}
