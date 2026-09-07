using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Presentation.Blazor.Components.Shared
{
    public static class OrderStatusPresentation
    {
        public static string Label(OrderStatus status) => status switch
        {
            OrderStatus.Open => "Offen",
            OrderStatus.InProgress => "In Bearbeitung",
            OrderStatus.Completed => "Abgeschlossen",
            OrderStatus.Cancelled => "Storniert",
            _ => status.ToString(),
        };

        public static StatusTone Tone(OrderStatus status) => status switch
        {
            OrderStatus.Open => StatusTone.Info,
            OrderStatus.InProgress => StatusTone.Warning,
            OrderStatus.Completed => StatusTone.Success,
            OrderStatus.Cancelled => StatusTone.Danger,
            _ => StatusTone.Neutral,
        };
    }
}
