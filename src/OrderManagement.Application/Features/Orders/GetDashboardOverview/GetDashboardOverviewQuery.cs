namespace OrderManagement.Application.Features.Orders.GetDashboardOverview
{
    public sealed record GetDashboardOverviewQuery(int RecentOrdersLimit = 5, int TrendMonths = 12);
}
