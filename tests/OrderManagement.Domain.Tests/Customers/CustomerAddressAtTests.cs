using OrderManagement.Domain.Customers;

namespace OrderManagement.Domain.Tests.Customers
{
    [TestClass]
    public sealed class CustomerAddressAtTests
    {
        private static Customer CreateCustomerWithOldAndNewAddress()
        {
            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 1, 1), "Old Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(new DateOnly(2026, 9, 1), "New Street", "2", "8000", "Zurich", "CH").EnsureSuccess();
            return customer;
        }

        [TestMethod]
        public void AddressAt_LastDayOldAddressIsValid_ShouldReturnOldAddress()
        {
            Customer customer = CreateCustomerWithOldAndNewAddress();

            CustomerAddress? address = customer.AddressAt(new DateOnly(2026, 8, 31));

            Assert.IsNotNull(address);
            Assert.AreEqual("Old Street", address.Street);
        }

        [TestMethod]
        public void AddressAt_FirstDayNewAddressIsValid_ShouldReturnNewAddress()
        {
            Customer customer = CreateCustomerWithOldAndNewAddress();

            CustomerAddress? address = customer.AddressAt(new DateOnly(2026, 9, 1));

            Assert.IsNotNull(address);
            Assert.AreEqual("New Street", address.Street);
        }

        [TestMethod]
        public void AddressAt_LaterDateAfterNewAddressStarts_ShouldReturnNewAddress()
        {
            Customer customer = CreateCustomerWithOldAndNewAddress();

            CustomerAddress? address = customer.AddressAt(new DateOnly(2026, 9, 6));

            Assert.IsNotNull(address);
            Assert.AreEqual("New Street", address.Street);
        }

        [TestMethod]
        public void AddressAt_ExactValidFromBoundary_ShouldReturnAddress()
        {
            Customer customer = Customer.Create("CU00002", "Doe", "John", "john@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 3, 15), "Street A", "1", "9000", "St. Gallen", "CH").EnsureSuccess();

            CustomerAddress? address = customer.AddressAt(new DateOnly(2026, 3, 15));

            Assert.IsNotNull(address);
        }

        [TestMethod]
        public void AddressAt_ExactValidToBoundary_ShouldReturnClosedAddress()
        {
            Customer customer = Customer.Create("CU00003", "Doe", "Jill", "jill@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 1, 1), "Old Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(new DateOnly(2026, 6, 1), "New Street", "2", "8000", "Zurich", "CH").EnsureSuccess();

            CustomerAddress? address = customer.AddressAt(new DateOnly(2026, 5, 31));

            Assert.IsNotNull(address);
            Assert.AreEqual("Old Street", address.Street);
        }

        [TestMethod]
        public void AddressAt_BeforeEarliestAddressValidFrom_ShouldReturnNull()
        {
            Customer customer = Customer.Create("CU00004", "Doe", "Jack", "jack@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 6, 1), "Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();

            CustomerAddress? address = customer.AddressAt(new DateOnly(2026, 5, 31));

            Assert.IsNull(address);
        }

        [TestMethod]
        public void AddressAt_CustomerWithNoAddresses_ShouldReturnNull()
        {
            Customer customer = Customer.Create("CU00005", "Doe", "Jim", "jim@example.com", null).EnsureValue();

            CustomerAddress? address = customer.AddressAt(new DateOnly(2026, 1, 1));

            Assert.IsNull(address);
        }

        [TestMethod]
        public void AddressAt_WithTwoFutureAddresses_ShouldReturnPeriodMatchingDate()
        {
            Customer customer = Customer.Create("CU00006", "Doe", "June", "june@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 1, 1), "Current Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(new DateOnly(2026, 9, 1), "First Future Street", "2", "8000", "Zurich", "CH").EnsureSuccess();
            customer.ChangeAddress(new DateOnly(2026, 12, 1), "Second Future Street", "3", "3000", "Bern", "CH").EnsureSuccess();

            Assert.AreEqual("Current Street", customer.AddressAt(new DateOnly(2026, 8, 31))!.Street);
            Assert.AreEqual("First Future Street", customer.AddressAt(new DateOnly(2026, 9, 1))!.Street);
            Assert.AreEqual("First Future Street", customer.AddressAt(new DateOnly(2026, 11, 30))!.Street);
            Assert.AreEqual("Second Future Street", customer.AddressAt(new DateOnly(2026, 12, 1))!.Street);
        }
    }
}
