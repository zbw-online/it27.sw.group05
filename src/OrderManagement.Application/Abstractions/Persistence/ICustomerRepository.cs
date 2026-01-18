using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;


namespace OrderManagement.Application.Abstractions.Persistence
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default);
        Task<Customer?> GetByNumberAsync(CustomerNumber number, CancellationToken ct = default);

        Task AddAsync(Customer customer, CancellationToken ct = default);

        void Update(Customer customer);
        void Remove(Customer customer);
    }
}
