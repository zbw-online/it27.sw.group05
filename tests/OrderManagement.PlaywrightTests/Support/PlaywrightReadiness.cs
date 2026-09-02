using Microsoft.Playwright;

namespace OrderManagement.PlaywrightTests.Support
{
    internal static class PlaywrightReadiness
    {
        public static async Task WaitForBlazorInteractiveAsync(this IPage page)
            => await page.Locator("html[data-blazor-interactive='true']").WaitForAsync(new() { Timeout = 20_000 });
    }
}
