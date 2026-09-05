using Microsoft.AspNetCore.Components.Web;

namespace OrderManagement.Presentation.Blazor.Components.Shared
{
    public sealed record CategoryFlyoutKeyEvent(int Level, int Index, CategoryFlyoutItem Item, KeyboardEventArgs Args);
}
