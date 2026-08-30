using OrderManagement.Application.DTOs.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetQuarterlyKpis
{
    public interface IGetQuarterlyKpisUseCase
    {
        Task<Result<IReadOnlyList<QuarterlyKpiRowDto>>> ExecuteAsync(
            GetQuarterlyKpisQuery query,
            CancellationToken cancellationToken = default);
    }
}
