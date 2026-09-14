namespace OrderManagement.Application.Features.Orders.AddOrderLine
{
    public sealed record AddOrderLineCommand(int OrderId, int ArticleId, int Quantity);
}
