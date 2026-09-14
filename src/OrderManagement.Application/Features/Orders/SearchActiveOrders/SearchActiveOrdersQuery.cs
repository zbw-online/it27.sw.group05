namespace OrderManagement.Application.Features.Orders.SearchActiveOrders
{
    public sealed record SearchActiveOrdersQuery(string? SearchTerm, int Page = 1, int PageSize = 15);
}
