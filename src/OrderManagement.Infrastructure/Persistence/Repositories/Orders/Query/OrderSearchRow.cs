namespace OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query
{
    /// <summary>
    /// Flat projection row for the raw SQL order search query. Kept separate from the Application
    /// DTO because EF Core's raw SQL mapping for unmapped types matches column aliases to settable
    /// properties, not positional record constructors.
    /// </summary>
    internal sealed class OrderSearchRow
    {
        public int OrderId { get; init; }
        public string OrderNumber { get; init; } = default!;
        public DateTime OrderDate { get; init; }
        public DateOnly DeliveryDate { get; init; }
        public int CustomerId { get; init; }
        public string CustomerNumber { get; init; } = default!;
        public int LineCount { get; init; }
        public decimal TotalAmount { get; init; }
        public string TotalCurrency { get; init; } = default!;
        public string Status { get; init; } = default!;
        public bool IsOverdue { get; init; }
    }
}
