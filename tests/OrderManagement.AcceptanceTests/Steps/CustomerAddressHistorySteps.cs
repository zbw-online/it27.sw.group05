using System.Globalization;

using OrderManagement.AcceptanceTests.Support;
using OrderManagement.Application.Features.Customers.AddCustomerAddress;
using OrderManagement.Application.Features.Customers.CreateCustomer;
using OrderManagement.Application.Features.Customers.GetCustomerDetails;

using Reqnroll;

using SharedKernel.Primitives;

namespace OrderManagement.AcceptanceTests.Steps
{
    [Binding]
    public sealed class CustomerAddressHistorySteps(
        ICreateCustomerUseCase createCustomerUseCase,
        IAddCustomerAddressUseCase addCustomerAddressUseCase,
        IGetCustomerDetailsUseCase getCustomerDetailsUseCase,
        AcceptanceTestContext context)
    {
        private Result? _lastAddressResult;
        private GetCustomerDetailsResponse? _lastDetails;

        [Given(@"a customer ""([^""]*)"" is registered with address ""([^""]*)"" valid from ""([^""]*)""")]
        public async Task GivenACustomerIsRegisteredWithAddressValidFrom(string customerNumber, string address, string validFrom)
        {
            var parsed = AddressText.Parse(address);

            Result<CreateCustomerResponse> result = await createCustomerUseCase.ExecuteAsync(new CreateCustomerCommand(
                customerNumber, "Doe", "Jane", $"{customerNumber.ToLowerInvariant()}@example.com", null,
                DateOnly.Parse(validFrom, CultureInfo.InvariantCulture), parsed.Street, parsed.HouseNumber, parsed.PostalCode, parsed.City, parsed.CountryCode));

            Assert.IsTrue(result.IsSuccess, result.Error);
            context.CustomerIdsByNumber[customerNumber] = result.Value!.CustomerId;
        }

        [Given(@"customer ""([^""]*)"" moved to ""([^""]*)"" valid from ""([^""]*)""")]
        public async Task GivenCustomerMovedTo(string customerNumber, string address, string validFrom)
        {
            var parsed = AddressText.Parse(address);
            int customerId = context.CustomerIdsByNumber[customerNumber];

            Result result = await addCustomerAddressUseCase.ExecuteAsync(new AddCustomerAddressCommand(
                customerId, DateOnly.Parse(validFrom, CultureInfo.InvariantCulture), parsed.Street, parsed.HouseNumber, parsed.PostalCode, parsed.City, parsed.CountryCode));

            Assert.IsTrue(result.IsSuccess, result.Error);
        }

        [When(@"I add a future address ""([^""]*)"" for customer ""([^""]*)"" valid from ""([^""]*)""")]
        public async Task WhenIAddAFutureAddressForCustomer(string address, string customerNumber, string validFrom)
        {
            var parsed = AddressText.Parse(address);
            int customerId = context.CustomerIdsByNumber[customerNumber];

            _lastAddressResult = await addCustomerAddressUseCase.ExecuteAsync(new AddCustomerAddressCommand(
                customerId, DateOnly.Parse(validFrom, CultureInfo.InvariantCulture), parsed.Street, parsed.HouseNumber, parsed.PostalCode, parsed.City, parsed.CountryCode));
        }

        [When(@"I view the address history for customer ""([^""]*)""")]
        public async Task WhenIViewTheAddressHistoryForCustomer(string customerNumber)
        {
            int customerId = context.CustomerIdsByNumber[customerNumber];
            Result<GetCustomerDetailsResponse> result = await getCustomerDetailsUseCase.ExecuteAsync(
                new GetCustomerDetailsQuery(customerId));

            Assert.IsTrue(result.IsSuccess, result.Error);
            _lastDetails = result.Value;
        }

        [Then(@"customer ""([^""]*)"" has (\d+) future address(?:es)?")]
        public async Task ThenCustomerHasFutureAddresses(string customerNumber, int expectedCount)
        {
            GetCustomerDetailsResponse details = await GetDetailsAsync(customerNumber);
            Assert.AreEqual(expectedCount, details.FutureAddresses.Count);
        }

        [Then(@"customer ""([^""]*)"" has (\d+) previous address(?:es)?")]
        public void ThenCustomerHasPreviousAddresses(string customerNumber, int expectedCount)
        {
            _ = customerNumber;
            Assert.AreEqual(expectedCount, _lastDetails!.PreviousAddresses.Count);
        }

        [Then(@"the future address for customer ""([^""]*)"" is ""([^""]*)""")]
        public async Task ThenTheFutureAddressForCustomerIs(string customerNumber, string expectedAddress)
        {
            GetCustomerDetailsResponse details = await GetDetailsAsync(customerNumber);
            Assert.AreEqual(expectedAddress, CustomerManagementSteps.FormatAddress(details.FutureAddresses.Single()));
        }

        [Then(@"the current address for customer ""([^""]*)"" is ""([^""]*)""")]
        public void ThenTheCurrentAddressForCustomerIs(string customerNumber, string expectedAddress)
        {
            _ = customerNumber;
            Assert.AreEqual(expectedAddress, CustomerManagementSteps.FormatAddress(_lastDetails!.CurrentAddress!));
        }

        [Then(@"the address change is rejected because it overlaps the existing address")]
        public void ThenTheAddressChangeIsRejectedBecauseItOverlapsTheExistingAddress()
        {
            Assert.IsFalse(_lastAddressResult!.Value.IsSuccess);
            StringAssert.Contains(_lastAddressResult.Value.Error, "overlaps");
        }

        private async Task<GetCustomerDetailsResponse> GetDetailsAsync(string customerNumber)
        {
            int customerId = context.CustomerIdsByNumber[customerNumber];
            Result<GetCustomerDetailsResponse> result = await getCustomerDetailsUseCase.ExecuteAsync(
                new GetCustomerDetailsQuery(customerId));

            Assert.IsTrue(result.IsSuccess, result.Error);
            return result.Value!;
        }
    }
}
