using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Abstractions;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Infrastructure.Persistence;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Tests.Persistence
{
    [TestClass]
    public sealed class UnitOfWorkTests : IntegrationTestBase
    {
        private UnitOfWork? _unitOfWork;

        [TestInitialize]
        public void Setup() => _unitOfWork = new UnitOfWork(DbContext!);

        [TestMethod]
        public void Constructor_WithDbContext_ShouldNotThrow() => _ = new UnitOfWork(DbContext!);

        [TestMethod]
        public void Constructor_WithNullDbContext_ShouldThrowArgumentNullException() => Assert.ThrowsException<ArgumentNullException>(() => _ = new UnitOfWork(null!));

        [TestMethod]
        public void UnitOfWork_ShouldImplementIUnitOfWork() => Assert.IsInstanceOfType<IUnitOfWork>(_unitOfWork);

        [TestMethod]
        public async Task CommitAsync_WithNoChanges_ShouldReturnSuccess()
        {


            Result result = await _unitOfWork!.CommitAsync();

            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public async Task CommitAsync_WithAddedEntity_ShouldPersistChanges()
        {



            Result<ArticleGroup> groupResult = ArticleGroup.Create(600, "UoW Test Group");
            Assert.IsTrue(groupResult.IsSuccess);

            _ = DbContext!.ArticleGroups.Add(groupResult.Value!);

            Result commitResult = await _unitOfWork!.CommitAsync();

            Assert.IsTrue(commitResult.IsSuccess);

            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(600));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("UoW Test Group", retrieved.Name);
        }

        [TestMethod]
        public async Task CommitAsync_WithMultipleEntities_ShouldPersistAll()
        {



            Result<ArticleGroup> group1 = ArticleGroup.Create(601, "Group 1");
            Result<ArticleGroup> group2 = ArticleGroup.Create(602, "Group 2");
            Result<ArticleGroup> group3 = ArticleGroup.Create(603, "Group 3");

            Assert.IsTrue(group1.IsSuccess && group2.IsSuccess && group3.IsSuccess);

            _ = DbContext!.ArticleGroups.Add(group1.Value!);
            _ = DbContext.ArticleGroups.Add(group2.Value!);
            _ = DbContext.ArticleGroups.Add(group3.Value!);

            Result result = await _unitOfWork!.CommitAsync();

            Assert.IsTrue(result.IsSuccess);

            List<ArticleGroup> groups = await DbContext.ArticleGroups
                .Where(g => g.Id == new ArticleGroupId(601) || g.Id == new ArticleGroupId(602) || g.Id == new ArticleGroupId(603))
                .ToListAsync();

            Assert.AreEqual(3, groups.Count);
        }

        [TestMethod]
        public async Task CommitAsync_WithModifiedEntity_ShouldPersistChanges()
        {



            Result<ArticleGroup> groupResult = ArticleGroup.Create(604, "Original Name");
            Assert.IsTrue(groupResult.IsSuccess);
            ArticleGroup group = groupResult.Value!;

            _ = DbContext!.ArticleGroups.Add(group);
            _ = await _unitOfWork!.CommitAsync();

            Result renameResult = group.Rename("Modified Name");
            Assert.IsTrue(renameResult.IsSuccess);

            Result commitResult = await _unitOfWork.CommitAsync();

            Assert.IsTrue(commitResult.IsSuccess);

            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(604));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual("Modified Name", retrieved.Name);
        }

        [TestMethod]
        public async Task CommitAsync_WithDeletedEntity_ShouldRemoveFromDatabase()
        {



            Result<ArticleGroup> groupResult = ArticleGroup.Create(605, "To Delete");
            Assert.IsTrue(groupResult.IsSuccess);
            ArticleGroup group = groupResult.Value!;

            _ = DbContext!.ArticleGroups.Add(group);
            _ = await _unitOfWork!.CommitAsync();

            _ = DbContext.ArticleGroups.Remove(group);
            Result deleteResult = await _unitOfWork.CommitAsync();

            Assert.IsTrue(deleteResult.IsSuccess);

            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(605));

            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public async Task CommitAsync_MultipleTimes_ShouldAllSucceed()
        {



            // First commit
            Result<ArticleGroup> group1 = ArticleGroup.Create(607, "First Commit");
            Assert.IsTrue(group1.IsSuccess);
            _ = DbContext!.ArticleGroups.Add(group1.Value!);
            Result result1 = await _unitOfWork!.CommitAsync();
            Assert.IsTrue(result1.IsSuccess);

            // Second commit
            Result<ArticleGroup> group2 = ArticleGroup.Create(608, "Second Commit");
            Assert.IsTrue(group2.IsSuccess);
            _ = DbContext.ArticleGroups.Add(group2.Value!);
            Result result2 = await _unitOfWork.CommitAsync();
            Assert.IsTrue(result2.IsSuccess);

            // Third commit with no changes
            Result result3 = await _unitOfWork.CommitAsync();
            Assert.IsTrue(result3.IsSuccess);

            List<ArticleGroup> groups = await DbContext.ArticleGroups
                .Where(g => g.Id == new ArticleGroupId(607) || g.Id == new ArticleGroupId(608))
                .ToListAsync();

            Assert.AreEqual(2, groups.Count);
        }

        [TestMethod]
        public async Task CommitAsync_WithDatabaseConstraintViolation_ShouldReturnFailure()
        {



            // Add an article group
            Result<ArticleGroup> groupResult = ArticleGroup.Create(609, "First Insert");
            Assert.IsTrue(groupResult.IsSuccess);
            _ = DbContext!.ArticleGroups.Add(groupResult.Value!);
            Result firstCommit = await _unitOfWork!.CommitAsync();
            Assert.IsTrue(firstCommit.IsSuccess);

            // Detach the entity so we can try to add a duplicate
            DbContext.Entry(groupResult.Value!).State = EntityState.Detached;

            // Try to add another with same ID (should fail)
            Result<ArticleGroup> duplicateResult = ArticleGroup.Create(609, "Duplicate Insert");
            Assert.IsTrue(duplicateResult.IsSuccess);
            _ = DbContext.ArticleGroups.Add(duplicateResult.Value!);

            Result secondCommit = await _unitOfWork.CommitAsync();

            Assert.IsFalse(secondCommit.IsSuccess);
            Assert.IsNotNull(secondCommit.Error);
        }

        [TestMethod]
        public async Task CommitAsync_WithTransaction_ShouldRespectTransactionBoundaries()
        {



            using IDbContextTransaction transaction = await DbContext!.Database.BeginTransactionAsync();

            Result<ArticleGroup> groupResult = ArticleGroup.Create(610, "Transaction UoW");
            Assert.IsTrue(groupResult.IsSuccess);
            _ = DbContext.ArticleGroups.Add(groupResult.Value!);

            Result result = await _unitOfWork!.CommitAsync();
            Assert.IsTrue(result.IsSuccess);

            await transaction.RollbackAsync();

            ArticleGroup? retrieved = await DbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(610));

            Assert.IsNull(retrieved);
        }

        [TestMethod]
        public async Task CommitAsync_WithComplexRelationship_ShouldPersistBothEntities()
        {



            // Create parent group
            Result<ArticleGroup> parentResult = ArticleGroup.Create(611, "Parent Group");
            Assert.IsTrue(parentResult.IsSuccess);
            _ = DbContext!.ArticleGroups.Add(parentResult.Value!);
            _ = await _unitOfWork!.CommitAsync();

            // Create article in that group
            Result<Article> articleResult = Article.Create(
                id: 612,
                articleNr: "UOW-001",
                name: "UoW Test Article",
                priceAmount: 99.99m,
                priceCurrency: "EUR",
                groupId: 611,
                stock: 5
            );

            Assert.IsTrue(articleResult.IsSuccess);
            _ = DbContext.Articles.Add(articleResult.Value!);
            Result commitResult = await _unitOfWork.CommitAsync();

            Assert.IsTrue(commitResult.IsSuccess);

            Article? retrieved = await DbContext.Articles
                .FirstOrDefaultAsync(a => a.Id == new ArticleId(612));

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(611, retrieved.ArticleGroupId.Value);
        }

        [TestMethod]
        public async Task CommitAsync_ConcurrentCalls_ShouldHandleSerially()
        {



            var tasks = new List<Task<Result>>();

            for (int i = 613; i <= 617; i++)
            {
                int id = i;
                Result<ArticleGroup> groupResult = ArticleGroup.Create(id, $"Group {id}");
                Assert.IsTrue(groupResult.IsSuccess);
                _ = DbContext!.ArticleGroups.Add(groupResult.Value!);
            }

            // Attempt concurrent commits (they should be serialized by EF Core)
            tasks.Add(_unitOfWork!.CommitAsync());

            Result[] results = await Task.WhenAll(tasks);

            Assert.IsTrue(results.All(r => r.IsSuccess));

            List<ArticleGroup> groups = await DbContext!.ArticleGroups
                .Where(g => g.Id == new ArticleGroupId(613) ||
                            g.Id == new ArticleGroupId(614) ||
                            g.Id == new ArticleGroupId(615) ||
                            g.Id == new ArticleGroupId(616) ||
                            g.Id == new ArticleGroupId(617))
                .ToListAsync();

            Assert.AreEqual(5, groups.Count);
        }

        [TestMethod]
        public async Task CommitAsync_AfterExceptionInPreviousCall_ShouldStillWork()
        {


            // Create a new context for this test to avoid contamination
            DbContextOptions<OrderManagementDbContext> options = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(DbContext!.Database.GetConnectionString())
                .Options;

            await using var context = new OrderManagementDbContext(options);
            var uow = new UnitOfWork(context);

            // First, create a valid entity
            Result<ArticleGroup> group1 = ArticleGroup.Create(618, "First Group");
            Assert.IsTrue(group1.IsSuccess);
            _ = context.ArticleGroups.Add(group1.Value!);
            Result result1 = await uow.CommitAsync();
            Assert.IsTrue(result1.IsSuccess);

            // Clear the context
            await context.DisposeAsync();

            // Create another context
            await using var context2 = new OrderManagementDbContext(options);
            var uow2 = new UnitOfWork(context2);

            // Try to add duplicate (will fail)
            Result<ArticleGroup> duplicate = ArticleGroup.Create(618, "Duplicate");
            Assert.IsTrue(duplicate.IsSuccess);
            _ = context2.ArticleGroups.Add(duplicate.Value!);
            Result failResult = await uow2.CommitAsync();
            Assert.IsFalse(failResult.IsSuccess);

            // Dispose and create a fresh context
            await context2.DisposeAsync();
            await using var context3 = new OrderManagementDbContext(options);
            var uow3 = new UnitOfWork(context3);

            // Now try a valid operation
            Result<ArticleGroup> group2 = ArticleGroup.Create(619, "After Error");
            Assert.IsTrue(group2.IsSuccess);
            _ = context3.ArticleGroups.Add(group2.Value!);
            Result result2 = await uow3.CommitAsync();
            Assert.IsTrue(result2.IsSuccess);

            ArticleGroup? retrieved = await context3.ArticleGroups
                .FirstOrDefaultAsync(g => g.Id == new ArticleGroupId(619));

            Assert.IsNotNull(retrieved);
        }
    }
}
