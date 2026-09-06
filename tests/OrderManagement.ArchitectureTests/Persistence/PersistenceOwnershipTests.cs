using System.Reflection;

using Microsoft.EntityFrameworkCore;

namespace OrderManagement.ArchitectureTests.Persistence
{
    [TestClass]
    public sealed class PersistenceOwnershipTests
    {
        private static readonly Assembly DomainAssembly = typeof(Domain.Orders.Order).Assembly;
        private static readonly Assembly ApplicationAssembly = typeof(Application.ApplicationServiceCollectionExtensions).Assembly;
        private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.InfrastructureServiceCollectionExtensions).Assembly;

        [TestMethod]
        public void RepositoryAndSerializerInterfaces_AreOwnedByApplication()
        {
            List<Type> misplaced = [.. DomainAssembly.GetTypes()
                .Concat(InfrastructureAssembly.GetTypes())
                .Where(type => type.IsInterface)
                .Where(type => type.Name.EndsWith("Repository", StringComparison.Ordinal)
                    || type.Name.EndsWith("Serializer", StringComparison.Ordinal)
                    || type.Name.EndsWith("SerializerResolver", StringComparison.Ordinal)
                    || type.Name == "IUnitOfWork")];

            Assert.IsFalse(misplaced.Count > 0,
                $"Repository/serializer abstractions must live in Application, but found: {string.Join(", ", misplaced.Select(t => t.FullName))}");
        }

        [TestMethod]
        public void ConcreteRepositoriesAndSerializers_AreOwnedByInfrastructure()
        {
            List<Type> misplaced = [.. DomainAssembly.GetTypes()
                .Concat(ApplicationAssembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => (type.Name.EndsWith("Repository", StringComparison.Ordinal)
                    || type.Name.EndsWith("Serializer", StringComparison.Ordinal))
                    && !type.Name.EndsWith("Builder", StringComparison.Ordinal))];

            Assert.IsFalse(misplaced.Count > 0,
                $"Concrete repository/serializer implementations must live in Infrastructure, but found: {string.Join(", ", misplaced.Select(t => t.FullName))}");
        }

        [TestMethod]
        public void Domain_ContainsNoEfCoreEntityConfigurationsOrDbContexts()
        {
            List<Type> violations = [.. DomainAssembly.GetTypes()
                .Where(type => typeof(DbContext).IsAssignableFrom(type)
                    || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IEntityTypeConfiguration", StringComparison.Ordinal)))];

            Assert.IsFalse(violations.Count > 0,
                $"Domain must not contain EF Core configuration types: {string.Join(", ", violations.Select(t => t.FullName))}");
        }

        [TestMethod]
        public void Application_ContainsNoEfCoreEntityConfigurationsOrDbContexts()
        {
            List<Type> violations = [.. ApplicationAssembly.GetTypes()
                .Where(type => typeof(DbContext).IsAssignableFrom(type)
                    || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("IEntityTypeConfiguration", StringComparison.Ordinal)))];

            Assert.IsFalse(violations.Count > 0,
                $"Application must not contain EF Core configuration types: {string.Join(", ", violations.Select(t => t.FullName))}");
        }
    }
}
