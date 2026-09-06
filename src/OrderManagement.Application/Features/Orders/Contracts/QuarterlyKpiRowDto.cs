namespace OrderManagement.Application.Features.Orders.Contracts
{
    public sealed class QuarterlyKpiRowDto
    {
        public string Category { get; init; } = default!;
        public int Year { get; init; }
        public int Quarter { get; init; }
        public decimal Value { get; init; }
        public decimal? PrevYearValue { get; init; }
        public decimal? YoYDelta { get; init; }
        public decimal? YoYDeltaPercent { get; init; }
    }
}
