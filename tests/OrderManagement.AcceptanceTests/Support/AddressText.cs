namespace OrderManagement.AcceptanceTests.Support
{
    internal sealed record AddressText(string Street, string HouseNumber, string PostalCode, string City, string CountryCode)
    {
        public static AddressText Parse(string text)
        {
            string[] parts = text.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
            {
                throw new FormatException($"Expected address format 'Street Number, PostalCode City, CountryCode' but got '{text}'.");
            }

            int lastSpace = parts[0].LastIndexOf(' ');
            string street = parts[0][..lastSpace];
            string houseNumber = parts[0][(lastSpace + 1)..];

            int firstSpace = parts[1].IndexOf(' ');
            string postalCode = parts[1][..firstSpace];
            string city = parts[1][(firstSpace + 1)..];

            return new AddressText(street, houseNumber, postalCode, city, parts[2]);
        }

        public string ToDisplayString() => $"{Street} {HouseNumber}, {PostalCode} {City}, {CountryCode}";
    }
}
