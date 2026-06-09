using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query
{
    public sealed class CustomerQueryRepository(OrderManagementDbContext context) : ICustomerQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<Customer?> GetByIdAsync(
            CustomerId id,
            CancellationToken ct = default)
            => await _context.Customers
                .Include(c => c.Addresses)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<IReadOnlyList<Customer>> GetListAsync(
            CancellationToken ct = default)
            => await _context.Customers
                .Include(c => c.Addresses)
                .AsNoTracking()
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.SurName)
                .ToListAsync(ct);

        public async Task<Customer?> GetByCustomerNumberAsync(
            CustomerNumber number,
            CancellationToken cancellationToken = default)
            => await _context.Customers
                .Include(c => c.Addresses)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerNumber == number, cancellationToken);

        public async Task<Customer?> GetByEmailAsync(
            Email email,
            CancellationToken cancellationToken = default)
            => await _context.Customers
                .Include(c => c.Addresses)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

        public async Task<IReadOnlyList<Customer>> SearchByNameOrNumberAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            string term = (searchTerm ?? string.Empty).Trim();

            if (term.Length == 0)
            {
                return await GetListAsync(cancellationToken);
            }

            // Intentionally filter in memory after loading the small school-project data set.
            // This avoids brittle EF translation of ValueObject.Value members.
            IReadOnlyList<Customer> customers = await GetListAsync(cancellationToken);

            return [.. customers
                .Where(c =>
                    c.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.SurName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.CustomerNumber.Value.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.SurName)];
        }
    }
}
