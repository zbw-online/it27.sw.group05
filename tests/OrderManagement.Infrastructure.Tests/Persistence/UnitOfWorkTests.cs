using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence
{
    [TestClass]
    public sealed class UnitOfWorkTests : IntegrationTestBase
    {
        private UnitOfWork _unitOfWork = default!;

        protected override Task OnDatabaseInitializedAsync()
        {
            _unitOfWork = new UnitOfWork(DbContext);
            return Task.CompletedTask;
        }

        [TestMethod]
        public void Constructor_WithDbContext_ShouldImplementIUnitOfWork() => Assert.IsInstanceOfType<IUnitOfWork>(_unitOfWork);

        [TestMethod]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException() => _ = Assert.ThrowsException<ArgumentNullException>(() => _ = new UnitOfWork(null!));

        [TestMethod]
        public async Task CommitAsync_WithNoChanges_ShouldReturnSuccess()
        {
            Result result = await _unitOfWork.CommitAsync();

            Assert.IsTrue(result.IsSuccess, result.Error);
        }

        [TestMethod]
        public async Task CommitAsync_WithAddedEntity_ShouldPersistAndGenerateId()
        {
            ArticleGroup group = ArticleGroup.Create("UoW Test Group").EnsureValue();

            _ = DbContext.ArticleGroups.Add(group);

            Result result = await _unitOfWork.CommitAsync();

            Assert.IsTrue(result.IsSuccess, result.Error);
            Assert.IsTrue(group.Id.IsAssigned);

            DbContext.ChangeTracker.Clear();

            ArticleGroup? persisted = await DbContext.ArticleGroups
                .AsNoTracking()
                .SingleOrDefaultAsync(g => g.Id == group.Id);

            Assert.IsNotNull(persisted);
            Assert.AreEqual("UoW Test Group", persisted.Name);
        }

        [TestMethod]
        public async Task CommitAsync_WithUniqueConstraintViolation_ShouldReturnFailure()
        {
            Customer existing = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: "C-19999",
                email: "existing@test.local");

            Customer duplicate = Customer.Create(
                customerNr: existing.CustomerNumber.Value,
                lastName: "Duplicate",
                surName: "Customer",
                email: "duplicate@test.local",
                website: null).EnsureValue();

            _ = DbContext.Customers.Add(duplicate);

            Result result = await _unitOfWork.CommitAsync();

            Assert.IsFalse(result.IsSuccess);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Error));
        }
    }
}
