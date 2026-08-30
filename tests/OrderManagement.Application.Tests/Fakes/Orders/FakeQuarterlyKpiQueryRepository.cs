using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Application.DTOs.Orders;

namespace OrderManagement.Application.Tests.Fakes.Orders
{
    public sealed class FakeQuarterlyKpiQueryRepository : IQuarterlyKpiQueryRepository
    {
        public IReadOnlyList<QuarterlyKpiRowDto> Rows { get; set; } = [];

        public Task<IReadOnlyList<QuarterlyKpiRowDto>> GetQuarterlyKpisLast3YearsAsync(CancellationToken ct = default)
            => Task.FromResult(Rows);
    }
}
