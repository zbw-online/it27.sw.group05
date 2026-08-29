using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Application.Tests.Fakes.Orders
{
    public sealed class FakeOrderCommandRepository : IOrderCommandRepository
    {
        private readonly Dictionary<OrderId, Order> _orders = [];
        private int _nextId = 1;

        public List<Order> Added { get; } = [];
        public List<Order> Updated { get; } = [];
        public List<Order> Removed { get; } = [];

        public Order Seed(Order order)
        {
            if (!order.Id.IsAssigned)
            {
                TestIdAssigner.Assign(order, new OrderId(_nextId));
            }

            _nextId = Math.Max(_nextId, order.Id.Value + 1);
            _orders[order.Id] = order;
            return order;
        }

        public void Add(Order order)
        {
            var id = new OrderId(_nextId++);
            TestIdAssigner.Assign(order, id);
            _orders[id] = order;
            Added.Add(order);
        }

        public void Update(Order order)
        {
            _orders[order.Id] = order;
            Updated.Add(order);
        }

        public void Remove(Order order)
        {
            _ = _orders.Remove(order.Id);
            Removed.Add(order);
        }

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.GetValueOrDefault(id));
    }
}
