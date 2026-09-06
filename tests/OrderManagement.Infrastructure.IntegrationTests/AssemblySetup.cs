using OrderManagement.TestSupport;

namespace OrderManagement.Infrastructure.IntegrationTests
{
    [TestClass]
    public static class AssemblySetup
    {
        private static readonly SqlServerTestContainer Container = new();

        internal static string MasterConnectionString => Container.MasterConnectionString;

        [AssemblyInitialize]
        public static async Task AssemblyInitialize(TestContext _) => await Container.StartAsync();

        [AssemblyCleanup]
        public static async Task AssemblyCleanup() => await Container.DisposeAsync();
    }
}
