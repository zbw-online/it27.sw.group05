using OrderManagement.TestSupport;

using Reqnroll;

namespace OrderManagement.AcceptanceTests.Support
{
    [Binding]
    public sealed class AssemblySetup
    {
        private static readonly SqlServerTestContainer Container = new();

        internal static string MasterConnectionString => Container.MasterConnectionString;

        [BeforeTestRun]
        public static async Task BeforeTestRunAsync() => await Container.StartAsync();

        [AfterTestRun]
        public static async Task AfterTestRunAsync() => await Container.DisposeAsync();
    }
}
