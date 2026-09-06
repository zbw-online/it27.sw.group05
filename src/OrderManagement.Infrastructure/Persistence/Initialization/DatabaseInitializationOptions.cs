namespace OrderManagement.Infrastructure.Persistence.Initialization
{
    public sealed class DatabaseInitializationOptions
    {
        public const string SectionName = "DatabaseInitialization";

        public bool SeedDemoData { get; set; }
    }
}
