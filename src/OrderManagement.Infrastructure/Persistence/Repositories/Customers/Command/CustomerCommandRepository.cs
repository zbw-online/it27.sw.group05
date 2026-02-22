
using OrderManagement.Application.Abstractions.Interfaces.Customers.Command;
using OrderManagement.Domain.Customers;


namespace OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command
{
    public class CustomerCommandRepository(OrderManagementDbContext context) : ICustomerCommandRepository
    {
        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        public void Add(Customer customer)
            => _context.Set<Customer>().Add(customer);

        public void Update(Customer customer)
            => _context.Set<Customer>().Update(customer);

        public void Remove(Customer customer)
            => _context.Set<Customer>().Remove(customer);
    }
}
