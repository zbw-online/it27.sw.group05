using OrderManagement.Application.Abstractions.Persistence.Catalog.Query;
using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetDashboardOverview
{
    public sealed class GetDashboardOverviewUseCase(
        IOrderQueryRepository orderQueryRepository,
        ICustomerQueryRepository customerQueryRepository,
        IArticleQueryRepository articleQueryRepository) : IGetDashboardOverviewUseCase
    {
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;

        public async Task<Result<DashboardOverviewDto>> ExecuteAsync(
            GetDashboardOverviewQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Order> orders = await _orderQueryRepository.GetListAsync(cancellationToken);
            IReadOnlyList<Customer> customers = await _customerQueryRepository.GetListAsync(cancellationToken);
            IReadOnlyList<Article> articles = await _articleQueryRepository.GetListAsync(cancellationToken);

            var customerNumberById = customers.ToDictionary(c => c.Id.Value, c => c.CustomerNumber.Value);

            decimal revenue = orders.Sum(o => o.Total.Amount);
            string currency = orders.Count == 0 ? "CHF" : orders[0].Total.Currency;
            decimal averageOrderValue = orders.Count == 0 ? 0m : decimal.Round(revenue / orders.Count, 2);

            IReadOnlyList<OrderListItemDto> recentOrders = [.. orders
                .OrderByDescending(o => o.OrderDate)
                .Take(query.RecentOrdersLimit)
                .Select(o => ToListItem(o, customerNumberById))];

            IReadOnlyList<MonthlyTrendPointDto> trend = BuildMonthlyTrend(orders, query.TrendMonths);

            var dto = new DashboardOverviewDto(
                orders.Count,
                customers.Count,
                revenue,
                currency,
                averageOrderValue,
                articles.Count,
                trend,
                recentOrders);

            return Results.Success(dto);
        }

        private static List<MonthlyTrendPointDto> BuildMonthlyTrend(IReadOnlyList<Order> orders, int trendMonths)
        {
            DateTime today = DateTime.UtcNow;
            var buckets = new List<MonthlyTrendPointDto>();

            for (int offset = trendMonths - 1; offset >= 0; offset--)
            {
                DateTime bucketMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-offset);

                IReadOnlyList<Order> ordersInMonth = [.. orders
                    .Where(o => o.OrderDate.Year == bucketMonth.Year && o.OrderDate.Month == bucketMonth.Month)];

                buckets.Add(new MonthlyTrendPointDto(
                    bucketMonth.Year,
                    bucketMonth.Month,
                    ordersInMonth.Count,
                    ordersInMonth.Sum(o => o.Total.Amount)));
            }

            return buckets;
        }

        private static OrderListItemDto ToListItem(Order order, Dictionary<int, string> customerNumberById)
            => new(
                order.Id.Value,
                order.OrderNumber.Value,
                order.OrderDate,
                order.CustomerId.Value,
                customerNumberById.TryGetValue(order.CustomerId.Value, out string? number) ? number : string.Empty,
                order.Lines.Count,
                order.Total.Amount,
                order.Total.Currency);
    }
}
