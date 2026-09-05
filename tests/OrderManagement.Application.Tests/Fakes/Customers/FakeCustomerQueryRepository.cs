using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Fakes.Customers
{
    public sealed class FakeCustomerQueryRepository : ICustomerQueryRepository
    {
        private readonly List<Customer> _customers = [];
        private int _nextId = 1;

        public int GetListCallCount { get; private set; }

        public Customer Seed(Customer customer)
        {
            if (!customer.Id.IsAssigned)
            {
                TestIdAssigner.Assign(customer, new CustomerId(_nextId));
            }

            _nextId = Math.Max(_nextId, customer.Id.Value + 1);
            _customers.Add(customer);
            return customer;
        }

        public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct = default)
            => Task.FromResult(_customers.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Customer>> GetListAsync(CancellationToken ct = default)
        {
            GetListCallCount++;
            return Task.FromResult<IReadOnlyList<Customer>>([.. _customers]);
        }

        public Task<Customer?> GetByCustomerNumberAsync(CustomerNumber number, CancellationToken cancellationToken = default)
            => Task.FromResult(_customers.FirstOrDefault(c => c.CustomerNumber == number));

        public Task<Customer?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
            => Task.FromResult(_customers.FirstOrDefault(c => c.Email == email));

        public Task<IReadOnlyList<Customer>> SearchByNameOrNumberAsync(string searchTerm, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Customer>>([.. _customers.Where(c =>
                c.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.SurName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.CustomerNumber.Value.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))]);
    }
}
