using System.Runtime.CompilerServices;

namespace SharedKernel.SeedWork
{
    public abstract class Entity<TId> : IEquatable<Entity<TId>>
        where TId : notnull
    {
        protected Entity(TId id) => Id = id;
        protected Entity()
        {
            // For ORMs
        }
        public TId Id { get; protected set; } = default!;
        private bool IsTransient() => EqualityComparer<TId>.Default.Equals(Id, default!);
        public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);
        public bool Equals(Entity<TId>? other)
        {
            if (other is null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }

            // Two new entities with default ids must not be considered equal.
            return !IsTransient() && !other.IsTransient() && EqualityComparer<TId>.Default.Equals(Id, other.Id);
        }

        public override int GetHashCode() => IsTransient() ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(GetType(), Id);
        public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
        public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
    }
}
