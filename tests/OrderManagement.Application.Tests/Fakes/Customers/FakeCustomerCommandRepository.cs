using OrderManagement.Application.Abstractions.Persistence.Customers.Command;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

namespace OrderManagement.Application.Tests.Fakes.Customers
{
    public sealed class FakeCustomerCommandRepository : ICustomerCommandRepository
    {
        private readonly Dictionary<CustomerId, Customer> _customers = [];
        private int _nextId = 1;

        public List<Customer> Added { get; } = [];
        public List<Customer> Updated { get; } = [];
        public List<Customer> Removed { get; } = [];

        public Customer Seed(Customer customer)
        {
            if (!customer.Id.IsAssigned)
            {
                TestIdAssigner.Assign(customer, new CustomerId(_nextId));
            }

            _nextId = Math.Max(_nextId, customer.Id.Value + 1);
            _customers[customer.Id] = customer;
            return customer;
        }

        public void Add(Customer customer)
        {
            var id = new CustomerId(_nextId++);
            TestIdAssigner.Assign(customer, id);
            _customers[id] = customer;
            Added.Add(customer);
        }

        public void Update(Customer customer)
        {
            _customers[customer.Id] = customer;
            Updated.Add(customer);
        }

        public void Remove(Customer customer)
        {
            _ = _customers.Remove(customer.Id);
            Removed.Add(customer);
        }

        public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_customers.GetValueOrDefault(id));
    }
}
