namespace OrderManagement.Application.Abstractions.Persistence.Orders.Query
{
    public sealed record OrderSearchCriteria(
        OrderSearchScope Scope,
        string? SearchTerm,
        int Page,
        int PageSize,
        DateOnly Today);
}
