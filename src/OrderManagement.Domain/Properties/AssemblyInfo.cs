using System.Runtime.CompilerServices;

// Allow Infrastructure to rehydrate domain types (e.g., Value Objects) without widening the public API.
[assembly: InternalsVisibleTo("OrderManagement.Infrastructure")]
