namespace SharedKernel.SeedWork
{
    public abstract class Entity<TId> : IEquatable<Entity<TId>>
        where TId : notnull
    {
        protected Entity(TId id) => Id = id;

        // For ORMs
        protected Entity() { }
        public TId Id { get; protected set; } = default!;
        public override bool Equals(object? obj) => obj is Entity<TId> other && Equals(other);
        public bool Equals(Entity<TId>? other)
        {
            if (other is null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }

            // Entities are Equal if they are the same type and have the same identity
            return GetType() == other.GetType() && EqualityComparer<TId>.Default.Equals(Id, other.Id);

        }

        public override int GetHashCode() => HashCode.Combine(GetType(), Id);
        public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
        public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
    }
}
