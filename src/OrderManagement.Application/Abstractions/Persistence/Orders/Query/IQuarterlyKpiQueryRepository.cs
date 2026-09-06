
using OrderManagement.Application.Features.Orders.Contracts;

namespace OrderManagement.Application.Abstractions.Persistence.Orders.Query
{
    public interface IQuarterlyKpiQueryRepository
    {
        Task<IReadOnlyList<QuarterlyKpiRowDto>> GetQuarterlyKpisLast3YearsAsync(CancellationToken ct = default);
    }

}
