namespace OrderManagement.AcceptanceTests.Support
{
    public sealed class AcceptanceTestContext
    {
        public Dictionary<string, int> CustomerIdsByNumber { get; } = [];
        public Dictionary<string, int> ArticleGroupIdsByName { get; } = [];
        public Dictionary<string, int> ArticleIdsByNumber { get; } = [];
        public Dictionary<string, int> OrderIdsByNumber { get; } = [];
    }
}
