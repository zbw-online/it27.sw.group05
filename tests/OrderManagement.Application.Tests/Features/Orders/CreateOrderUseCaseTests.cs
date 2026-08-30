using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Features.Orders.CreateOrder;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class CreateOrderUseCaseTests
    {
        private static CreateOrderCommand ValidCommand(int customerId, IReadOnlyList<CreateOrderLineInput> lines, string orderNumber = "ORD-2026-001")
            => new(orderNumber, customerId, "Main Street", "1", "8000", "Zurich", "CH", lines);

        [TestMethod]
        public async Task ExecuteAsync_WithExistingCustomerAndOneValidArticle_ShouldPersistOrderAndCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(
                ValidCommand(customer.Id.Value, [new CreateOrderLineInput(article.Id.Value, 2)]));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual("ORD-2026-001", result.Value!.OrderNumber);
            Assert.AreEqual(19.98m, result.Value.TotalAmount);
            Assert.AreEqual(1, orderCommandRepository.Added.Count);
            Assert.AreEqual(1, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithValidLine_ShouldReduceArticleStock()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(
                ValidCommand(customer.Id.Value, [new CreateOrderLineInput(article.Id.Value, 3)]));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(7, article.Stock);
            Assert.AreEqual(1, articleCommandRepository.Updated.Count);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithQuantityExceedingStock_ShouldFailAndNotCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 2).EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(
                ValidCommand(customer.Id.Value, [new CreateOrderLineInput(article.Id.Value, 5)]));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "stock");
            Assert.AreEqual(2, article.Stock);
            Assert.AreEqual(0, orderCommandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithMultipleLines_ShouldSumAllLinesIntoTotal()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());
            Article first = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 10m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());
            Article second = articleCommandRepository.Seed(
                Article.Create("ART-002", "Gadget", 5m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(ValidCommand(
                customer.Id.Value,
                [new CreateOrderLineInput(first.Id.Value, 2), new CreateOrderLineInput(second.Id.Value, 3)]));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(35m, result.Value!.TotalAmount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithZeroQuantityLine_ShouldFailAndNotCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());
            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1)).EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(
                ValidCommand(customer.Id.Value, [new CreateOrderLineInput(article.Id.Value, 0)]));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "Quantity must be positive");
            Assert.AreEqual(0, orderCommandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownCustomer_ShouldFail()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(ValidCommand(999, []));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "Customer");
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithUnknownArticle_ShouldFailAndNotCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(
                ValidCommand(customer.Id.Value, [new CreateOrderLineInput(999, 1)]));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "Article");
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDuplicateOrderNumber_ShouldFailAndNotCommit()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = customerQueryRepository.Seed(
                Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue());

            _ = orderQueryRepository.Seed(Domain.Orders.Order.Create(
                "ORD-2026-001",
                customer.Id,
                Address.Create("Old Street", "1", "8000", "Zurich", "CH").EnsureValue())
                .EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(ValidCommand(customer.Id.Value, []));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "already exists");
            Assert.AreEqual(0, orderCommandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }
    }
}
