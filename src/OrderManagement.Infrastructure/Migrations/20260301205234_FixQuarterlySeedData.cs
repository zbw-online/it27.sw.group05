using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixQuarterlySeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(@"
-- ============================================================
-- FixQuarterlySeedData
-- Ensures CT- data exists AND has correct deterministic totals.
-- Idempotent: insert missing + update existing.
-- ============================================================

ALTER TABLE Orders SET (SYSTEM_VERSIONING = OFF);
ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = OFF);

DECLARE @y int = YEAR(GETDATE());

;WITH Years AS (
    SELECT @y AS [Year] UNION ALL SELECT @y - 1 UNION ALL SELECT @y - 2
),
Quarters AS (
    SELECT 1 AS [Quarter],  1 AS StartMonth UNION ALL
    SELECT 2,               4 UNION ALL
    SELECT 3,               7 UNION ALL
    SELECT 4,              10
),
Targets AS (
    SELECT
        y.[Year],
        q.[Quarter],
        OrderNumber = CONCAT('CT-', y.[Year], '-Q', q.[Quarter]),
        OrderDate = DATETIMEFROMPARTS(y.[Year], q.StartMonth, 20, 12, 0, 0, 0),
        TargetRevenue =
            CAST(1000
                 + CASE y.[Year]
                       WHEN @y - 2 THEN 0
                       WHEN @y - 1 THEN 200
                       WHEN @y     THEN 400
                   END
                 + CASE q.[Quarter]
                       WHEN 1 THEN 0
                       WHEN 2 THEN 50
                       WHEN 3 THEN 100
                       WHEN 4 THEN 150
                   END
            AS decimal(18,2))
    FROM Years y
    CROSS JOIN Quarters q
)
-- 1) Insert missing CT orders
INSERT INTO dbo.Orders
(
    OrderNumber, OrderDate, CustomerId,
    DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode,
    TotalAmount, TotalCurrency
)
SELECT
    t.OrderNumber,
    t.OrderDate,
    1,
    'Controlstrasse', '1', '8000', 'Zürich', 'CH',
    t.TargetRevenue,
    'CHF'
FROM Targets t
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Orders o WHERE o.OrderNumber = t.OrderNumber
);

-- 2) Update existing CT orders (fixes stale TotalAmount = 0)
UPDATE o
SET
    o.OrderDate = t.OrderDate,
    o.CustomerId = 1,
    o.DeliveryStreet = 'Controlstrasse',
    o.DeliveryHouseNumber = '1',
    o.DeliveryPostalCode = '8000',
    o.DeliveryCity = 'Zürich',
    o.DeliveryCountryCode = 'CH',
    o.TotalAmount = t.TargetRevenue,
    o.TotalCurrency = 'CHF'
FROM dbo.Orders o
JOIN Targets t ON t.OrderNumber = o.OrderNumber
WHERE o.OrderNumber LIKE 'CT-%';

-- 3) Insert missing CT line 1 (valid FK ArticleId=1)
INSERT INTO dbo.OrderLines
(
    OrderId, LineNumber, ArticleId, ArticleName, Quantity,
    UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency
)
SELECT
    o.OrderId,
    1,
    1,
    'CONTROL LINE',
    1,
    o.TotalAmount,
    o.TotalCurrency,
    o.TotalAmount,
    o.TotalCurrency
FROM dbo.Orders o
WHERE o.OrderNumber LIKE 'CT-%'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.OrderLines ol WHERE ol.OrderId = o.OrderId AND ol.LineNumber = 1
  );

-- 4) Update CT line 1 to match CT totals (fixes existing wrong lines)
UPDATE ol
SET
    ol.ArticleId = 1,
    ol.ArticleName = 'CONTROL LINE',
    ol.Quantity = 1,
    ol.UnitPriceAmount = o.TotalAmount,
    ol.UnitPriceCurrency = o.TotalCurrency,
    ol.LineTotalAmount = o.TotalAmount,
    ol.LineTotalCurrency = o.TotalCurrency
FROM dbo.OrderLines ol
JOIN dbo.Orders o ON o.OrderId = ol.OrderId
WHERE o.OrderNumber LIKE 'CT-%'
  AND ol.LineNumber = 1;

ALTER TABLE Orders SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrdersHistory));
ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrderLinesHistory));
");

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(@"
ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = OFF);
ALTER TABLE Orders SET (SYSTEM_VERSIONING = OFF);

DELETE ol
FROM dbo.OrderLines ol
JOIN dbo.Orders o ON o.OrderId = ol.OrderId
WHERE o.OrderNumber LIKE 'CT-%';

DELETE o
FROM dbo.Orders o
WHERE o.OrderNumber LIKE 'CT-%';

ALTER TABLE Orders SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrdersHistory));
ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrderLinesHistory));
");
    }
}
