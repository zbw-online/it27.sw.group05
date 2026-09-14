namespace OrderManagement.Application.Features.Orders.Contracts
{
    public sealed record OrderSearchResultDto(
        IReadOnlyList<OrderSearchItemDto> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages);
}
