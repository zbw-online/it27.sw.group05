using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;


namespace OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query
{
    public class CustomerQueryRepository(OrderManagementDbContext context) : ICustomerQueryRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        public async Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
            => await _context.Customers
            .Include(c => c.Addresses)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<IReadOnlyList<Customer>> GetListAsync(CancellationToken ct = default)
            => await _context.Customers
            .Include(c => c.Addresses)
            .AsNoTracking()
            .ToListAsync(ct);

        public async Task<Customer?> GetByCustomerNumberAsync(CustomerNumber number, CancellationToken ct = default)
            => await _context.Customers
            .Include(c => c.Addresses)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerNumber == number, ct);

        public async Task<Customer?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => await _context.Customers
            .Include(c => c.Addresses)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == email, ct);

        public async Task<IReadOnlyList<Customer>> SearchByNameAsync(string searchTerm, CancellationToken ct = default)
        {
            string term = (searchTerm ?? string.Empty).Trim();
            return term.Length == 0
                ? []
                : (IReadOnlyList<Customer>)await _context.Customers
                .Include(c => c.Addresses)
                .AsNoTracking()
                .Where(c =>
                    EF.Functions.Like(c.LastName, $"%{term}%") ||
                    EF.Functions.Like(c.SurName, $"%{term}%"))
                .ToListAsync(ct);
        }

    }
}
