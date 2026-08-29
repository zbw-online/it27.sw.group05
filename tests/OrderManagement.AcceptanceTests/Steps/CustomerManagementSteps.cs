using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.AcceptanceTests.Support;
using OrderManagement.Application.Features.Customers.CreateCustomer;
using OrderManagement.Application.Features.Customers.DeleteCustomer;
using OrderManagement.Application.Features.Customers.GetCustomerDetails;
using OrderManagement.Application.Features.Customers.SearchCustomers;
using OrderManagement.Application.Features.Customers.Shared;
using OrderManagement.Application.Features.Customers.UpdateCustomer;

using Reqnroll;

using SharedKernel.Primitives;

namespace OrderManagement.AcceptanceTests.Steps
{
    [Binding]
    public sealed class CustomerManagementSteps(
        ICreateCustomerUseCase createCustomerUseCase,
        IUpdateCustomerUseCase updateCustomerUseCase,
        IDeleteCustomerUseCase deleteCustomerUseCase,
        ISearchCustomersUseCase searchCustomersUseCase,
        IGetCustomerDetailsUseCase getCustomerDetailsUseCase,
        AcceptanceTestContext context)
    {
        private Result<CreateCustomerResponse>? _lastCreateResult;
        private Result? _lastCommandResult;
        private IReadOnlyList<CustomerListItemDto>? _lastSearchResult;

        [Given(@"no customer is registered with the number ""([^""]*)""")]
        public async Task GivenNoCustomerIsRegisteredWithTheNumber(string customerNumber)
        {
            Result<IReadOnlyList<CustomerListItemDto>> result = await searchCustomersUseCase.ExecuteAsync(
                new SearchCustomersQuery(customerNumber));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsFalse(result.Value!.Any(c => c.CustomerNumber == customerNumber));
        }

        [Given(@"a customer ""([^""]*)"" named ""([^""]*)"" is already registered")]
        public async Task GivenACustomerIsAlreadyRegistered(string customerNumber, string fullName)
            => await GivenACustomerWithEmailIsAlreadyRegistered(customerNumber, fullName, $"{customerNumber.ToLowerInvariant()}@example.com");

        [Given(@"a customer ""([^""]*)"" named ""([^""]*)"" with email ""([^""]*)"" is already registered")]
        public async Task GivenACustomerWithEmailIsAlreadyRegistered(string customerNumber, string fullName, string email)
        {
            (string lastName, string surName) = SplitName(fullName);

            Result<CreateCustomerResponse> result = await createCustomerUseCase.ExecuteAsync(new CreateCustomerCommand(
                customerNumber, lastName, surName, email, null,
                new DateOnly(2026, 1, 1), "Main Street", "1", "8000", "Zurich", "CH"));

            Assert.IsTrue(result.IsSuccess, result.Error);
            context.CustomerIdsByNumber[customerNumber] = result.Value!.CustomerId;
        }

        [When(@"I register a customer ""([^""]*)"" named ""([^""]*)"" with email ""([^""]*)"" and address ""([^""]*)""")]
        public async Task WhenIRegisterACustomer(string customerNumber, string fullName, string email, string address)
        {
            (string lastName, string surName) = SplitName(fullName);
            var parsedAddress = AddressText.Parse(address);

            _lastCreateResult = await createCustomerUseCase.ExecuteAsync(new CreateCustomerCommand(
                customerNumber, lastName, surName, email, null,
                DateOnly.FromDateTime(DateTime.Today), parsedAddress.Street, parsedAddress.HouseNumber,
                parsedAddress.PostalCode, parsedAddress.City, parsedAddress.CountryCode));

            if (_lastCreateResult.Value.IsSuccess)
            {
                context.CustomerIdsByNumber[customerNumber] = _lastCreateResult.Value.Value!.CustomerId;
            }
        }

        [When(@"I update customer ""([^""]*)"" to name ""([^""]*)"" and email ""([^""]*)""")]
        public async Task WhenIUpdateCustomer(string customerNumber, string fullName, string email)
        {
            (string lastName, string surName) = SplitName(fullName);
            int customerId = context.CustomerIdsByNumber[customerNumber];

            _lastCommandResult = await updateCustomerUseCase.ExecuteAsync(new UpdateCustomerCommand(
                customerId, lastName, surName, email, null,
                new DateOnly(2026, 1, 1), "Main Street", "1", "8000", "Zurich", "CH"));

            Assert.IsTrue(_lastCommandResult.Value.IsSuccess, _lastCommandResult.Value.Error);
        }

        [When(@"I search customers for ""([^""]*)""")]
        public async Task WhenISearchCustomersFor(string searchTerm)
        {
            Result<IReadOnlyList<CustomerListItemDto>> result = await searchCustomersUseCase.ExecuteAsync(
                new SearchCustomersQuery(searchTerm));

            Assert.IsTrue(result.IsSuccess, result.Error);
            _lastSearchResult = result.Value;
        }

        [When(@"I delete customer ""([^""]*)""")]
        public async Task WhenIDeleteCustomer(string customerNumber)
        {
            int customerId = context.CustomerIdsByNumber[customerNumber];
            _lastCommandResult = await deleteCustomerUseCase.ExecuteAsync(new DeleteCustomerCommand(customerId));

            Assert.IsTrue(_lastCommandResult.Value.IsSuccess, _lastCommandResult.Value.Error);
        }

        [Then(@"the customer ""([^""]*)"" is registered successfully")]
        public void ThenTheCustomerIsRegisteredSuccessfully(string customerNumber)
        {
            Assert.IsTrue(_lastCreateResult!.Value.IsSuccess, _lastCreateResult.Value.Error);
            Assert.AreEqual(customerNumber, _lastCreateResult.Value.Value!.CustomerNumber);
        }

        [Then(@"the customer ""([^""]*)"" has the address ""([^""]*)""")]
        public async Task ThenTheCustomerHasTheAddress(string customerNumber, string expectedAddress)
        {
            GetCustomerDetailsResponse details = await GetDetailsAsync(customerNumber);
            Assert.AreEqual(expectedAddress, FormatAddress(details.CurrentAddress!));
        }

        [Then(@"the registration is rejected because the customer number already exists")]
        public void ThenTheRegistrationIsRejectedBecauseTheCustomerNumberAlreadyExists()
        {
            Assert.IsFalse(_lastCreateResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCreateResult.Value.Error, "number");
        }

        [Then(@"the registration is rejected because the email already exists")]
        public void ThenTheRegistrationIsRejectedBecauseTheEmailAlreadyExists()
        {
            Assert.IsFalse(_lastCreateResult!.Value.IsSuccess);
            StringAssert.Contains(_lastCreateResult.Value.Error, "Email");
        }

        [Then(@"the customer ""([^""]*)"" has the name ""([^""]*)"" and email ""([^""]*)""")]
        public async Task ThenTheCustomerHasTheNameAndEmail(string customerNumber, string fullName, string email)
        {
            GetCustomerDetailsResponse details = await GetDetailsAsync(customerNumber);
            Assert.AreEqual(fullName, details.FullName);
            Assert.AreEqual(email, details.Email);
        }

        [Then(@"the search returns exactly the customers named ""([^""]*)""")]
        public void ThenTheSearchReturnsExactlyTheCustomersNamed(string lastName)
        {
            Assert.IsTrue(_lastSearchResult!.Count > 0);
            Assert.IsTrue(_lastSearchResult.All(c => c.FullName.Contains(lastName, StringComparison.Ordinal)));
        }

        [Then(@"customer ""([^""]*)"" can no longer be found")]
        public async Task ThenCustomerCanNoLongerBeFound(string customerNumber)
        {
            int customerId = context.CustomerIdsByNumber[customerNumber];
            Result<GetCustomerDetailsResponse> result = await getCustomerDetailsUseCase.ExecuteAsync(
                new GetCustomerDetailsQuery(customerId));

            Assert.IsFalse(result.IsSuccess);
        }

        private async Task<GetCustomerDetailsResponse> GetDetailsAsync(string customerNumber)
        {
            int customerId = context.CustomerIdsByNumber[customerNumber];
            Result<GetCustomerDetailsResponse> result = await getCustomerDetailsUseCase.ExecuteAsync(
                new GetCustomerDetailsQuery(customerId));

            Assert.IsTrue(result.IsSuccess, result.Error);
            return result.Value!;
        }

        internal static string FormatAddress(CustomerAddressDto address)
            => $"{address.Street} {address.HouseNumber}, {address.PostalCode} {address.City}, {address.CountryCode}";

        internal static (string LastName, string SurName) SplitName(string fullName)
        {
            string[] parts = fullName.Split(' ', 2);
            return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], parts[0]);
        }
    }
}
