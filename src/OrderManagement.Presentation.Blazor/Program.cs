using System.Globalization;

using OrderManagement.Application;
using OrderManagement.Infrastructure;
using OrderManagement.Presentation.Blazor.Components;

namespace OrderManagement.Presentation.Blazor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var swissCulture = CultureInfo.GetCultureInfo("de-CH");
            CultureInfo.DefaultThreadCurrentCulture = swissCulture;
            CultureInfo.DefaultThreadCurrentUICulture = swissCulture;

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            _ = builder.Services
                .AddRazorComponents()
                .AddInteractiveServerComponents();

            string connectionString = builder.Configuration.GetConnectionString("OrderManagement")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:OrderManagement' is missing.");

            _ = builder.Services.AddOrderManagementApplication();
            _ = builder.Services.AddOrderManagementInfrastructure(connectionString);

            WebApplication app = builder.Build();

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

            _ = app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
