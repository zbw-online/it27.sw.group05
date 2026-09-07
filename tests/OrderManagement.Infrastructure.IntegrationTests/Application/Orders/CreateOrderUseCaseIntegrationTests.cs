using OrderManagement.Application.Features.Orders.CreateOrder;
using OrderManagement.Domain.Catalog;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Application.Orders
{
    [TestClass]
    public sealed class CreateOrderUseCaseIntegrationTests : IntegrationTestBase
    {
        private OrderCommandRepository _orderCommandRepository = default!;
        private OrderQueryRepository _orderQueryRepository = default!;
        private CustomerQueryRepository _customerQueryRepository = default!;
        private ArticleCommandRepository _articleCommandRepository = default!;
        private UnitOfWork _unitOfWork = default!;
        private CreateOrderUseCase _useCase = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _orderCommandRepository = new OrderCommandRepository(DbContext);
            _orderQueryRepository = new OrderQueryRepository(DbContext);
            _customerQueryRepository = new CustomerQueryRepository(DbContext);
            _articleCommandRepository = new ArticleCommandRepository(DbContext);
            _unitOfWork = new UnitOfWork(DbContext);
            _useCase = new CreateOrderUseCase(
                _orderCommandRepository, _orderQueryRepository, _customerQueryRepository, _articleCommandRepository, _unitOfWork);
            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task ExecuteAsync_CreatingTwoOrdersForTheSameArticleInTheSameDbContext_ShouldNotFailOnTheSecondOrder()
        {
            // Regression test: Article.Price is a single tracked Money instance once the article has
            // been loaded once in this DbContext. If CreateOrderUseCase passed that instance straight
            // through as a second order line's UnitPrice, EF Core's owned-type tracking would reject it
            // ("part of a key and so cannot be modified") the moment a second order reuses the article.
            // CreatePersistedCustomerAsync already registers an active address valid from 2026-01-01.
            Domain.Customers.Customer customer = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(DbContext);

            Article article = await InfrastructureTestDataFactory.CreatePersistedArticleAsync(DbContext, priceAmount: 25m, stock: 100);

            CreateOrderCommand firstCommand = BuildCommand("ORD-2026-701", customer.Id.Value, article.Id.Value);
            Result<CreateOrderResponse> firstResult = await _useCase.ExecuteAsync(firstCommand);
            Assert.IsTrue(firstResult.IsSuccess, firstResult.Error);

            CreateOrderCommand secondCommand = BuildCommand("ORD-2026-702", customer.Id.Value, article.Id.Value);
            Result<CreateOrderResponse> secondResult = await _useCase.ExecuteAsync(secondCommand);

            Assert.IsTrue(secondResult.IsSuccess, secondResult.Error);
            Assert.AreEqual(25m, secondResult.Value!.TotalAmount);
        }

        private static CreateOrderCommand BuildCommand(string orderNumber, int customerId, int articleId)
            => new(
                orderNumber,
                customerId,
                new DateOnly(2026, 6, 1),
                null,
                new AddressOverrideInput("Main Street", "1", "8000", "Zurich", "CH"),
                new AddressOverrideInput("Main Street", "1", "8000", "Zurich", "CH"),
                [new CreateOrderLineInput(articleId, 1)]);
    }
}
