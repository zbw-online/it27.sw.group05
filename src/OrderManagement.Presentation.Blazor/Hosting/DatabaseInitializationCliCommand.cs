using OrderManagement.Infrastructure.Persistence.Initialization;

using SharedKernel.Primitives;

namespace OrderManagement.Presentation.Blazor.Hosting
{
    internal static class DatabaseInitializationCliCommand
    {
        public static async Task<bool> RunAsync(IServiceProvider services)
        {
            using IServiceScope scope = services.CreateScope();
            IDatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

            Result result = await initializer.InitializeAsync();

            if (!result.IsSuccess)
            {
                await Console.Error.WriteLineAsync($"Fehler: {result.Error}");
                return false;
            }

            Console.WriteLine("Datenbankinitialisierung erfolgreich abgeschlossen.");
            return true;
        }
    }
}
