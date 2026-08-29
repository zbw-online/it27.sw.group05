using System.Reflection;

namespace OrderManagement.Application.Tests.Fakes
{
    internal static class TestIdAssigner
    {
        public static void Assign<TId>(object entity, TId id)
        {
            PropertyInfo property = entity.GetType().GetProperty("Id")
                ?? throw new InvalidOperationException($"Entity {entity.GetType().Name} has no Id property.");

            property.SetValue(entity, id);
        }
    }
}
