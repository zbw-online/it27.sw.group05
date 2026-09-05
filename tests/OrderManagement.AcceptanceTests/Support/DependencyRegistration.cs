using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application;
using OrderManagement.Infrastructure;
using OrderManagement.TestSupport;

using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace OrderManagement.AcceptanceTests.Support
{
    public static class DependencyRegistration
    {
        [ScenarioDependencies]
        public static IServiceCollection CreateServices()
        {
            string databaseName = TestDatabaseName.Create("OrderManagement_Acceptance");
            string connectionString = TestDatabaseName.BuildScopedConnectionString(AssemblySetup.MasterConnectionString, databaseName);

            var services = new ServiceCollection();
            _ = services.AddOrderManagementApplication();
            _ = services.AddOrderManagementInfrastructure(connectionString, enableDetailedErrors: true);
            _ = services.AddScoped<AcceptanceTestContext>();

            return services;
        }
    }
}
