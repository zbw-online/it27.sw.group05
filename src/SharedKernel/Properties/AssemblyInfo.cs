using System.Runtime.CompilerServices;

// Allow Infrastructure to rehydrate shared kernel types (e.g., Value Objects) without widening the public API.
[assembly: InternalsVisibleTo("OrderManagement.Infrastructure")]
