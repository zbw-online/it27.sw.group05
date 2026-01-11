namespace SharedKernel.SeedWork
{

    public abstract class ValueObject : IEquatable<ValueObject>
    {
        protected abstract IEnumerable<object?> GetEqualityComponents();
        public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

        public bool Equals(ValueObject? other) => other is not null && (ReferenceEquals(this, other) || (GetType() == other.GetType() && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents())));

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (object? component in GetEqualityComponents())
            {
                hash.Add(component);
            }
            return hash.ToHashCode();
        }

        public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

        public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
    }
}
