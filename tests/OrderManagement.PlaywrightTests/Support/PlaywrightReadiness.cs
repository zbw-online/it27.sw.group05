using Microsoft.Playwright;

namespace OrderManagement.PlaywrightTests.Support
{
    internal static class PlaywrightReadiness
    {
        private const int TimeoutMilliseconds = 20_000;

        public static async Task WaitForBlazorInteractiveAsync(this IPage page)
        {
            var consoleErrors = new List<string>();
            var pageErrors = new List<string>();

            void OnConsole(object? _, IConsoleMessage message)
            {
                if (message.Type == "error")
                {
                    consoleErrors.Add(message.Text);
                }
            }

            void OnPageError(object? _, string error) => pageErrors.Add(error);

            page.Console += OnConsole;
            page.PageError += OnPageError;

            try
            {
                await page.Locator("html[data-blazor-interactive='true']").WaitForAsync(new() { Timeout = TimeoutMilliseconds });
            }
            catch (TimeoutException ex)
            {
                string diagnostics = await BuildFailureDiagnosticsAsync(page, consoleErrors, pageErrors);
                throw new TimeoutException(
                    $"Blazor interactivity marker did not appear within {TimeoutMilliseconds}ms.{Environment.NewLine}{diagnostics}",
                    ex);
            }
            finally
            {
                page.Console -= OnConsole;
                page.PageError -= OnPageError;
            }
        }

        private static async Task<string> BuildFailureDiagnosticsAsync(
            IPage page,
            IReadOnlyList<string> consoleErrors,
            IReadOnlyList<string> pageErrors)
        {
            var lines = new List<string>
            {
                $"Requested URL: {page.Url}",
                $"HTTP response status for requested URL: {await GetHttpStatusAsync(page.Url)}",
            };

            lines.AddRange(PlaywrightAppFixture.BuildApplicationDiagnostics());

            lines.Add("Browser console errors:");
            lines.AddRange(FormatLines(consoleErrors));

            lines.Add("Browser page errors:");
            lines.AddRange(FormatLines(pageErrors));

            return string.Join(Environment.NewLine, lines);
        }

        private static async Task<string> GetHttpStatusAsync(string url)
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                HttpResponseMessage response = await httpClient.GetAsync(url);
                return $"{(int)response.StatusCode} {response.ReasonPhrase}";
            }
            catch (Exception ex)
            {
                return $"request failed ({ex.Message})";
            }
        }

        private static IEnumerable<string> FormatLines(IReadOnlyList<string> lines)
            => lines.Count == 0 ? ["  (none)"] : lines.Select(line => $"  {line}");
    }
}
