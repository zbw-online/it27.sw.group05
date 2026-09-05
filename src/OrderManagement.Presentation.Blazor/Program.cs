using System.Globalization;

using Microsoft.Extensions.DependencyInjection.Extensions;

using OrderManagement.Application;
using OrderManagement.Application.Features.Catalog.ReconcileInventory;
using OrderManagement.Infrastructure;
using OrderManagement.Presentation.Blazor.Components;

using SharedKernel.Primitives;

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

            string connectionString = builder.Configuration.GetConnectionString("OrderManagement")
                ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:OrderManagement' is missing.");

            _ = builder.Services.AddOrderManagementApplication();
            _ = builder.Services.AddOrderManagementInfrastructure(connectionString);

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
                await RunReconcileInventoryCommandAsync(app.Services, apply);
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

            _ = app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            await app.RunAsync();
        }

        private static async Task RunReconcileInventoryCommandAsync(IServiceProvider services, bool apply)
        {
            using IServiceScope scope = services.CreateScope();
            IReconcileInventoryUseCase useCase = scope.ServiceProvider.GetRequiredService<IReconcileInventoryUseCase>();

            Result<ReconciliationReportDto> result = await useCase.ExecuteAsync(new ReconcileInventoryCommand(apply));

            if (!result.IsSuccess)
            {
                Console.WriteLine($"Fehler: {result.Error}");
                return;
            }

            ReconciliationReportDto report = result.Value!;

            Console.WriteLine(apply ? "=== Lagerbestand-Abgleich (Anwenden) ===" : "=== Lagerbestand-Abgleich (Testlauf) ===");

            if (report.ArticleImpacts.Count == 0 && report.Conflicts.Count == 0)
            {
                Console.WriteLine("Keine nicht abgeglichenen Aufträge gefunden. Nichts zu tun.");
                return;
            }

            if (report.ArticleImpacts.Count > 0)
            {
                Console.WriteLine($"Betroffene Aufträge: {string.Join(", ", report.AffectedOrderNumbers)}");
                Console.WriteLine();
                Console.WriteLine("Artikel-Nr.\tBestand aktuell\tAbzuziehende Menge\tBestand danach");

                foreach (ReconciliationArticleImpactDto impact in report.ArticleImpacts)
                {
                    string flag = impact.HasInsufficientStock ? "  [KONFLIKT: unzureichender Bestand]" : string.Empty;
                    Console.WriteLine($"{impact.ArticleNumber}\t{impact.CurrentStock}\t{impact.QuantityToDeduct}\t{impact.ResultingStock}{flag}");
                }
            }

            if (report.Conflicts.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Konflikte:");
                foreach (string conflict in report.Conflicts)
                {
                    Console.WriteLine($"  - {conflict}");
                }
            }

            Console.WriteLine();
            Console.WriteLine(report.WasApplied
                ? "Abgleich wurde angewendet."
                : apply
                    ? "Abgleich wurde NICHT angewendet (Konflikte vorhanden)."
                    : "Testlauf abgeschlossen. Keine Änderungen wurden vorgenommen. Mit --apply erneut ausführen, um den Abgleich anzuwenden.");
        }

        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            private readonly DateTimeOffset _now = now;

            public override DateTimeOffset GetUtcNow() => _now;

            public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        }
    }
}
