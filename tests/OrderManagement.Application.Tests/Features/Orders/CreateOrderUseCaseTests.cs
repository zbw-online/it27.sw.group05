using OrderManagement.Application.Features.Orders.CreateOrder;
using OrderManagement.Application.Tests.Fakes;
using OrderManagement.Application.Tests.Fakes.Catalog;
using OrderManagement.Application.Tests.Fakes.Customers;
using OrderManagement.Application.Tests.Fakes.Orders;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Tests.Features.Orders
{
    [TestClass]
    public sealed class CreateOrderUseCaseTests
    {
        private static readonly DateOnly DeliveryDate = new(2026, 9, 1);

        private static CreateOrderCommand ValidCommand(int customerId, IReadOnlyList<CreateOrderLineInput> lines, string orderNumber = "ORD-2026-001")
            => new(
                orderNumber,
                customerId,
                DeliveryDate,
                null,
                new AddressOverrideInput("Main Street", "1", "8000", "Zurich", "CH"),
                new AddressOverrideInput("Main Street", "1", "8000", "Zurich", "CH"),
                lines);

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
        public async Task ExecuteAsync_WithValidLine_ShouldMarkInventoryApplied()
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
            Order order = orderCommandRepository.Added.Single();
            Assert.IsTrue(order.IsInventoryApplied);
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
        public async Task ExecuteAsync_WithDeactivatedArticle_ShouldFailAndNotCommit()
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
            article.Deactivate().EnsureSuccess();

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(
                ValidCommand(customer.Id.Value, [new CreateOrderLineInput(article.Id.Value, 1)]));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "deaktiviert");
            Assert.AreEqual(0, orderCommandRepository.Added.Count);
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

            _ = orderQueryRepository.Seed(Order.Create(
                "ORD-2026-001",
                customer.Id,
                DeliveryDate,
                Address.Create("Old Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Old Street", "1", "8000", "Zurich", "CH").EnsureValue(),
                AddressSource.Automatic)
                .EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(ValidCommand(customer.Id.Value, []));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "already exists");
            Assert.AreEqual(0, orderCommandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithNoLines_ShouldFailAndNotCommit()
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

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(ValidCommand(customer.Id.Value, []));

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "mindestens eine gültige Position");
            Assert.AreEqual(0, orderCommandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithoutOverride_ResolvesAddressValidOnDeliveryDate()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 1, 1), "Old Street", "1", "8000", "Zurich", "CH").EnsureSuccess();
            customer.ChangeAddress(new DateOnly(2026, 9, 1), "New Street", "2", "9000", "St. Gallen", "CH").EnsureSuccess();
            _ = customerQueryRepository.Seed(customer);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            var command = new CreateOrderCommand(
                "ORD-2026-050",
                customer.Id.Value,
                new DateOnly(2026, 8, 31),
                null,
                null,
                null,
                [new CreateOrderLineInput(article.Id.Value, 1)]);

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(command);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Order order = orderCommandRepository.Added.Single();
            Assert.AreEqual("Old Street", order.BillingAddress.Street);
            Assert.AreEqual("Old Street", order.DeliveryAddress.Street);
            Assert.AreEqual(AddressSource.Automatic, order.BillingAddressSource);
            Assert.AreEqual(AddressSource.Automatic, order.DeliveryAddressSource);

            CreateOrderCommand commandOnNewAddress = command with { OrderNumber = "ORD-2026-051", DeliveryDate = new DateOnly(2026, 9, 1) };
            Result<CreateOrderResponse> secondResult = await useCase.ExecuteAsync(commandOnNewAddress);

            Assert.IsTrue(secondResult.IsSuccess, secondResult.Error);
            Order secondOrder = orderCommandRepository.Added.Last();
            Assert.AreEqual("New Street", secondOrder.BillingAddress.Street);
            Assert.AreEqual("New Street", secondOrder.DeliveryAddress.Street);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithNoAddressValidOnDeliveryDateAndNoOverride_ShouldFailWithBusinessMessage()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue();
            _ = customerQueryRepository.Seed(customer);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            var command = new CreateOrderCommand(
                "ORD-2026-060",
                customer.Id.Value,
                new DateOnly(2026, 9, 1),
                null,
                null,
                null,
                [new CreateOrderLineInput(article.Id.Value, 1)]);

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(command);

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.Error, "keine gültige Kundenadresse hinterlegt");
            Assert.AreEqual(0, orderCommandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDifferentBillingAndDeliveryOverrides_StoresBothIndependently()
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

            var command = new CreateOrderCommand(
                "ORD-2026-070",
                customer.Id.Value,
                DeliveryDate,
                "Projekt XY",
                new AddressOverrideInput("Rechnungsweg", "1", "8000", "Zurich", "CH"),
                new AddressOverrideInput("Lieferweg", "2", "9000", "St. Gallen", "CH"),
                [new CreateOrderLineInput(article.Id.Value, 1)]);

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(command);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Order order = orderCommandRepository.Added.Single();
            Assert.AreEqual("Rechnungsweg", order.BillingAddress.Street);
            Assert.AreEqual("Lieferweg", order.DeliveryAddress.Street);
            Assert.AreEqual(AddressSource.Manual, order.BillingAddressSource);
            Assert.AreEqual(AddressSource.Manual, order.DeliveryAddressSource);
            Assert.AreEqual("Projekt XY", order.CustomerReference);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithBillingOverrideOnly_ShouldKeepDeliveryAddressAutomatic()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 1, 1), "Customer Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            _ = customerQueryRepository.Seed(customer);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            var command = new CreateOrderCommand(
                "ORD-2026-071",
                customer.Id.Value,
                DeliveryDate,
                null,
                new AddressOverrideInput("Billing Only Street", "1", "8000", "Zurich", "CH"),
                null,
                [new CreateOrderLineInput(article.Id.Value, 1)]);

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(command);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Order order = orderCommandRepository.Added.Single();
            Assert.AreEqual("Billing Only Street", order.BillingAddress.Street);
            Assert.AreEqual("Customer Street", order.DeliveryAddress.Street);
            Assert.AreEqual(AddressSource.Manual, order.BillingAddressSource);
            Assert.AreEqual(AddressSource.Automatic, order.DeliveryAddressSource);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDeliveryOverrideOnly_ShouldKeepBillingAddressAutomatic()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 1, 1), "Customer Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            _ = customerQueryRepository.Seed(customer);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            var command = new CreateOrderCommand(
                "ORD-2026-072",
                customer.Id.Value,
                DeliveryDate,
                null,
                null,
                new AddressOverrideInput("Delivery Only Street", "2", "3000", "Bern", "CH"),
                [new CreateOrderLineInput(article.Id.Value, 1)]);

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(command);

            Assert.IsTrue(result.IsSuccess, result.Error);
            Order order = orderCommandRepository.Added.Single();
            Assert.AreEqual("Customer Street", order.BillingAddress.Street);
            Assert.AreEqual("Delivery Only Street", order.DeliveryAddress.Street);
            Assert.AreEqual(AddressSource.Automatic, order.BillingAddressSource);
            Assert.AreEqual(AddressSource.Manual, order.DeliveryAddressSource);
        }

        [TestMethod]
        public async Task ExecuteAsync_CustomerAddressChangedAfterOrderCreation_ShouldNotMutateExistingOrderSnapshot()
        {
            var orderCommandRepository = new FakeOrderCommandRepository();
            var orderQueryRepository = new FakeOrderQueryRepository();
            var customerQueryRepository = new FakeCustomerQueryRepository();
            var articleCommandRepository = new FakeArticleCommandRepository();
            var unitOfWork = new FakeUnitOfWork();
            var useCase = new CreateOrderUseCase(
                orderCommandRepository, orderQueryRepository, customerQueryRepository, articleCommandRepository, unitOfWork);

            Customer customer = Customer.Create("CU00001", "Doe", "Jane", "jane@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 1, 1), "Original Street", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            _ = customerQueryRepository.Seed(customer);

            Article article = articleCommandRepository.Seed(
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());

            var command = new CreateOrderCommand(
                "ORD-2026-073",
                customer.Id.Value,
                DeliveryDate,
                null,
                null,
                null,
                [new CreateOrderLineInput(article.Id.Value, 1)]);

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(command);
            Assert.IsTrue(result.IsSuccess, result.Error);
            Order order = orderCommandRepository.Added.Single();
            Assert.AreEqual("Original Street", order.BillingAddress.Street);
            Assert.AreEqual("Original Street", order.DeliveryAddress.Street);

            customer.ChangeAddress(DeliveryDate, "Moved-To Street", "9", "1000", "Lausanne", "CH").EnsureSuccess();

            Assert.AreEqual("Original Street", order.BillingAddress.Street);
            Assert.AreEqual("Original Street", order.DeliveryAddress.Street);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithRequestedQuantityExactlyEqualToStock_ShouldSucceedAndLeaveStockAtZero()
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
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 5).EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(
                ValidCommand(customer.Id.Value, [new CreateOrderLineInput(article.Id.Value, 5)]));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(0, article.Stock);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithSecondLineInvalid_ShouldNotCommitAnyChangeFromFirstLine()
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
                Article.Create("ART-001", "Widget", 9.99m, "CHF", new ArticleGroupId(1), stock: 10).EnsureValue());
            Article second = articleCommandRepository.Seed(
                Article.Create("ART-002", "Gadget", 5.00m, "CHF", new ArticleGroupId(1), stock: 2).EnsureValue());

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(ValidCommand(
                customer.Id.Value,
                [new CreateOrderLineInput(first.Id.Value, 3), new CreateOrderLineInput(second.Id.Value, 5)]));

            // The use case only ever calls SaveChanges once, at the very end. Since the second
            // line fails before that point is reached, nothing - including the first line's
            // already-applied in-memory stock change - is ever written to the database.
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(0, orderCommandRepository.Added.Count);
            Assert.AreEqual(0, unitOfWork.CommitCount);
        }

        [TestMethod]
        public async Task ExecuteAsync_WithDuplicateArticleLines_ShouldDeductCombinedQuantityConsistently()
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

            Result<CreateOrderResponse> result = await useCase.ExecuteAsync(ValidCommand(
                customer.Id.Value,
                [new CreateOrderLineInput(article.Id.Value, 2), new CreateOrderLineInput(article.Id.Value, 3)]));

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.AreEqual(5, article.Stock);
            Order order = orderCommandRepository.Added.Single();
            Assert.AreEqual(2, order.Lines.Count);
            Assert.AreEqual(5, order.Lines.Sum(l => l.Quantity));
        }
    }
}
