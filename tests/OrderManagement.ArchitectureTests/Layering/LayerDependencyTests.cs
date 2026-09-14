using System.Reflection;

namespace OrderManagement.ArchitectureTests.Layering
{
    [TestClass]
    public sealed class LayerDependencyTests
    {
        private static readonly Assembly SharedKernelAssembly = typeof(SharedKernel.Primitives.Result).Assembly;
        private static readonly Assembly DomainAssembly = typeof(Domain.Orders.Order).Assembly;
        private static readonly Assembly ApplicationAssembly = typeof(Application.ApplicationServiceCollectionExtensions).Assembly;
        private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.InfrastructureServiceCollectionExtensions).Assembly;
        private static readonly Assembly PresentationAssembly = typeof(Presentation.Blazor.Program).Assembly;

        private static IEnumerable<string> ReferencedAssemblyNames(Assembly assembly)
            => assembly.GetReferencedAssemblies().Select(name => name.Name!);

        [TestMethod]
        public void SharedKernel_DoesNotReferenceAnyOtherSolutionProject()
        {
            string[] solutionAssemblies = ["OrderManagement.Domain", "OrderManagement.Application", "OrderManagement.Infrastructure", "OrderManagement.Presentation.Blazor"];

            IEnumerable<string> violations = ReferencedAssemblyNames(SharedKernelAssembly).Intersect(solutionAssemblies);

            Assert.IsFalse(violations.Any(), $"SharedKernel must not depend on: {string.Join(", ", violations)}");
        }

        [TestMethod]
        public void Domain_OnlyReferencesSharedKernelAmongSolutionProjects()
        {
            string[] forbidden = ["OrderManagement.Application", "OrderManagement.Infrastructure", "OrderManagement.Presentation.Blazor"];

            IEnumerable<string> violations = ReferencedAssemblyNames(DomainAssembly).Intersect(forbidden);

            Assert.IsFalse(violations.Any(), $"Domain must not depend on: {string.Join(", ", violations)}");
        }

        [TestMethod]
        public void Domain_HasNoFrameworkOrInfrastructureDependency()
        {
            string[] forbiddenPrefixes =
            [
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions.Configuration",
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.Extensions.Options",
                "System.Text.Json",
                "Newtonsoft.Json"
            ];

            List<string> violations = [.. ReferencedAssemblyNames(DomainAssembly)
                .Where(name => forbiddenPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))];

            Assert.IsFalse(violations.Count > 0, $"Domain must stay framework-free but references: {string.Join(", ", violations)}");
        }

        [TestMethod]
        public void Application_OnlyReferencesDomainAndSharedKernelAmongSolutionProjects()
        {
            string[] forbidden = ["OrderManagement.Infrastructure", "OrderManagement.Presentation.Blazor"];

            IEnumerable<string> violations = ReferencedAssemblyNames(ApplicationAssembly).Intersect(forbidden);

            Assert.IsFalse(violations.Any(), $"Application must not depend on: {string.Join(", ", violations)}");
        }

        [TestMethod]
        public void Infrastructure_DoesNotReferencePresentation()
        {
            IEnumerable<string> violations = ReferencedAssemblyNames(InfrastructureAssembly)
                .Where(name => name == "OrderManagement.Presentation.Blazor");

            Assert.IsFalse(violations.Any(), "Infrastructure must not depend on Presentation.");
        }

        [TestMethod]
        public void Infrastructure_ReferencesApplicationAndTransitivelyDomainAndSharedKernel()
        {
            IEnumerable<string> referenced = ReferencedAssemblyNames(InfrastructureAssembly);

            Assert.IsTrue(referenced.Contains("OrderManagement.Application"), "Infrastructure should depend on Application to implement its ports.");

            IEnumerable<string> applicationReferenced = ReferencedAssemblyNames(ApplicationAssembly);
            Assert.IsTrue(applicationReferenced.Contains("OrderManagement.Domain"), "Domain should be reachable transitively through Application.");
            Assert.IsTrue(applicationReferenced.Contains("SharedKernel"), "SharedKernel should be reachable transitively through Application.");
        }

        [TestMethod]
        public void Presentation_ReferencesApplicationAndInfrastructure()
        {
            IEnumerable<string> referenced = ReferencedAssemblyNames(PresentationAssembly);

            Assert.IsTrue(referenced.Contains("OrderManagement.Application"), "Presentation is the composition root and must reference Application.");
            Assert.IsTrue(referenced.Contains("OrderManagement.Infrastructure"), "Presentation is the composition root and must reference Infrastructure.");
        }
    }
}
