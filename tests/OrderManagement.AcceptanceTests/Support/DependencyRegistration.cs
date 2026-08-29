using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application;
using OrderManagement.Infrastructure;

using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace OrderManagement.AcceptanceTests.Support
{
    public static class DependencyRegistration
    {
        [ScenarioDependencies]
        public static IServiceCollection CreateServices()
        {
            string databaseName = $"OrderManagement_Acceptance_{Guid.NewGuid():N}";

            var connectionStringBuilder = new SqlConnectionStringBuilder(AssemblySetup.MasterConnectionString)
            {
                InitialCatalog = databaseName,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            var services = new ServiceCollection();
            _ = services.AddOrderManagementApplication();
            _ = services.AddOrderManagementInfrastructure(connectionStringBuilder.ConnectionString);
            _ = services.AddScoped<AcceptanceTestContext>();

            return services;
        }
    }
}
