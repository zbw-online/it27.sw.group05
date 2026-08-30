using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Features.Orders.GetQuarterlyKpis;
using OrderManagement.Application.Tests.Fakes.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class GetQuarterlyKpisUseCaseTests
    {
        [TestMethod]
        public async Task ExecuteAsync_ShouldReturnRowsFromRepository()
        {
            var repository = new FakeQuarterlyKpiQueryRepository
            {
                Rows =
                [
                    new QuarterlyKpiRowDto { Category = "Gesamtumsatz", Year = 2026, Quarter = 2, Value = 720000m }
                ]
            };
            var useCase = new GetQuarterlyKpisUseCase(repository);

            Result<IReadOnlyList<QuarterlyKpiRowDto>> result = await useCase.ExecuteAsync(new GetQuarterlyKpisQuery());

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(1, result.Value!.Count);
            Assert.AreEqual("Gesamtumsatz", result.Value[0].Category);
        }
    }
}
