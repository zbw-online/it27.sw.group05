using System.Globalization;

namespace OrderManagement.Presentation.Blazor
{
    public static class Formatting
    {
        public static readonly CultureInfo SwissCulture = CultureInfo.GetCultureInfo("de-CH");

        public static string Chf(decimal amount) => $"CHF {amount.ToString("N2", SwissCulture)}";

        public static string Number(decimal amount) => amount.ToString("N0", SwissCulture);

        public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        public static string Date(DateTime date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    }
}
