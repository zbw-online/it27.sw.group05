using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;


namespace OrderManagement.Application.Abstractions.Interfaces.Customers.Query
{
    public interface ICustomerQueryRepository : IQueryRepository<Customer, CustomerId>
    {
        Task<Customer?> GetByCustomerNumberAsync(CustomerNumber number, CancellationToken ct = default);
        Task<Customer?> GetByEmailAsync(Email email, CancellationToken ct = default);
        Task<IReadOnlyList<Customer>> SearchByNameAsync(string searchTerm, CancellationToken ct = default);
    }
}
