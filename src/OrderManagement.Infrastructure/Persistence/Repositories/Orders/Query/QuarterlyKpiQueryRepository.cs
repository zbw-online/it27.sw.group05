using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query
{
    public sealed class QuarterlyKpiQueryRepository(OrderManagementDbContext db) : IQuarterlyKpiQueryRepository
    {
        private readonly OrderManagementDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<IReadOnlyList<QuarterlyKpiRowDto>> GetQuarterlyKpisLast3YearsAsync(CancellationToken ct = default)
        {
            const string sql = @"
DECLARE @y int = YEAR(GETUTCDATE());

WITH Years AS (
    SELECT @y AS [Year] UNION ALL SELECT @y - 1 UNION ALL SELECT @y - 2
),
Quarters AS (
    SELECT 1 AS [Quarter] UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
),
Calendar AS (
    SELECT y.[Year], q.[Quarter]
    FROM Years y
    CROSS JOIN Quarters q
),
OrderLineCounts AS (
    SELECT OrderId, LineCountPerOrder = COUNT_BIG(*)
    FROM dbo.OrderLines
    GROUP BY OrderId
),
Agg AS (
    SELECT
        [Year]    = YEAR(o.OrderDate),
        [Quarter] = DATEPART(QUARTER, o.OrderDate),

        OrdersCount       = COUNT_BIG(*),
        TotalRevenue      = SUM(o.TotalAmount),
        DistinctCustomers = COUNT(DISTINCT o.CustomerId),
        LineCount         = SUM(COALESCE(olc.LineCountPerOrder, 0))
    FROM dbo.Orders o
    LEFT JOIN OrderLineCounts olc ON olc.OrderId = o.OrderId
    WHERE YEAR(o.OrderDate) BETWEEN (@y - 2) AND @y
    GROUP BY YEAR(o.OrderDate), DATEPART(QUARTER, o.OrderDate)
),
Kpis AS (
    SELECT
        c.[Year],
        c.[Quarter],

        OrdersCount       = COALESCE(a.OrdersCount, 0),
        TotalRevenue      = COALESCE(CAST(a.TotalRevenue AS decimal(18,2)), 0),
        DistinctCustomers = COALESCE(a.DistinctCustomers, 0),
        LineCount         = COALESCE(a.LineCount, 0),

        ManagedArticles = (SELECT COUNT(*) FROM dbo.Articles ar WHERE ar.Status = 1),

        AvgItemsPerOrder =
            CASE WHEN COALESCE(a.OrdersCount, 0) = 0 THEN CAST(0 AS decimal(18,2))
                 ELSE CAST(a.LineCount AS decimal(18,2)) / CAST(a.OrdersCount AS decimal(18,2))
            END,

        RevenuePerCustomer =
            CASE WHEN COALESCE(a.DistinctCustomers, 0) = 0 THEN CAST(0 AS decimal(18,2))
                 ELSE CAST(a.TotalRevenue AS decimal(18,2)) / CAST(a.DistinctCustomers AS decimal(18,2))
            END
    FROM Calendar c
    LEFT JOIN Agg a
        ON a.[Year] = c.[Year] AND a.[Quarter] = c.[Quarter]
),
Unpivoted AS (
    SELECT [Year], [Quarter], Category = N'Anzahl Auftraege',             Value = CAST(OrdersCount        AS decimal(18,2)) FROM Kpis
    UNION ALL
    SELECT [Year], [Quarter], Category = N'Anzahl verwaltete Artikel',    Value = CAST(ManagedArticles    AS decimal(18,2)) FROM Kpis
    UNION ALL
    SELECT [Year], [Quarter], Category = N'Durchschnitt Artikel/Auftrag', Value = CAST(AvgItemsPerOrder   AS decimal(18,2)) FROM Kpis
    UNION ALL
    SELECT [Year], [Quarter], Category = N'Umsatz pro Kunde',             Value = CAST(RevenuePerCustomer AS decimal(18,2)) FROM Kpis
    UNION ALL
    SELECT [Year], [Quarter], Category = N'Gesamtumsatz',                 Value = CAST(TotalRevenue       AS decimal(18,2)) FROM Kpis
),
YoY AS (
    SELECT
        Category,
        [Year],
        [Quarter],
        Value,
        PrevYearValue = LAG(Value, 1) OVER (PARTITION BY Category, [Quarter] ORDER BY [Year]),
        YoYDelta      = Value - LAG(Value, 1) OVER (PARTITION BY Category, [Quarter] ORDER BY [Year]),
        YoYDeltaPercent =
            CASE
                WHEN LAG(Value, 1) OVER (PARTITION BY Category, [Quarter] ORDER BY [Year]) IS NULL THEN NULL
                WHEN LAG(Value, 1) OVER (PARTITION BY Category, [Quarter] ORDER BY [Year]) = 0 THEN NULL
                ELSE CAST(
                    (Value - LAG(Value, 1) OVER (PARTITION BY Category, [Quarter] ORDER BY [Year]))
                    / LAG(Value, 1) OVER (PARTITION BY Category, [Quarter] ORDER BY [Year]) * 100.0
                AS decimal(18,2))
            END
    FROM Unpivoted
)
SELECT
    Category,
    [Year],
    [Quarter],
    Value,
    PrevYearValue,
    YoYDelta,
    YoYDeltaPercent
FROM YoY
ORDER BY Category, [Year], [Quarter];
";

            return await _db.Database
                .SqlQueryRaw<QuarterlyKpiRowDto>(sql)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
