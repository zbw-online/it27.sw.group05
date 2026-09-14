using OrderManagement.Application.Features.Orders.Contracts;

namespace OrderManagement.Application.Features.Orders.GetDashboardOverview
{
    public sealed record DashboardOverviewDto(
        int TotalOrders,
        int ActiveCustomers,
        decimal Revenue,
        string RevenueCurrency,
        decimal AverageOrderValue,
        int ArticleCount,
        IReadOnlyList<MonthlyTrendPointDto> MonthlyTrend,
        IReadOnlyList<OrderListItemDto> RecentOrders);
}
