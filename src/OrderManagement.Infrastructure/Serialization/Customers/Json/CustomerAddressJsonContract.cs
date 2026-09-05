namespace OrderManagement.Infrastructure.Serialization.Customers.Json
{
    internal sealed class CustomerAddressJsonContract
    {
        public DateOnly ValidFrom { get; set; }
        public string Street { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
    }
}
