using OrderManagement.Domain.Customers.Events;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;



namespace OrderManagement.Domain.Customers
{
    public sealed class Customer : AggregateRoot<CustomerId>
    {

        private readonly List<CustomerAddress> _addresses = [];

        private Customer() : base(CustomerId.Empty)
        {
            // Required by EF Core.
        }
        private Customer(
            CustomerNumber customerNumber,
            string lastName,
            string surName,
            Email email,
            string? website
            ) : base(CustomerId.Empty)
        {

            CustomerNumber = customerNumber;
            LastName = lastName;
            SurName = surName;
            Email = email;
            Website = website;

            AddDomainEvent(new CustomerCreated(customerNumber, DateTime.UtcNow));
        }

        public CustomerNumber CustomerNumber { get; private set; } = default!;
        public string LastName { get; private set; } = default!;
        public string SurName { get; private set; } = default!;
        public Email Email { get; private set; } = default!;
        public string? Website { get; private set; }

        public IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();

        public CustomerAddress? AddressAt(DateOnly onDate)
            => _addresses
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefault(a => a.IsActiveOn(onDate));

        public static Result<Customer> Create(
            string customerNr,
            string lastName,
            string surName,
            string email,
            string? website
            )
        {
            // CustomerNumber Rules
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

            Result<string?> websiteResult = NormalizeWebsite(website);
            if (!websiteResult.IsSuccess)
            {
                return Results.Fail<Customer>(websiteResult.Error!);
            }

            string? w = websiteResult.Value;

            var customer = new Customer(
                nr.Value!,
                ln,
                sn,
                em.Value!,
                w
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
                validFrom: validFrom,
                validTo: null,
                street: street.Trim(),
                houseNumber: houseNumber.Trim(),
                postalCode: postalCode.Trim(),
                city: city.Trim(),
                countryCode: countryCode.Trim().ToUpperInvariant()
                ));

            AddDomainEvent(new CustomerAddressChanged(CustomerNumber, DateTime.UtcNow));
            return Result.Success();
        }

        public Result ChangeWebsite(string? website)
        {
            Result<string?> websiteResult = NormalizeWebsite(website);
            if (!websiteResult.IsSuccess)
            {
                return Result.Fail(websiteResult.Error!);
            }

            Website = websiteResult.Value;
            return Result.Success();
        }


        private static Result<string?> NormalizeWebsite(string? website)
        {
            string value = (website ?? string.Empty).Trim();

            if (value.Length == 0)
            {
                return Results.Success<string?>(null);
            }

            if (value.Length > 255)
            {
                return Results.Fail<string?>("Website is too long.");
            }

            string valueForValidation = value.Contains("://", StringComparison.Ordinal)
                ? value
                : $"https://{value}";

            return !Uri.TryCreate(valueForValidation, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrWhiteSpace(uri.Host) ||
                !uri.Host.Contains('.', StringComparison.Ordinal)
                ? Results.Fail<string?>("Website must be a valid website address.")
                : Results.Success<string?>(value);
        }

        public Result ChangeName(string lastName, string surName)
        {
            string normalizedLastName = (lastName ?? string.Empty).Trim();
            string normalizedSurName = (surName ?? string.Empty).Trim();

            if (normalizedLastName.Length == 0)
            {
                return Result.Fail("LastName is required.");
            }

            if (normalizedSurName.Length == 0)
            {
                return Result.Fail("SurName is required.");
            }

            LastName = normalizedLastName;
            SurName = normalizedSurName;

            return Result.Success();
        }

        public Result ChangeEmail(string email)
        {
            Result<Email> emailResult = Email.Create(email);

            if (!emailResult.IsSuccess)
            {
                return Result.Fail(emailResult.Error!);
            }

            Email = emailResult.Value!;

            return Result.Success();
        }
    }
}
