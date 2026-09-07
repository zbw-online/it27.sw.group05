using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Application.Features.Customers.ExportCustomerData;
using OrderManagement.Application.Features.Customers.ImportCustomerData;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;
using OrderManagement.Infrastructure.Serialization.Customers;
using OrderManagement.TestSupport;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests.Application.Customers
{
    /// <summary>
    /// Exercises the real Export -> file -> fresh database -> Import round trip end to end
    /// against SQL Server, for both JSON and XML, as required by the customer data exchange spec.
    /// </summary>
    [TestClass]
    public sealed class CustomerDataExportImportRoundtripTests : IntegrationTestBase
    {
        private readonly List<OrderManagementDbContext> _targetContextsToDispose = [];

        [TestCleanup]
        public async Task DisposeTargetDatabasesAsync()
        {
            foreach (OrderManagementDbContext context in _targetContextsToDispose)
            {
                _ = await context.Database.EnsureDeletedAsync();
                await context.DisposeAsync();
            }
        }

        [TestMethod]
        public async Task JsonRoundtrip_FromSourceDatabaseIntoFreshTargetDatabase_PreservesAllCustomers()
            => await RunRoundtripAsync(CustomerDataFormat.Json, "CU800");

        [TestMethod]
        public async Task XmlRoundtrip_FromSourceDatabaseIntoFreshTargetDatabase_PreservesAllCustomers()
            => await RunRoundtripAsync(CustomerDataFormat.Xml, "CU810");

        private async Task RunRoundtripAsync(CustomerDataFormat format, string customerNumberPrefix)
        {
            // Arrange: seed the source database with a customer that has a current address,
            // one without any address, and one whose website is set - covering every exported field.
            Customer withAddress = await InfrastructureTestDataFactory.CreatePersistedCustomerAsync(
                DbContext,
                customerNumber: $"{customerNumberPrefix}01",
                lastName: "Muster",
                surName: "Hans",
                validFrom: new DateOnly(2026, 1, 1),
                street: "Musterstrasse",
                houseNumber: "10",
                postalCode: "8000",
                city: "Zürich",
                countryCode: "CH");

            Result<Customer> withoutAddressResult = Customer.Create(
                $"{customerNumberPrefix}02", "Ohne", "Adresse", "ohne.adresse@test.local", "www.example.ch");
            Assert.IsTrue(withoutAddressResult.IsSuccess, withoutAddressResult.Error);
            Customer withoutAddress = withoutAddressResult.EnsureValue();
            _ = DbContext.Customers.Add(withoutAddress);
            _ = await DbContext.SaveChangesAsync();

            IOptions<CustomerDataExchangeOptions> options = Options.Create(new CustomerDataExchangeOptions());
            var jsonSerializer = new JsonCustomerDataSerializer(options);
            var xmlSerializer = new XmlCustomerDataSerializer(options);
            var resolver = new CustomerDataSerializerResolver([jsonSerializer, xmlSerializer]);

            var currentRepository = new CustomerCurrentQueryRepository(DbContext);
            var temporalRepository = new CustomerTemporalQueryRepository(DbContext);
            var exportUseCase = new ExportCustomerDataUseCase(
                currentRepository, temporalRepository, resolver, TimeProvider.System);

            // Act: export the current data set.
            Result<CustomerDataFile> exportResult = await exportUseCase.ExecuteAsync(
                ExportCustomerDataQuery.Current(format));
            Assert.IsTrue(exportResult.IsSuccess, exportResult.Error);
            CustomerDataFile exportedFile = exportResult.Value!;

            // The file must actually deserialize back to the exact customers we seeded.
            Result<IReadOnlyList<CustomerDataDto>> deserializeResult;
            using (var verifyStream = new MemoryStream(exportedFile.Content))
            {
                deserializeResult = await resolver.Resolve(format).Value!.DeserializeAsync(verifyStream);
            }

            Assert.IsTrue(deserializeResult.IsSuccess, deserializeResult.Error);
            CustomerDataDto exportedWithAddress = deserializeResult.Value!.Single(c => c.CustomerNumber == withAddress.CustomerNumber.Value);
            CustomerDataDto exportedWithoutAddress = deserializeResult.Value!.Single(c => c.CustomerNumber == withoutAddress.CustomerNumber.Value);

            Assert.IsNotNull(exportedWithAddress.Address);
            Assert.AreEqual("Musterstrasse", exportedWithAddress.Address!.Street);
            Assert.IsNull(exportedWithoutAddress.Address);

            // Act: import that very file into a brand-new, independently migrated database.
            // Disposal (and dropping the temp database) happens in DisposeTargetDatabasesAsync -
            // an `await using` here would dispose it a second time and fail on TestCleanup.
            OrderManagementDbContext targetContext = await CreateFreshTargetDatabaseAsync(
                $"OrderManagement_RoundtripTarget_{format}");

            var targetCommandRepository = new CustomerCommandRepository(targetContext);
            var targetQueryRepository = new CustomerQueryRepository(targetContext);
            var targetUnitOfWork = new UnitOfWork(targetContext);
            var targetPlanBuilder = new CustomerImportPlanBuilder(resolver, targetQueryRepository, options);
            var importUseCase = new ImportCustomerDataUseCase(targetPlanBuilder, targetCommandRepository, targetUnitOfWork);

            Result<ImportCustomerDataResponse> importResult = await importUseCase.ExecuteAsync(
                new ImportCustomerDataCommand(exportedFile));

            // Assert: business equality between the source rows and the freshly imported target rows.
            Assert.IsTrue(importResult.IsSuccess, importResult.Error);
            Assert.IsTrue(importResult.Value!.IsValid, string.Join("; ", importResult.Value.Issues.Select(i => i.Message)));
            Assert.AreEqual(2, importResult.Value.ImportedCount);

            targetContext.ChangeTracker.Clear();
            Customer? importedWithAddress = await targetQueryRepository.GetByCustomerNumberAsync(withAddress.CustomerNumber);
            Customer? importedWithoutAddress = await targetQueryRepository.GetByCustomerNumberAsync(withoutAddress.CustomerNumber);

            Assert.IsNotNull(importedWithAddress);
            Assert.AreEqual(withAddress.LastName, importedWithAddress!.LastName);
            Assert.AreEqual(withAddress.SurName, importedWithAddress.SurName);
            Assert.AreEqual(withAddress.Email.Value, importedWithAddress.Email.Value);
            CustomerAddress? importedAddress = importedWithAddress.AddressAt(new DateOnly(2026, 6, 1));
            Assert.IsNotNull(importedAddress);
            Assert.AreEqual("Musterstrasse", importedAddress!.Street);
            Assert.AreEqual("Zürich", importedAddress.City);

            Assert.IsNotNull(importedWithoutAddress);
            Assert.AreEqual(withoutAddress.Website, importedWithoutAddress!.Website);
            Assert.IsNull(importedWithoutAddress.AddressAt(DateOnly.FromDateTime(DateTime.UtcNow)));
        }

        private async Task<OrderManagementDbContext> CreateFreshTargetDatabaseAsync(string prefix)
        {
            string databaseName = TestDatabaseName.Create(prefix, TestContext.TestName);
            string connectionString = TestDatabaseName.BuildScopedConnectionString(AssemblySetup.MasterConnectionString, databaseName);

            DbContextOptions<OrderManagementDbContext> targetOptions = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseSqlServer(
                    connectionString,
                    sql => sql.MigrationsAssembly(typeof(OrderManagementDbContext).Assembly.FullName))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .Options;

            var targetContext = new OrderManagementDbContext(targetOptions);
            _targetContextsToDispose.Add(targetContext);

            await targetContext.Database.MigrateAsync();
            return targetContext;
        }
    }
}
