namespace OrderManagement.Application.Features.Orders.GetDashboardOverview
{
    public sealed record MonthlyTrendPointDto(
        int Year,
        int Month,
        int OrderCount,
        decimal Revenue);
}
