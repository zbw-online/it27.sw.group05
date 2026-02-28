using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Customers.Command
{
    public interface ICustomerCommandRepository : ICommandRepository<Customer, CustomerId>
    {
    }
}
