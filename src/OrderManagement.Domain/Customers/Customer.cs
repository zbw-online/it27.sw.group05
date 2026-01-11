using OrderManagement.Domain.Customers.Events;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;



namespace OrderManagement.Domain.Customers
{
    public sealed class Customer : AggregateRoot<CustomerId>
    {

        private readonly List<CustomerAddress> _addresses = [];
        private int _addressIdSeq;

        private Customer() : base(default) { }
        private Customer(
            CustomerId id,
            CustomerNumber number,
            string lastName,
            string surName,
            Email email
            ) : base(id)
        {

            CustomerNumber = number;
            LastName = lastName;
            SurName = surName;
            Email = email;

            AddDomainEvent(new CustomerCreated(id, DateTime.UtcNow));
        }

        public CustomerNumber CustomerNumber { get; private set; } = default!;
        public string LastName { get; private set; } = default!;
        public string SurName { get; set; } = default!;
        public Email Email { get; private set; } = default!;

        public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

        public CustomerAddress? CurrentAddress(DateOnly onDate)
            => _addresses
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefault(a => a.IsAciveOn(onDate) && a.ValidTo is null);

        public static Result<Customer> Create(
            int id,
            string? customerNr,
            string? lastName,
            string? surName,
            string? email
            )
        {

            if (id <= 0) return Results.Fail<Customer>("Customer id must be positive.");

            Result<CustomerNumber> nr = CustomerNumber.Create(customerNr);

            if (!nr.IsSuccess) return Results.Fail<Customer>(nr.Error!);

            Result<Email> em = Email.Create(email);
            if (!em.IsSuccess) return Results.Fail<Customer>(em.Error!);

            string ln = (lastName ?? string.Empty).Trim();
            string sn = (surName ?? string.Empty).Trim();

            if (ln.Length == 0) return Results.Fail<Customer>("LastName is required.");
            if (sn.Length == 0) return Results.Fail<Customer>("SurName is required.");

            var customer = new Customer(new CustomerId(id), nr.Value!, ln, sn, em.Value!);

            return Results.Success(customer);
        }

        public Result ChangeAddress(
            DateOnly validFrom,
            string street,
            string houseNumber,
            string postalCode,
            string city,
            string countryCode)
        {

            // Basic domain validations (business rules)
            if (string.IsNullOrWhiteSpace(street)) return Result.Fail("Street is required.");
            if (string.IsNullOrWhiteSpace(houseNumber)) return Result.Fail("HouseNumber is required.");
            if (string.IsNullOrWhiteSpace(postalCode)) return Result.Fail("PostalCode is required.");
            if (string.IsNullOrWhiteSpace(city)) return Result.Fail("City is required.");
            if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2) return Result.Fail("CountryCode must be 2 letters.");

            CustomerAddress? active = _addresses.FirstOrDefault(a => a.ValidTo is null);

            if (active is not null)
            {
                // Close current address the day before new address becomes valid
                DateOnly closeDate = validFrom.AddDays(-1);
                if (closeDate < active.ValidFrom)
                {
                    return Result.Fail("validFrom overlaps existing active address.");
                }
                active.Close(closeDate);
            }

            _addressIdSeq++;
            _addresses.Add(new CustomerAddress(
                id: _addressIdSeq,
                validFrom: validFrom,
                validTo: null,
                street: street.Trim(),
                houseNumber: houseNumber.Trim(),
                postalCode: postalCode.Trim(),
                city: city.Trim(),
                countryCode: countryCode.Trim().ToUpperInvariant()
                ));

            AddDomainEvent(new CustomerAddressChanged(Id, DateTime.UtcNow));
            return Result.Success();
        }
    }
}
