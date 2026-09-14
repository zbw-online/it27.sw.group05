using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetDashboardOverview
{
    public interface IGetDashboardOverviewUseCase
    {
        Task<Result<DashboardOverviewDto>> ExecuteAsync(
            GetDashboardOverviewQuery query,
            CancellationToken cancellationToken = default);
    }
}
