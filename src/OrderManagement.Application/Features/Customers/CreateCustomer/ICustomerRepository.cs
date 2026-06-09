using OrderManagement.Domain.Customers;

namespace OrderManagement.Application.Features.Customers.CreateCustomer
{
    public interface ICustomerRepository
    {
        void Add(Customer customer);

        Task<bool> ExistsWithCustomerNumberAsync(
            string customerNumber,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsWithEmailAsync(
            string email,
            CancellationToken cancellationToken = default);
    }
}
