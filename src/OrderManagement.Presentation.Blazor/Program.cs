using System.Globalization;

using Microsoft.Extensions.DependencyInjection.Extensions;

using OrderManagement.Application;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Infrastructure;
using OrderManagement.Presentation.Blazor.Components;
using OrderManagement.Presentation.Blazor.Hosting;

namespace OrderManagement.Presentation.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var swissCulture = CultureInfo.GetCultureInfo("de-CH");
            CultureInfo.DefaultThreadCurrentCulture = swissCulture;
            CultureInfo.DefaultThreadCurrentUICulture = swissCulture;

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            _ = builder.Services
                .AddRazorComponents()
                .AddInteractiveServerComponents();

            _ = builder.Services.AddHealthChecks();

            string connectionString = builder.Configuration.GetConnectionString("OrderManagement")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:OrderManagement' is missing.");

            _ = builder.Services.AddOrderManagementApplication();
            _ = builder.Services.AddOrderManagementInfrastructure(connectionString);
            _ = builder.Services.Configure<CustomerDataExchangeOptions>(
                builder.Configuration.GetSection(CustomerDataExchangeOptions.SectionName));

            string? fixedUtcNow = builder.Configuration["Testing:FixedUtcNow"];
            if (!string.IsNullOrWhiteSpace(fixedUtcNow))
            {
                var fixedNow = DateTimeOffset.Parse(
                    fixedUtcNow, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                _ = builder.Services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FixedTimeProvider(fixedNow)));
            }

            WebApplication app = builder.Build();

            if (args.Contains("reconcile-inventory", StringComparer.OrdinalIgnoreCase))
            {
                bool apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
                await InventoryReconciliationCliCommand.RunAsync(app.Services, apply);
                return;
            }

            if (!app.Environment.IsDevelopment())
            {
                _ = app.UseExceptionHandler("/Error");
                _ = app.UseHsts();
            }

            _ = app.UseRequestLocalization(new RequestLocalizationOptions()
                .SetDefaultCulture(swissCulture.Name)
                .AddSupportedCultures(swissCulture.Name)
                .AddSupportedUICultures(swissCulture.Name));

            _ = app.UseHttpsRedirection();
            _ = app.UseStaticFiles();
            _ = app.UseAntiforgery();

            _ = app.MapHealthChecks("/health/live");

            _ = app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            await app.RunAsync();
        }
    }
}
