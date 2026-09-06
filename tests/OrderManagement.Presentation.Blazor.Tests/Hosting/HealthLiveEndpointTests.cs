using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace OrderManagement.Presentation.Blazor.Tests.Hosting
{
    [TestClass]
    public sealed class HealthLiveEndpointTests
    {
        [TestMethod]
        public async Task HealthLive_WithUnreachableDatabase_ReturnsSuccess()
        {
            await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    _ = config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:OrderManagement"] =
                            "Server=unreachable;Database=OrderManagement;User Id=sa;Password=Placeholder_Not_Real!;TrustServerCertificate=True;Connect Timeout=1;"
                    });
                }));

            using HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
