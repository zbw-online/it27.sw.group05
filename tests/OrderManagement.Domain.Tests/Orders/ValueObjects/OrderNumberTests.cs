using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Tests.Domain.Orders.ValueObjects
{
    [TestClass]
    public sealed class OrderNumberTests
    {
        [TestMethod]
        [DataRow("ORD-2026-001")]
        [DataRow("ORD-1999-999")]
        [DataRow("ORD-2026-000")]
        public void Create_WithValidValue_Succeeds(string value)
        {
            Result<OrderNumber> result = OrderNumber.Create(value);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(value, result.Value!.Value);
        }

        [TestMethod]
        public void Create_WithLowercaseInput_NormalizesToUppercase()
        {
            Result<OrderNumber> result = OrderNumber.Create("ord-2026-001");

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("ORD-2026-001", result.Value!.Value);
        }

        [TestMethod]
        public void Create_WithLeadingAndTrailingWhitespace_TrimsBeforeValidating()
        {
            Result<OrderNumber> result = OrderNumber.Create("  ORD-2026-001  ");

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("ORD-2026-001", result.Value!.Value);
        }

        [TestMethod]
        public void Create_WithNullInput_ReturnsRequiredFailure()
        {
            Result<OrderNumber> result = OrderNumber.Create(null);

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "required");
        }

        [TestMethod]
        public void Create_WithWhitespaceOnlyInput_ReturnsRequiredFailure()
        {
            Result<OrderNumber> result = OrderNumber.Create("   ");

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "required");
        }

        [TestMethod]
        [DataRow("ORD-PW-DELETE-001", DisplayName = "Alphabetic segment instead of year and sequence")]
        [DataRow("ORD-ABCD-001", DisplayName = "Alphabetic year")]
        [DataRow("ORD-2026-ABC", DisplayName = "Alphabetic sequence")]
        [DataRow("ORD-26-001", DisplayName = "Too few year digits")]
        [DataRow("ORD-20266-001", DisplayName = "Too many year digits")]
        [DataRow("ORD-2026-01", DisplayName = "Too few sequence digits")]
        [DataRow("ORD-2026-0001", DisplayName = "Too many sequence digits")]
        [DataRow("ORD-2026001", DisplayName = "Missing separator")]
        [DataRow("XYZ-2026-001", DisplayName = "Wrong prefix")]
        [DataRow("ORD-2026-001-EXTRA", DisplayName = "Trailing extra segment")]
        public void Create_WithInvalidFormat_ReturnsFormatFailure(string value)
        {
            Result<OrderNumber> result = OrderNumber.Create(value);

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error!, "ORD-YYYY-NNN");
        }

        [TestMethod]
        public void Create_ErrorMessage_DoesNotImplyOnly2025IsValid()
        {
            Result<OrderNumber> result = OrderNumber.Create("INVALID");

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(result.Error!.Contains("2025"), "The validation message should not imply that only the year 2025 is valid.");
        }
    }
}
