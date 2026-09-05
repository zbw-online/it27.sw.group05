namespace OrderManagement.Infrastructure.Serialization.Customers.Json
{
    internal sealed class CustomerJsonContract
    {
        public string CustomerNumber { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SurName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Website { get; set; }
        public CustomerAddressJsonContract? Address { get; set; }
    }
}
