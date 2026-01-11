using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Customers
{
    public sealed class CustomerAddress : Entity<int>
    {

        private CustomerAddress() : base(0) { }

        internal CustomerAddress(
            int id,
            DateOnly validFrom,
            DateOnly? validTo,
            string street,
            string houseNumber,
            string postalCode,
            string city,
            string countryCode) : base(id)
        {

            ValidFrom = validFrom;
            ValidTo = validTo;
            Street = street;
            HouseNumber = houseNumber;
            PostalCode = postalCode;
            City = city;
            CountryCode = countryCode;
        }

        public DateOnly ValidFrom { get; private set; }
        public DateOnly? ValidTo { get; private set; }

        public string Street { get; private set; } = default!;
        public string HouseNumber { get; private set; } = default!;
        public string PostalCode { get; private set; } = default!;
        public string City { get; private set; } = default!;
        public string CountryCode { get; private set; } = default!;

        public bool IsAciveOn(DateOnly date) => ValidFrom <= date && (ValidTo is null || date <= ValidTo.Value);

        internal void Close(DateOnly validTo) => ValidTo = validTo;

    }
}
