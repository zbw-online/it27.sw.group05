using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Domain.Orders.ValueObjects;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query
{
    /// <summary>
    /// Server-side, paginated order search. Deliberately avoids loading full Order/Customer
    /// aggregates: it projects only the columns the orders list needs, filters, sorts and pages
    /// entirely in SQL Server, using the same OrderStatus values the Domain layer defines for
    /// "active" vs. "archived" (see Order.IsActive / Order.IsArchived / Order.IsOverdue).
    /// </summary>
    public sealed class OrderSearchQueryRepository(OrderManagementDbContext context) : IOrderSearchQueryRepository
    {
        private static readonly string[] ActiveStatuses = [nameof(OrderStatus.Open), nameof(OrderStatus.InProgress)];
        private static readonly string[] ArchivedStatuses = [nameof(OrderStatus.Completed), nameof(OrderStatus.Cancelled)];

        private readonly OrderManagementDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<OrderSearchResultDto> SearchAsync(
            OrderSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            string[] statuses = criteria.Scope == OrderSearchScope.Active ? ActiveStatuses : ArchivedStatuses;
            string orderBy = criteria.Scope == OrderSearchScope.Active
                ? "CASE WHEN o.[DeliveryDate] < @Today THEN 0 ELSE 1 END, o.[DeliveryDate] ASC, o.[OrderDate] DESC, o.[OrderId] ASC"
                : "o.[StatusChangedAtUtc] DESC, o.[OrderDate] DESC, o.[OrderId] ASC";

            object searchPattern = string.IsNullOrWhiteSpace(criteria.SearchTerm)
                ? DBNull.Value
                : $"%{criteria.SearchTerm.Trim()}%";

            int skip = (criteria.Page - 1) * criteria.PageSize;

            // Mirrors Order.IsOverdue (IsActive && DeliveryDate < today) exactly: overdue is always
            // defined against the fixed "active" statuses, independent of which scope is queried,
            // so archived orders can never be reported as overdue.
            string isOverdueExpression =
                $"CASE WHEN o.[Status] IN ('{OrderStatus.Open}', '{OrderStatus.InProgress}') " +
                "AND o.[DeliveryDate] < @Today THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END";

            string itemsSql = $"""
                SELECT
                    o.[OrderId] AS OrderId,
                    o.[OrderNumber] AS OrderNumber,
                    o.[OrderDate] AS OrderDate,
                    o.[DeliveryDate] AS DeliveryDate,
                    o.[CustomerId] AS CustomerId,
                    c.[CustomerNumber] AS CustomerNumber,
                    (SELECT COUNT(*) FROM [dbo].[OrderLines] ol WHERE ol.[OrderId] = o.[OrderId]) AS LineCount,
                    o.[TotalAmount] AS TotalAmount,
                    o.[TotalCurrency] AS TotalCurrency,
                    o.[Status] AS Status,
                    {isOverdueExpression} AS IsOverdue
                FROM [dbo].[Orders] o
                INNER JOIN [dbo].[Customers] c ON c.[CustomerId] = o.[CustomerId]
                WHERE o.[Status] IN ({string.Join(",", statuses.Select((_, i) => $"@Status{i}"))})
                    AND (@SearchPattern IS NULL OR o.[OrderNumber] LIKE @SearchPattern OR c.[CustomerNumber] LIKE @SearchPattern)
                ORDER BY {orderBy}
                OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
                """;

            string countSql = $"""
                SELECT COUNT(*) AS Value
                FROM [dbo].[Orders] o
                INNER JOIN [dbo].[Customers] c ON c.[CustomerId] = o.[CustomerId]
                WHERE o.[Status] IN ({string.Join(",", statuses.Select((_, i) => $"@Status{i}"))})
                    AND (@SearchPattern IS NULL OR o.[OrderNumber] LIKE @SearchPattern OR c.[CustomerNumber] LIKE @SearchPattern)
                """;

            SqlParameter[] itemParameters =
            [
                .. statuses.Select((status, i) => new SqlParameter($"@Status{i}", status)),
                new SqlParameter("@SearchPattern", searchPattern),
                new SqlParameter("@Today", criteria.Today.ToDateTime(TimeOnly.MinValue)),
                new SqlParameter("@Skip", skip),
                new SqlParameter("@Take", criteria.PageSize),
            ];

            SqlParameter[] countParameters =
            [
                .. statuses.Select((status, i) => new SqlParameter($"@Status{i}", status)),
                new SqlParameter("@SearchPattern", searchPattern),
            ];

            List<OrderSearchRow> rows = await _context.Database
                .SqlQueryRaw<OrderSearchRow>(itemsSql, itemParameters)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            int totalCount = await _context.Database
                .SqlQueryRaw<int>(countSql, countParameters)
                .SingleAsync(cancellationToken);

            IReadOnlyList<OrderSearchItemDto> items = [.. rows.Select(row => new OrderSearchItemDto(
                row.OrderId,
                row.OrderNumber,
                row.OrderDate,
                row.DeliveryDate,
                row.CustomerId,
                row.CustomerNumber,
                row.LineCount,
                row.TotalAmount,
                row.TotalCurrency,
                Enum.Parse<OrderStatus>(row.Status),
                row.IsOverdue))];

            int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)criteria.PageSize);

            return new OrderSearchResultDto(items, totalCount, criteria.Page, criteria.PageSize, totalPages);
        }
    }
}
