using Microsoft.Extensions.Options;

using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.DataExchange.Shared
{
    public sealed class CustomerImportPlanBuilder(
        ICustomerDataSerializerResolver serializerResolver,
        ICustomerQueryRepository customerQueryRepository,
        IOptions<CustomerDataExchangeOptions> options) : ICustomerImportPlanBuilder
    {
        private readonly CustomerDataExchangeOptions _options = options.Value;


        public async Task<CustomerImportPlan> BuildAsync(CustomerDataFile file, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Content.Length == 0)
            {
                return SingleFileIssue("Die Datei ist leer.");
            }

            if (file.Content.Length > _options.MaxFileSizeBytes)
            {
                return SingleFileIssue($"Die Datei überschreitet die maximale Grösse von {_options.MaxFileSizeBytes / (1024 * 1024)} MB.");
            }

            Result<ICustomerDataSerializer> resolveResult = serializerResolver.Resolve(file.Format);
            if (!resolveResult.IsSuccess)
            {
                return SingleFileIssue("Das Dateiformat wird nicht unterstützt.");
            }

            using var stream = new MemoryStream(file.Content);
            Result<IReadOnlyList<CustomerDataDto>> deserializeResult = await resolveResult.Value!.DeserializeAsync(stream, cancellationToken);
            if (!deserializeResult.IsSuccess)
            {
                return SingleFileIssue("Die Datei konnte nicht gelesen werden. Bitte überprüfen Sie das Format der Datei.");
            }

            IReadOnlyList<CustomerDataDto> records = deserializeResult.Value!;

            if (records.Count == 0)
            {
                return SingleFileIssue("Die Datei enthält keine Kundendaten.");
            }

            if (records.Count > _options.MaxCustomerCount)
            {
                return new CustomerImportPlan(
                    [],
                    [new CustomerImportValidationIssue(null, null, "file", $"Die Datei enthält zu viele Kundendatensätze (maximal {_options.MaxCustomerCount}).")],
                    records.Count);
            }

            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<Customer> existingCustomers = await customerQueryRepository.GetListAsync(cancellationToken);
            var existingNumbers = new HashSet<string>(existingCustomers.Select(c => c.CustomerNumber.Value), StringComparer.Ordinal);
            var existingEmails = new HashSet<string>(existingCustomers.Select(c => c.Email.Value), StringComparer.Ordinal);

            var seenNumbers = new HashSet<string>(StringComparer.Ordinal);
            var seenEmails = new HashSet<string>(StringComparer.Ordinal);
            var issues = new List<CustomerImportValidationIssue>();
            var customersToImport = new List<Customer>();

            for (int index = 0; index < records.Count; index++)
            {
                CustomerDataDto record = records[index];

                Result<CustomerNumber> numberResult = CustomerNumber.Create(record.CustomerNumber);
                if (!numberResult.IsSuccess)
                {
                    issues.Add(new CustomerImportValidationIssue(index, record.CustomerNumber, "customerNumber", TranslateDomainError(numberResult.Error!)));
                    continue;
                }

                string normalizedNumber = numberResult.Value!.Value;

                if (!seenNumbers.Add(normalizedNumber))
                {
                    issues.Add(new CustomerImportValidationIssue(index, normalizedNumber, "customerNumber", $"Die Kundennummer '{normalizedNumber}' ist in der Datei mehrfach vorhanden."));
                    continue;
                }

                Result<Email> emailResult = Email.Create(record.Email);
                if (!emailResult.IsSuccess)
                {
                    issues.Add(new CustomerImportValidationIssue(index, normalizedNumber, "email", TranslateDomainError(emailResult.Error!)));
                    continue;
                }

                string normalizedEmail = emailResult.Value!.Value;

                if (!seenEmails.Add(normalizedEmail))
                {
                    issues.Add(new CustomerImportValidationIssue(index, normalizedNumber, "email", $"Die E-Mail-Adresse '{normalizedEmail}' ist in der Datei mehrfach vorhanden."));
                    continue;
                }

                Result<Customer> customerResult = Customer.Create(
                    record.CustomerNumber,
                    record.LastName,
                    record.SurName,
                    record.Email,
                    record.Website);

                if (!customerResult.IsSuccess)
                {
                    issues.Add(new CustomerImportValidationIssue(index, normalizedNumber, "customer", TranslateDomainError(customerResult.Error!)));
                    continue;
                }

                Customer customer = customerResult.Value!;

                if (record.Address is not null)
                {
                    Result addressResult = customer.ChangeAddress(
                        record.Address.ValidFrom,
                        record.Address.Street,
                        record.Address.HouseNumber,
                        record.Address.PostalCode,
                        record.Address.City,
                        record.Address.CountryCode);

                    if (!addressResult.IsSuccess)
                    {
                        issues.Add(new CustomerImportValidationIssue(index, normalizedNumber, "address", TranslateDomainError(addressResult.Error!)));
                        continue;
                    }
                }

                if (existingNumbers.Contains(normalizedNumber))
                {
                    issues.Add(new CustomerImportValidationIssue(index, normalizedNumber, "customerNumber", $"Die Kundennummer '{normalizedNumber}' existiert bereits."));
                    continue;
                }

                if (existingEmails.Contains(normalizedEmail))
                {
                    issues.Add(new CustomerImportValidationIssue(index, normalizedNumber, "email", $"Die E-Mail-Adresse '{normalizedEmail}' existiert bereits."));
                    continue;
                }

                customersToImport.Add(customer);
            }

            return new CustomerImportPlan(customersToImport, issues, records.Count);
        }

        private static CustomerImportPlan SingleFileIssue(string message)
            => new([], [new CustomerImportValidationIssue(null, null, "file", message)], 0);

        private static string TranslateDomainError(string englishError) => englishError switch
        {
            "Customer number is required." => "Die Kundennummer ist erforderlich.",
            "Customer number must match a format similar to 'CU00001'." => "Die Kundennummer muss dem Format 'CU00001' entsprechen.",
            "Email is required." => "Die E-Mail-Adresse ist erforderlich.",
            "Email is too long." => "Die E-Mail-Adresse ist zu lang.",
            "Email format is invalid." => "Die E-Mail-Adresse ist ungültig.",
            "LastName is required." => "Der Nachname ist erforderlich.",
            "SurName is required." => "Der Vorname ist erforderlich.",
            "Website is too long." => "Die Website-Adresse ist zu lang.",
            "Website must be a valid website address." => "Die Website-Adresse ist ungültig.",
            "Street is required." => "Die Strasse ist erforderlich.",
            "HouseNumber is required." => "Die Hausnummer ist erforderlich.",
            "PostalCode is required." => "Die Postleitzahl ist erforderlich.",
            "City is required." => "Der Ort ist erforderlich.",
            "CountryCode must be 2 letters." => "Der Länder-Code muss aus 2 Buchstaben bestehen.",
            _ => "Die Kundendaten sind ungültig.",
        };
    }
}
