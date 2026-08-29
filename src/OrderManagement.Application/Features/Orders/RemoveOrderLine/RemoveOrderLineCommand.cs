namespace OrderManagement.Application.Features.Orders.RemoveOrderLine
{
    public sealed record RemoveOrderLineCommand(int OrderId, int OrderLineId);
}
