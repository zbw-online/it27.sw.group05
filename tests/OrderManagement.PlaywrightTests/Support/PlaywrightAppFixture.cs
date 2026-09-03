using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

using Microsoft.EntityFrameworkCore;

using OrderManagement.Infrastructure.Persistence;
using OrderManagement.TestSupport;

namespace OrderManagement.PlaywrightTests.Support
{
    [TestClass]
    public static class PlaywrightAppFixture
    {
        private const int MaxCapturedOutputLines = 200;

        private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(60);
        private static readonly SqlServerTestContainer Container = new();
        private static readonly BoundedLog StandardOutput = new(MaxCapturedOutputLines);
        private static readonly BoundedLog StandardError = new(MaxCapturedOutputLines);

        private static Process? _appProcess;

        internal static string BaseUrl { get; private set; } = default!;
        internal static string ConnectionString { get; private set; } = default!;

        internal static bool HasApplicationProcessExited => _appProcess?.HasExited ?? true;

        internal static int? ApplicationExitCode => _appProcess is { HasExited: true } process ? process.ExitCode : null;

        internal static IReadOnlyList<string> GetRecentStandardOutput() => StandardOutput.Snapshot();

        internal static IReadOnlyList<string> GetRecentStandardError() => StandardError.Snapshot();

        internal static IReadOnlyList<string> BuildApplicationDiagnostics()
        {
            var lines = new List<string>
            {
                $"Application process exited: {HasApplicationProcessExited} (exit code: {ApplicationExitCode?.ToString(CultureInfo.InvariantCulture) ?? "n/a"})",
                "Recent application stdout:",
            };
            lines.AddRange(FormatLines(GetRecentStandardOutput()));
            lines.Add("Recent application stderr:");
            lines.AddRange(FormatLines(GetRecentStandardError()));
            return lines;
        }

        private static IEnumerable<string> FormatLines(IReadOnlyList<string> lines)
            => lines.Count == 0 ? ["  (none)"] : lines.Select(line => $"  {line}");

        [AssemblyInitialize]
        public static async Task AssemblyInitializeAsync(TestContext _)
        {
            await Container.StartAsync();

            string databaseName = TestDatabaseName.Create("OrderManagement_Playwright");
            ConnectionString = TestDatabaseName.BuildScopedConnectionString(Container.MasterConnectionString, databaseName);

            await MigrateAndSeedAsync();

            int port = GetFreeTcpPort();
            BaseUrl = $"http://127.0.0.1:{port}";

            _appProcess = StartApplicationProcess(port);
            await WaitUntilReadyAsync(BaseUrl);
        }

        [AssemblyCleanup]
        public static async Task AssemblyCleanupAsync()
        {
            if (_appProcess is { HasExited: false })
            {
                _appProcess.Kill(entireProcessTree: true);
                await _appProcess.WaitForExitAsync();
            }

            _appProcess?.Dispose();

            await Container.DisposeAsync();
        }

        private static async Task MigrateAndSeedAsync()
        {
            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(
                    ConnectionString,
                    sql => sql.MigrationsAssembly(typeof(OrderManagementDbContext).Assembly.FullName))
                .Options;

            await using var dbContext = new OrderManagementDbContext(options);
            await dbContext.Database.MigrateAsync();
            await PlaywrightSeedData.SeedAsync(dbContext);
        }

        private static Process StartApplicationProcess(int port)
        {
            string configuration = Environment.GetEnvironmentVariable("PLAYWRIGHT_APP_CONFIGURATION") ?? "Debug";
            string repositoryRoot = FindRepositoryRoot();
            string appDll = Path.Combine(
                repositoryRoot,
                "src", "OrderManagement.Presentation.Blazor", "bin", configuration, "net8.0",
                "OrderManagement.Presentation.Blazor.dll");

            if (!File.Exists(appDll))
            {
                throw new InvalidOperationException(
                    $"Could not find built application at '{appDll}'. Build the solution (dotnet build --configuration {configuration}) before running Playwright tests.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"exec \"{appDll}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            startInfo.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["ConnectionStrings__OrderManagement"] = ConnectionString;
            startInfo.Environment["Testing__FixedUtcNow"] = PlaywrightSeedData.ReferenceNow.ToString("O", CultureInfo.InvariantCulture);

            var process = new Process { StartInfo = startInfo };

            // The app's stdout/stderr must be drained even though nothing here needs the content:
            // ProcessStartInfo redirects them into a fixed-size OS pipe, and once the app's own
            // request/EF Core logging fills that pipe, the app process blocks on its next write -
            // hanging every subsequent request the tests depend on. A bounded ring buffer keeps a
            // recent tail for diagnostics without letting captured output grow unbounded.
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) StandardOutput.Add(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) StandardError.Add(e.Data); };
            _ = process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return process;
        }

        private static async Task WaitUntilReadyAsync(string baseUrl)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var deadlineCts = new CancellationTokenSource(ReadinessTimeout);

            string lastResult = "no attempt completed";

            while (!deadlineCts.IsCancellationRequested)
            {
                if (HasApplicationProcessExited)
                {
                    throw new InvalidOperationException(BuildReadinessFailureMessage(
                        baseUrl, $"the application process exited before becoming ready (exit code: {ApplicationExitCode?.ToString(CultureInfo.InvariantCulture) ?? "n/a"})"));
                }

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(baseUrl, deadlineCts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    lastResult = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                }
                catch (Exception ex) when (!deadlineCts.IsCancellationRequested)
                {
                    lastResult = ex.Message;
                }

                await Task.Delay(500, CancellationToken.None);
            }

            throw new InvalidOperationException(BuildReadinessFailureMessage(
                baseUrl, $"it did not return a successful response within {ReadinessTimeout.TotalSeconds} seconds (last response: {lastResult})"));
        }

        private static string BuildReadinessFailureMessage(string baseUrl, string reason)
        {
            var lines = new List<string> { $"The application at '{baseUrl}' was not ready: {reason}." };
            lines.AddRange(BuildApplicationDiagnostics());
            return string.Join(Environment.NewLine, lines);
        }

        private static int GetFreeTcpPort()
        {
            using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OrderManagement.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName
                ?? throw new InvalidOperationException("Could not locate the repository root (OrderManagement.sln) from the test output directory.");
        }

        private sealed class BoundedLog(int capacity)
        {
            private readonly ConcurrentQueue<string> _lines = new();
            private readonly int _capacity = capacity;

            public void Add(string line)
            {
                _lines.Enqueue(line);
                while (_lines.Count > _capacity && _lines.TryDequeue(out _))
                {
                }
            }

            public IReadOnlyList<string> Snapshot() => [.. _lines];
        }
    }
}
