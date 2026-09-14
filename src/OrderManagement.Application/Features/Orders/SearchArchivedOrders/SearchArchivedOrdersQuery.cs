namespace OrderManagement.Application.Features.Orders.SearchArchivedOrders
{
    public sealed record SearchArchivedOrdersQuery(string? SearchTerm, int Page = 1, int PageSize = 15);
}
