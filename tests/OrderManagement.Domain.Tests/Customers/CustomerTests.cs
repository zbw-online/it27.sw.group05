using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;


namespace OrderManagement.Domain.Tests.Customers
{
    [TestClass]
    public class CustomerEquivalenceAndBoundaryTests
    {
        // -----------------------------
        // Helpers
        // -----------------------------
        private static Result<Customer> CreateValidCustomer(
            string customerNr = "CU00001",
            string lastName = "Mueller",
            string surName = "Edi",
            string email = "edi.mueller@example.com",
            string? website = null
            ) => Customer.Create(
                customerNr: customerNr,
                lastName: lastName,
                surName: surName,
                email: email,
                website: website);

        private static string Repeat(char c, int count) => new(c, count);

        // ============================================================
        // 1) Create(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void CreateValidInputsShouldSucceedAndRaiseCreatedEvent()
        {
            // ECP: Valid equivalence class
            Result<Customer> r = CreateValidCustomer();

            Assert.IsTrue(r.IsSuccess);
            Customer c = r.Value!;
            Assert.IsTrue(c.DomainEvents.Count >= 1);
        }


        [TestMethod]
        public void CreateLastNameWhitespaceOnlyShouldFail()
        {
            // ECP: Invalid last name class (empty after trim)
            Result<Customer> r = CreateValidCustomer(lastName: "   ");

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateSurNameWhitespaceOnlyShouldFail()
        {
            // ECP: Invalid surname class (empty after trim)
            Result<Customer> r = CreateValidCustomer(surName: "   ");

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateInvalidEmailShouldFail()
        {
            // ECP: Invalid email class
            Result<Customer> r = CreateValidCustomer(email: "not-an-email");

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 2) Create(...) — Boundary Value Analysis
        // ============================================================

        [TestMethod]
        public void CreateCustomerNumberLengthBoundary7ShouldSucceed()
        {
            // BVA: customer number length = 7 (max valid)
            string nr = "CU00001"; // 7

            Result<Customer> r = CreateValidCustomer(customerNr: nr);

            Assert.IsTrue(r.IsSuccess);
        }

        [TestMethod]
        public void CreateCustomerNumberLengthBoundary8ShouldFail()
        {
            // BVA: customer number length = 8 (just over max)
            string nr = Repeat('A', 8);

            Result<Customer> r = CreateValidCustomer(customerNr: nr);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateWebsiteLengthBoundary255ShouldSucceed()
        {
            // BVA: website length = 255 is allowed
            // Need a valid absolute URL. We'll construct one of length 255.
            // Base: "https://example.com/" length = 20
            // Remaining = 255 - 20 = 235 characters of path.
            string baseUrl = "https://example.com/";
            string path = Repeat('a', 255 - baseUrl.Length);
            string website = baseUrl + path;

            Result<Customer> r = CreateValidCustomer(website: website);

            Assert.IsTrue(r.IsSuccess);
        }

        [TestMethod]
        public void CreateWebsiteLengthBoundary256ShouldFail()
        {
            // BVA: website length = 256 (just over max) should fail
            string baseUrl = "https://example.com/";
            string path = Repeat('a', 256 - baseUrl.Length);
            string website = baseUrl + path;

            Result<Customer> r = CreateValidCustomer(website: website);

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void CreateWebsiteWithoutSchemeAndPathShouldSucceed()
        {
            // ECP: valid website class according to the project requirement.
            Result<Customer> r = CreateValidCustomer(website: "example.com/path");

            Assert.IsTrue(r.IsSuccess);
        }

        [TestMethod]
        public void CreateWebsiteWithWhitespaceInsideDomainShouldFail()
        {
            Result<Customer> r = CreateValidCustomer(website: "exa mple.com");

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 3) ChangeAddress(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void ChangeAddressValidInputsFirstAddressShouldSucceed()
        {
            // ECP: Valid address change class (first address)
            Customer c = CreateValidCustomer().Value!;

            Result r = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zuerich",
                countryCode: "CH");

            Assert.IsTrue(r.IsSuccess);
            Assert.AreEqual(1, c.Addresses.Count);
        }

        [TestMethod]
        public void ChangeAddressInvalidCountryCodeLength3ShouldFail()
        {
            // ECP: Invalid country code class (length != 2)
            Customer c = CreateValidCustomer().Value!;

            Result r = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zuerich",
                countryCode: "CHE");

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void ChangeAddressStreetWhitespaceOnlyShouldFail()
        {
            // ECP: Invalid street class
            Customer c = CreateValidCustomer().Value!;

            Result r = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "   ",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zuerich",
                countryCode: "CH");

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 4) ChangeAddress(...) — Boundary Value Analysis (Country Code)
        // ============================================================

        [TestMethod]
        public void ChangeAddressCountryCodeLengthBoundary2ShouldSucceed()
        {
            // BVA: country code length = 2 (valid boundary)
            Customer c = CreateValidCustomer().Value!;

            Result r = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zuerich",
                countryCode: "CH");

            Assert.IsTrue(r.IsSuccess);
        }

        [TestMethod]
        public void ChangeAddressCountryCodeLengthBoundary1ShouldFail()
        {
            // BVA: country code length = 1 (just below boundary)
            Customer c = CreateValidCustomer().Value!;

            Result r = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zuerich",
                countryCode: "C");

            Assert.IsFalse(r.IsSuccess);
        }

        [TestMethod]
        public void ChangeAddressCountryCodeLengthBoundary3ShouldFail()
        {
            // BVA: country code length = 3 (just above boundary)
            Customer c = CreateValidCustomer().Value!;

            Result r = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zuerich",
                countryCode: "CHE");

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 5) ChangeAddress(...) — Boundary Value Analysis (Date overlap)
        // ============================================================

        [TestMethod]
        public void ChangeAddressOverlapBoundaryCloseDateEqualsValidFromMinusOneShouldSucceedAndClosePrevious()
        {
            // This tests the boundary where the previous address is closed exactly the day before the new one starts.
            Customer c = CreateValidCustomer().Value!;

            Result r1 = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zuerich",
                countryCode: "CH");

            Assert.IsTrue(r1.IsSuccess);

            Result r2 = c.ChangeAddress(
                validFrom: new DateOnly(2025, 02, 01),
                street: "Bahnhofstrasse",
                houseNumber: "10",
                postalCode: "8001",
                city: "Zuerich",
                countryCode: "CH");

            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, c.Addresses.Count);

            CustomerAddress first = c.Addresses.OrderBy(a => a.ValidFrom).First();
            Assert.IsNotNull(first.ValidTo);
            Assert.AreEqual(new DateOnly(2025, 01, 31), first.ValidTo!.Value);
        }



        [TestMethod]
        public void ChangeAddressFutureAddressShouldKeepCurrentAddressActiveUntilFutureValidFrom()
        {
            Customer c = CreateValidCustomer().Value!;
            var today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly currentValidFrom = today.AddMonths(-1);
            DateOnly futureValidFrom = today.AddMonths(1);

            Result r1 = c.ChangeAddress(
                validFrom: currentValidFrom,
                street: "Current Street",
                houseNumber: "1",
                postalCode: "9000",
                city: "St. Gallen",
                countryCode: "CH");

            Result r2 = c.ChangeAddress(
                validFrom: futureValidFrom,
                street: "Future Street",
                houseNumber: "2",
                postalCode: "8000",
                city: "Zurich",
                countryCode: "CH");

            Assert.IsTrue(r1.IsSuccess, r1.Error);
            Assert.IsTrue(r2.IsSuccess, r2.Error);

            CustomerAddress? current = c.AddressAt(today);
            Assert.IsNotNull(current);
            Assert.AreEqual("Current Street", current.Street);

            CustomerAddress future = c.Addresses.Single(a => a.ValidFrom == futureValidFrom);
            Assert.AreEqual("Future Street", future.Street);

            CustomerAddress oldCurrent = c.Addresses.Single(a => a.ValidFrom == currentValidFrom);
            Assert.AreEqual(futureValidFrom.AddDays(-1), oldCurrent.ValidTo);
        }

        [TestMethod]
        public void ChangeAddressOverlapBoundaryNewValidFromBeforeCurrentValidFromShouldFail()
        {
            // BVA: closeDate < active.ValidFrom should fail (overlap invalid)
            // active.ValidFrom = 2025-01-10
            // new validFrom = 2025-01-05 => closeDate = 2025-01-04 which is < 2025-01-10 => invalid
            Customer c = CreateValidCustomer().Value!;

            Result r1 = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 10),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zuerich",
                countryCode: "CH");

            Assert.IsTrue(r1.IsSuccess);

            Result r2 = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 05),
                street: "Bahnhofstrasse",
                houseNumber: "10",
                postalCode: "8001",
                city: "Zuerich",
                countryCode: "CH");

            Assert.IsFalse(r2.IsSuccess);
        }
    }
}
