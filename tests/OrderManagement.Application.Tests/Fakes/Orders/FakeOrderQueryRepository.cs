using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Application.Tests.Fakes.Orders
{
    public sealed class FakeOrderQueryRepository : IOrderQueryRepository
    {
        private readonly List<Order> _orders = [];
        private int _nextId = 1;

        public Order Seed(Order order)
        {
            if (!order.Id.IsAssigned)
            {
                TestIdAssigner.Assign(order, new OrderId(_nextId));
            }

            _nextId = Math.Max(_nextId, order.Id.Value + 1);
            _orders.Add(order);
            return order;
        }

        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
            => Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));

        public Task<IReadOnlyList<Order>> GetListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Order>>([.. _orders]);

        public Task<Order?> GetByOrderNumberAsync(OrderNumber orderNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.FirstOrDefault(o => o.OrderNumber == orderNumber));

        public Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Order>>([.. _orders.Where(o => o.CustomerId == customerId)]);

        public Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default)
            => GetListAsync(cancellationToken);

        public Task<IReadOnlyList<Order>> GetUnreconciledOrdersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Order>>([.. _orders.Where(o => !o.IsInventoryApplied)]);

        public Task<bool> ExistsOrderLineForArticleAsync(ArticleId articleId, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.Any(o => o.Lines.Any(l => l.ArticleId == articleId)));
    }
}
