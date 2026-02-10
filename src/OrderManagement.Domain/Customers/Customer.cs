using OrderManagement.Domain.Customers.Events;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;



namespace OrderManagement.Domain.Customers
{
    public sealed class Customer : AggregateRoot<CustomerId>
    {

        private readonly List<CustomerAddress> _addresses = [];

        private Customer() : base(new CustomerId(0)) { }
        private Customer(
            CustomerId id,
            CustomerNumber number,
            string lastName,
            string surName,
            Email email,
            string? website,
            string passwordHash
            ) : base(id)
        {

            CustomerNumber = number;
            LastName = lastName;
            SurName = surName;
            Email = email;
            Website = website;
            PasswordHash = passwordHash;

            AddDomainEvent(new CustomerCreated(id, DateTime.UtcNow));
        }

        public CustomerNumber CustomerNumber { get; private set; } = default!;
        public string LastName { get; private set; } = default!;
        public string SurName { get; private set; } = default!;
        public Email Email { get; private set; } = default!;
        public string? Website { get; private set; } = default!;
        public string PasswordHash { get; private set; } = default!;

        public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

        public CustomerAddress? AddressAt(DateOnly onDate)
            => _addresses
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefault(a => a.IsActiveOn(onDate));

        public static Result<Customer> Create(
            int id,
            string customerNr,
            string lastName,
            string surName,
            string email,
            string? website,
            string passwordHash
            )
        {

            // ID and CustomerNr Rules
            if (id <= 0) return Results.Fail<Customer>("Customer id must be positive.");

            Result<CustomerNumber> nr = CustomerNumber.Create(customerNr);

            if (!nr.IsSuccess) return Results.Fail<Customer>(nr.Error!);

            // E-Mail Rules
            Result<Email> em = Email.Create(email);
            if (!em.IsSuccess) return Results.Fail<Customer>(em.Error!);

            // Last- SurName Rules
            string ln = (lastName ?? string.Empty).Trim();
            string sn = (surName ?? string.Empty).Trim();

            if (ln.Length == 0) return Results.Fail<Customer>("LastName is required.");
            if (sn.Length == 0) return Results.Fail<Customer>("SurName is required.");

            // Website Rules
            string? w = null;
            string websiteTrim = (website ?? string.Empty).Trim();
            if (websiteTrim.Length > 0)
            {
                if (websiteTrim.Length > 255) return Results.Fail<Customer>("Website is too long.");
                if (!Uri.TryCreate(websiteTrim, UriKind.Absolute, out _))
                {
                    return Results.Fail<Customer>("Website must be a valid absolute URL.");
                }
                w = websiteTrim;
            }

            // PasswordHash Rules
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                return Results.Fail<Customer>("PasswordHash is required.");
            }

            var customer = new Customer(
                new CustomerId(id),
                nr.Value!,
                ln,
                sn,
                em.Value!,
                w,
                passwordHash
                );

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

            _addresses.Add(new CustomerAddress(
                id: 0,
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

        public Result ChangeWebsite(string? website)
        {
            string w = (website ?? string.Empty).Trim();

            if (w.Length == 0)
            {
                Website = null;
                return Result.Success();
            }

            if (w.Length > 255) return Result.Fail("Website is too long.");
            if (!Uri.TryCreate(w, UriKind.Absolute, out _))
            {
                return Result.Fail("Website must be a valid absolute URL.");
            }
            Website = w;
            return Result.Success();
        }

        public Result SetPasswordHash(string encodedHash)
        {
            if (string.IsNullOrWhiteSpace(encodedHash))
            {
                return Result.Fail("Password hash is required."); ;
            }

            PasswordHash = encodedHash.Trim();
            return Result.Success();
        }

    }
}
