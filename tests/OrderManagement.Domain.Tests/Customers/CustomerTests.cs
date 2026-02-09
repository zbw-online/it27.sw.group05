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
            int id = 1,
            string customerNr = "C-00001",
            string lastName = "Mueller",
            string surName = "Edi",
            string email = "edi.mueller@example.com",
            string? website = null,
            string passwordHash = "hash") => Customer.Create(
                id: id,
                customerNr: customerNr,
                lastName: lastName,
                surName: surName,
                email: email,
                website: website,
                passwordHash: passwordHash);

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
        public void CreateInvalidIdNegativeShouldFail()
        {
            // ECP: Invalid id class (id < 0)
            Result<Customer> r = CreateValidCustomer(id: -1);

            Assert.IsFalse(r.IsSuccess);
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

        [TestMethod]
        public void CreatePasswordHashEmptyShouldFail()
        {
            // ECP: Invalid password hash class (null/whitespace (here empty))
            Result<Customer> r = CreateValidCustomer(passwordHash: "");

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 2) Create(...) — Boundary Value Analysis
        // ============================================================

        [TestMethod]
        public void CreateCustomerNumberLengthBoundary7ShouldSucceed()
        {
            // BVA: customer number length = 7 (max valid)
            string nr = "C-00001"; // 7

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
        public void CreateWebsiteNotAbsoluteUrlShouldFail()
        {
            // ECP: invalid website class (not absolute)
            Result<Customer> r = CreateValidCustomer(website: "example.com/path");

            Assert.IsFalse(r.IsSuccess);
        }

        // ============================================================
        // 3) ChangeAddress(...) — Equivalence Classes
        // ============================================================

        [TestMethod]
        public void ChangeAddressValidInputsFirstAddressShouldSucceed()
        {
            // ECP: Valid address change class (first address)
            Customer c = CreateValidCustomer(id: 1).Value!;

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
            Customer c = CreateValidCustomer(id: 1).Value!;

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
            Customer c = CreateValidCustomer(id: 1).Value!;

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
            Customer c = CreateValidCustomer(id: 1).Value!;

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
            Customer c = CreateValidCustomer(id: 1).Value!;

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
            Customer c = CreateValidCustomer(id: 1).Value!;

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
            Customer c = CreateValidCustomer(id: 1).Value!;

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
        public void ChangeAddressOverlapBoundaryNewValidFromBeforeCurrentValidFromShouldFail()
        {
            // BVA: closeDate < active.ValidFrom should fail (overlap invalid)
            // active.ValidFrom = 2025-01-10
            // new validFrom = 2025-01-05 => closeDate = 2025-01-04 which is < 2025-01-10 => invalid
            Customer c = CreateValidCustomer(id: 1).Value!;

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
