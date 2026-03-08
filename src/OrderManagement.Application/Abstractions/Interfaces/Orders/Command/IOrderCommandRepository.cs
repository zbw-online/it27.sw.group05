using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Orders.Command
{
    public interface IOrderCommandRepository : ICommandRepository<Order, OrderId>
    {
    }
}
