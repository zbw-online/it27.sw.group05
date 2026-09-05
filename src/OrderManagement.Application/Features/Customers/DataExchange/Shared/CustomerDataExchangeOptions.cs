namespace OrderManagement.Application.Features.Customers.DataExchange.Shared
{
    public sealed class CustomerDataExchangeOptions
    {
        public const string SectionName = "CustomerDataExchange";

        public long MaxFileSizeBytes { get; set; } = 2 * 1024 * 1024;
        public int MaxCustomerCount { get; set; } = 5000;
        public int MaxJsonDepth { get; set; } = 64;
        public long MaxXmlCharacters { get; set; } = 20_000_000;
    }
}
