namespace OrderManagement.Application.Features.Orders.CreateOrder
{
    public sealed record CreateOrderLineInput(int ArticleId, int Quantity);
}
