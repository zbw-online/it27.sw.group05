
using OrderManagement.Application.DTOs.Orders;

namespace OrderManagement.Application.Abstractions.Interfaces.Orders.Query
{
    public interface IQuarterlyKpiQueryRepository
    {
        Task<IReadOnlyList<QuarterlyKpiRowDto>> GetQuarterlyKpisLast3YearsAsync(CancellationToken ct = default);
    }

}
