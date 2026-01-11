using OrderManagement.Domain.Customers;


namespace OrderManagement.Domain.Tests.Customers
{


    [TestClass]
    public class CustomerTests
    {
        [TestMethod]
        public void CreateShouldRaiseCustomerCreatedEvent()
        {
            SharedKernel.Primitives.Result<Customer> r = Customer.Create(
                id: 1,
                customerNr: "C-00001",
                lastName: "Müller",
                surName: "Edi",
                email: "edi.mueller@example.com");

            Assert.IsTrue(r.IsSuccess);

            Customer c = r.Value!;
            Assert.AreEqual(1, c.Id.Value);
            Assert.AreEqual("C-00001", c.CustomerNumber.Value);
            Assert.AreEqual(1, c.DomainEvents.Count);
        }

        [TestMethod]
        public void ChangeAddressShouldClosePreviousAddressAndAddNew()
        {
            Customer c = Customer.Create(1, "C-00001", "Müller", "Edi", "edi.mueller@example.com").Value!;

            SharedKernel.Primitives.Result r1 = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zürich",
                countryCode: "CH");

            Assert.IsTrue(r1.IsSuccess);
            Assert.AreEqual(1, c.Addresses.Count);

            SharedKernel.Primitives.Result r2 = c.ChangeAddress(
                validFrom: new DateOnly(2025, 02, 01),
                street: "Bahnhofstrasse",
                houseNumber: "10",
                postalCode: "8001",
                city: "Zürich",
                countryCode: "CH");

            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, c.Addresses.Count);

            CustomerAddress first = c.Addresses.OrderBy(a => a.ValidFrom).First();
            Assert.IsNotNull(first.ValidTo);
            Assert.AreEqual(new DateOnly(2025, 01, 31), first.ValidTo.Value);

            CustomerAddress? current = c.CurrentAddress(new DateOnly(2025, 02, 15));
            Assert.IsNotNull(current);
            Assert.AreEqual("Bahnhofstrasse", current!.Street);

            // Should have raised an address-changed event
            Assert.IsTrue(c.DomainEvents.Count >= 2);
        }

        [TestMethod]
        public void ChangeAddressShouldFailOnInvalidCountryCode()
        {
            Customer c = Customer.Create(1, "C-00001", "Müller", "Edi", "edi.mueller@example.com").Value!;
            SharedKernel.Primitives.Result r = c.ChangeAddress(
                validFrom: new DateOnly(2025, 01, 01),
                street: "Seestrasse",
                houseNumber: "55a",
                postalCode: "8002",
                city: "Zürich",
                countryCode: "CHE"); // invalid

            Assert.IsFalse(r.IsSuccess);
        }
    }
}
