using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.Shared;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class OrderDraftCalculatorTests
    {
        [TestMethod]
        public void Calculate_WithNoLines_ShouldReturnZeroTotals()
        {
            Result<OrderDraftTotalsDto> result = OrderDraftCalculator.Calculate([]);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(0m, result.Value!.Subtotal);
            Assert.AreEqual(0m, result.Value!.VatAmount);
            Assert.AreEqual(0m, result.Value!.Total);
        }

        [TestMethod]
        public void Calculate_WithSingleLine_ShouldSumLineTotalAndVat()
        {
            OrderDraftLineInput[] lines = [new OrderDraftLineInput(2.80m, "CHF", 20, 8.1m)];

            Result<OrderDraftTotalsDto> result = OrderDraftCalculator.Calculate(lines);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(56.00m, result.Value!.Subtotal);
            Assert.AreEqual(4.54m, result.Value!.VatAmount);
            Assert.AreEqual(60.54m, result.Value!.Total);
            Assert.AreEqual("CHF", result.Value!.Currency);
        }

        [TestMethod]
        public void Calculate_WithMultipleLines_ShouldAccumulateAcrossLines()
        {
            OrderDraftLineInput[] lines =
            [
                new OrderDraftLineInput(2.80m, "CHF", 20, 8.1m),
                new OrderDraftLineInput(8.90m, "CHF", 5, 8.1m)
            ];

            Result<OrderDraftTotalsDto> result = OrderDraftCalculator.Calculate(lines);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(100.50m, result.Value!.Subtotal);
        }

        [TestMethod]
        public void Calculate_WithMixedCurrencies_ShouldFail()
        {
            OrderDraftLineInput[] lines =
            [
                new OrderDraftLineInput(2.80m, "CHF", 1, 0m),
                new OrderDraftLineInput(3.10m, "EUR", 1, 0m)
            ];

            Result<OrderDraftTotalsDto> result = OrderDraftCalculator.Calculate(lines);

            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void Calculate_WithNonPositiveQuantity_ShouldFail()
        {
            OrderDraftLineInput[] lines = [new OrderDraftLineInput(2.80m, "CHF", 0, 0m)];

            Result<OrderDraftTotalsDto> result = OrderDraftCalculator.Calculate(lines);

            Assert.IsFalse(result.IsSuccess);
        }
    }
}
