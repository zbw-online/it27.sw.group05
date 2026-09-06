using OrderManagement.Application.Features.Catalog.ReconcileInventory;

using SharedKernel.Primitives;

namespace OrderManagement.Presentation.Blazor.Hosting
{
    internal static class InventoryReconciliationCliCommand
    {
        public static async Task RunAsync(IServiceProvider services, bool apply)
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
    }
}
