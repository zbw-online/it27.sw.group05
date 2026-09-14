namespace OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity
{
    public sealed record UpdateOrderLineQuantityCommand(int OrderId, int OrderLineId, int Quantity);
}
