using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    public partial class SeedQuarterlyAnalyticsData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(@"
-- ============================================================
-- SeedQuarterlyAnalyticsData
--   AN- : synthetic bulk dataset (volume)
--   CT- : controlled dataset (deterministic quarter totals)
-- ============================================================

-- Turn temporal OFF for inserts (mono-temporal tables)
ALTER TABLE Orders SET (SYSTEM_VERSIONING = OFF);
ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = OFF);

DECLARE @y int = YEAR(GETDATE());

-- ============================================================
-- A) AN- dataset (synthetic bulk)
-- ============================================================

;WITH Years AS (
    SELECT @y AS [Year] UNION ALL SELECT @y - 1 UNION ALL SELECT @y - 2
),
Quarters AS (
    SELECT 1 AS [Quarter],  1 AS StartMonth UNION ALL
    SELECT 2,               4 UNION ALL
    SELECT 3,               7 UNION ALL
    SELECT 4,              10
),
QuarterAnchors AS (
    SELECT
        y.[Year],
        q.[Quarter],
        AnchorDate = DATETIMEFROMPARTS(y.[Year], q.StartMonth, 15, 10, 0, 0, 0)
    FROM Years y
    CROSS JOIN Quarters q
),
CustomersSeed AS (
    -- assumes SeedTestData inserted CustomerId 1..5
    SELECT CustomerId
    FROM dbo.Customers
    WHERE CustomerId IN (1,2,3,4,5)
),
OrderPlan AS (
    -- 3 orders per customer per quarter = 3*5*12 = 180 orders
    SELECT
        qa.[Year],
        qa.[Quarter],
        c.CustomerId,
        n.OrderIdx,
        OrderDate = DATEADD(DAY, (n.OrderIdx * 9 + c.CustomerId * 3) % 70, qa.AnchorDate)
    FROM QuarterAnchors qa
    CROSS JOIN CustomersSeed c
    CROSS JOIN (VALUES (1),(2),(3)) n(OrderIdx)
),
OrdersToInsert AS (
    SELECT
        OrderNumber = CONCAT(
            'AN-', op.[Year], '-Q', op.[Quarter], '-',
            RIGHT('00000' + CAST(ROW_NUMBER() OVER (ORDER BY op.[Year], op.[Quarter], op.CustomerId, op.OrderIdx) AS varchar(10)), 5)
        ),
        op.OrderDate,
        op.CustomerId
    FROM OrderPlan op
)
INSERT INTO dbo.Orders
(
    OrderNumber, OrderDate, CustomerId,
    DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode,
    TotalAmount, TotalCurrency
)
SELECT
    o.OrderNumber,
    o.OrderDate,
    o.CustomerId,
    a.Street,
    a.HouseNumber,
    a.PostalCode,
    a.City,
    a.CountryCode,
    0.00,
    'CHF'
FROM OrdersToInsert o
OUTER APPLY (
    SELECT TOP(1)
        Street, HouseNumber, PostalCode, City, CountryCode
    FROM dbo.CustomerAddresses ca
    WHERE ca.CustomerId = o.CustomerId
      AND ca.ValidTo IS NULL
    ORDER BY ca.ValidFrom DESC
) a
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Orders ox WHERE ox.OrderNumber = o.OrderNumber
);

-- OrderLines for AN-: 1..4 lines per order (deterministic)
;WITH InsertedOrders AS (
    SELECT o.OrderId, o.OrderNumber
    FROM dbo.Orders o
    WHERE o.OrderNumber LIKE 'AN-%'
),
LinePlan AS (
    SELECT
        io.OrderId,
        LineCount = (ABS(CHECKSUM(io.OrderNumber)) % 4) + 1
    FROM InsertedOrders io
),
Lines AS (
    SELECT
        lp.OrderId,
        LineNumber = v.LineNo
    FROM LinePlan lp
    CROSS APPLY (VALUES (1),(2),(3),(4)) v(LineNo)
    WHERE v.LineNo <= lp.LineCount
),
PickArticles AS (
    SELECT
        l.OrderId,
        l.LineNumber,
        ArticleId = ((ABS(CHECKSUM(CONCAT(l.OrderId, ':', l.LineNumber))) % 18) + 1),
        Quantity = ((ABS(CHECKSUM(CONCAT(l.OrderId, '-', l.LineNumber))) % 5) + 1)
    FROM Lines l
)
INSERT INTO dbo.OrderLines
(
    OrderId, LineNumber, ArticleId, ArticleName, Quantity,
    UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency
)
SELECT
    pa.OrderId,
    pa.LineNumber,
    a.ArticleId,
    a.Name,
    pa.Quantity,
    a.PriceAmount,
    a.PriceCurrency,
    a.PriceAmount * pa.Quantity,
    a.PriceCurrency
FROM PickArticles pa
JOIN dbo.Articles a ON a.ArticleId = pa.ArticleId
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.OrderLines ol
    WHERE ol.OrderId = pa.OrderId AND ol.LineNumber = pa.LineNumber
);

-- Update AN- totals from lines
UPDATE o
SET
    o.TotalAmount = x.TotalAmount,
    o.TotalCurrency = 'CHF'
FROM dbo.Orders o
JOIN (
    SELECT ol.OrderId, TotalAmount = SUM(ol.LineTotalAmount)
    FROM dbo.OrderLines ol
    GROUP BY ol.OrderId
) x ON x.OrderId = o.OrderId
WHERE o.OrderNumber LIKE 'AN-%';


-- ============================================================
-- B) CT- dataset (controlled deterministic quarter totals)
--    12 orders: last 3 years × 4 quarters
-- ============================================================

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
INSERT INTO dbo.Orders
(
    OrderNumber, OrderDate, CustomerId,
    DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode,
    TotalAmount, TotalCurrency
)
SELECT
    CONCAT('CT-', t.[Year], '-Q', t.[Quarter]),
    t.OrderDate,
    1,
    'Controlstrasse', '1', '8000', 'Zürich', 'CH',
    t.TargetRevenue,
    'CHF'
FROM Targets t
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Orders o WHERE o.OrderNumber = CONCAT('CT-', t.[Year], '-Q', t.[Quarter])
);

-- One line per CT order; keep FK valid (ArticleId=1).
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

-- Turn temporal ON again
ALTER TABLE Orders SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrdersHistory));
ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrderLinesHistory));
");

        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(@"
ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = OFF);
ALTER TABLE Orders SET (SYSTEM_VERSIONING = OFF);

DELETE ol
FROM dbo.OrderLines ol
JOIN dbo.Orders o ON o.OrderId = ol.OrderId
WHERE o.OrderNumber LIKE 'AN-%' OR o.OrderNumber LIKE 'CT-%';

DELETE o
FROM dbo.Orders o
WHERE o.OrderNumber LIKE 'AN-%' OR o.OrderNumber LIKE 'CT-%';

ALTER TABLE Orders SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrdersHistory));
ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrderLinesHistory));
");
    }
}
