using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Application.DTOs.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetQuarterlyKpis
{
    public sealed class GetQuarterlyKpisUseCase(
        IQuarterlyKpiQueryRepository quarterlyKpiQueryRepository) : IGetQuarterlyKpisUseCase
    {
        private readonly IQuarterlyKpiQueryRepository _quarterlyKpiQueryRepository = quarterlyKpiQueryRepository;

        public async Task<Result<IReadOnlyList<QuarterlyKpiRowDto>>> ExecuteAsync(
            GetQuarterlyKpisQuery query,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<QuarterlyKpiRowDto> rows = await _quarterlyKpiQueryRepository.GetQuarterlyKpisLast3YearsAsync(cancellationToken);
            return Results.Success(rows);
        }
    }
}
