using System.Data;
using System.Data.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query;

namespace OrderManagement.Infrastructure.Tests.Persistence.Repositories.Orders.Query
{
#pragma warning disable CA1305
#pragma warning disable CA1310

    [TestClass]
    public sealed class QuarterlyKpiQueryRepositoryTests : IntegrationTestBase
    {
        private QuarterlyKpiQueryRepository? _repo;

        [TestInitialize]
        public void Setup()
        {
            Assert.IsNotNull(DbContext);
            _repo = new QuarterlyKpiQueryRepository(DbContext);
        }

        [TestMethod]
        public async Task GetQuarterlyKpisLast3Years_ShouldMatch_CTEExpected_ForGesamtumsatz()
        {
            Assert.IsNotNull(DbContext);
            Assert.IsNotNull(_repo);

            await EnsureCtSeededAsync();

            IReadOnlyList<QuarterlyKpiRowDto> actual = await _repo.GetQuarterlyKpisLast3YearsAsync();

            // 5 categories * 12 quarters = 60 rows
            Assert.AreEqual(60, actual.Count);

            var actualTotals = actual
                .Where(r => r.Category == "Gesamtumsatz")
                .OrderBy(r => r.Year).ThenBy(r => r.Quarter)
                .ToList();

            Assert.AreEqual(12, actualTotals.Count);

            List<ExpectedRow> expectedTotals = await QueryExpectedGesamtumsatzAsync();
            Assert.AreEqual(12, expectedTotals.Count);

            for (int i = 0; i < 12; i++)
            {
                QuarterlyKpiRowDto a = actualTotals[i];
                ExpectedRow e = expectedTotals[i];

                Assert.AreEqual(e.Year, a.Year);
                Assert.AreEqual(e.Quarter, a.Quarter);

                Assert.AreEqual(e.Value, a.Value);
                Assert.AreEqual(e.PrevYearValue, a.PrevYearValue);
                Assert.AreEqual(e.YoYDelta, a.YoYDelta);
                Assert.AreEqual(e.YoYDeltaPercent, a.YoYDeltaPercent);
            }
        }

        // -------------------------
        // Seed: CT orders only
        // -------------------------

        private async Task EnsureCtSeededAsync()
        {
            Assert.IsNotNull(DbContext);

            int ordersExists = await ExecuteScalarIntAsync(@"
SELECT CASE WHEN OBJECT_ID('dbo.Orders','U') IS NOT NULL THEN 1 ELSE 0 END;");
            Assert.AreEqual(1, ordersExists, "dbo.Orders does not exist.");

            // deterministic: delete only CT rows
            _ = await DbContext.Database.ExecuteSqlRawAsync("DELETE FROM dbo.Orders WHERE OrderNumber LIKE 'CT-%';");

            // ensure at least one customer exists and get a valid id
            int customerId = await EnsureAtLeastOneCustomerAsync();

            int y = DateTime.UtcNow.Year;

            for (int year = y - 2; year <= y; year++)
            {
                int yearOffset = (year == y - 2) ? 0 : (year == y - 1) ? 200 : 400;

                for (int q = 1; q <= 4; q++)
                {
                    int qOffset = q switch { 1 => 0, 2 => 50, 3 => 100, 4 => 150, _ => 0 };
                    decimal total = 1000m + yearOffset + qOffset;

                    DateTime date = q switch
                    {
                        1 => new DateTime(year, 2, 15),
                        2 => new DateTime(year, 5, 15),
                        3 => new DateTime(year, 8, 15),
                        4 => new DateTime(year, 11, 15),
                        _ => new DateTime(year, 1, 1),
                    };

                    string orderNumber = $"CT-{year}-Q{q}";

                    // Generate unique OrderId for each order
                    int orderId = (year * 100) + (q * 10);

                    _ = await DbContext.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO dbo.Orders
(
    OrderId,
    OrderNumber,
    CustomerId,
    OrderDate,
    TotalAmount,
    TotalCurrency,
    DeliveryStreet,
    DeliveryHouseNumber,
    DeliveryPostalCode,
    DeliveryCity,
    DeliveryCountryCode
)
VALUES
(
    {orderId},
    {orderNumber},
    {customerId},
    {date},
    {total},
    {"CHF"},
    {"Teststreet"},
    {"1"},
    {"9000"},
    {"St. Gallen"},
    {"CH"}
);
");
                }
            }
        }

        /// <summary>
        /// Ensures dbo.Customers has at least one row, without guessing column names.
        /// Inserts generic values for all NOT NULL, non-identity, non-computed columns.
        /// Returns a valid CustomerId.
        /// </summary>
        private async Task<int> EnsureAtLeastOneCustomerAsync()
        {
            Assert.IsNotNull(DbContext);

            int customersTableExists = await ExecuteScalarIntAsync(@"
SELECT CASE WHEN OBJECT_ID('dbo.Customers','U') IS NOT NULL THEN 1 ELSE 0 END;");
            if (customersTableExists != 1)
            {
                // If there is no Customers table, Orders.CustomerId should not be FK-enforced.
                return 1;
            }

            object? existingId = await ExecuteScalarAsync(@"
SELECT TOP (1) CustomerId
FROM dbo.Customers
ORDER BY CustomerId;");

            if (existingId is not null and not DBNull)
                return Convert.ToInt32(existingId);

            // No rows -> insert one
            return await InsertGenericCustomerRowAndReturnIdAsync();
        }

        private sealed record CustomerColumn(
            string Name,
            string SqlType,
            bool IsNullable,
            bool IsIdentity,
            bool IsComputed,
            bool IsRowVersion,
            bool IsPeriod);

        private async Task<int> InsertGenericCustomerRowAndReturnIdAsync()
        {
            Assert.IsNotNull(DbContext);

            // Pull column metadata from sys.* (works for your Testcontainers SQL Server)
            List<CustomerColumn> cols = await QueryCustomerColumnsAsync();

            // Try to detect CustomerId identity
            bool hasCustomerId = cols.Any(c => c.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase));
            bool customerIdIsIdentity = cols.Any(c => c.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase) && c.IsIdentity);

            // Columns we can/should write:
            // - not computed
            // - not rowversion/timestamp
            // - not temporal period columns
            // - not identity (unless we must provide CustomerId)
            var writable = cols
                .Where(c => !c.IsComputed && !c.IsRowVersion && !c.IsPeriod)
                .ToList();

            // required columns (NOT NULL)
            var required = writable.Where(c => !c.IsNullable).ToList();

            // If CustomerId is NOT identity, we must provide it when required
            // (also: even if nullable=false and not identity, we provide it)
            if (hasCustomerId && !customerIdIsIdentity)
            {
                // Ensure CustomerId is included (if not already)
                if (!required.Any(c => c.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase)))
                {
                    CustomerColumn idCol = writable.First(c => c.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase));
                    required.Insert(0, idCol);
                }
            }

            // Build INSERT
            var insertCols = new List<string>();
            var insertVals = new List<string>();

            foreach (CustomerColumn? c in required)
            {
                // Skip identity columns (unless CustomerId is not identity and we must provide it)
                if (c.IsIdentity && !(c.Name.Equals("CustomerId", StringComparison.OrdinalIgnoreCase) && !customerIdIsIdentity))
                    continue;

                insertCols.Add($"[{c.Name}]");
                insertVals.Add(GetGenericSqlLiteral(c.SqlType, c.Name, customerIdIsIdentity));
            }

            if (insertCols.Count == 0)
                throw new InvalidOperationException("Could not determine any writable NOT NULL columns for dbo.Customers.");

            // If CustomerId is identity, use SCOPE_IDENTITY() to get it.
            // If not identity, we set CustomerId=1 (or any deterministic value) and return it.
            string sql;
            if (hasCustomerId && customerIdIsIdentity)
            {
                sql = $@"
INSERT INTO dbo.Customers ({string.Join(", ", insertCols)})
VALUES ({string.Join(", ", insertVals)});
SELECT CAST(SCOPE_IDENTITY() AS int);
";
                object? id = await ExecuteScalarAsync(sql);
                return id is null or DBNull
                    ? throw new InvalidOperationException("Inserted dbo.Customers row but could not read identity.")
                    : Convert.ToInt32(id);
            }
            else
            {
                // CustomerId not identity (or not present) -> insert and then read TOP(1)
                sql = $@"
INSERT INTO dbo.Customers ({string.Join(", ", insertCols)})
VALUES ({string.Join(", ", insertVals)});
SELECT TOP (1) CustomerId FROM dbo.Customers ORDER BY CustomerId;
";
                object? id = await ExecuteScalarAsync(sql);
                return id is null or DBNull
                    ? throw new InvalidOperationException("Inserted dbo.Customers row but could not read CustomerId.")
                    : Convert.ToInt32(id);
            }
        }

        private static string GetGenericSqlLiteral(string sqlType, string columnName, bool customerIdIsIdentity)
        {
            // Special-case deterministic CustomerId if needed (when not identity)
            if (columnName.Equals("CustomerId", StringComparison.OrdinalIgnoreCase) && !customerIdIsIdentity)
                return "1";

            // Normalize type name (strip length/precision)
            string t = sqlType.ToLowerInvariant();

            // Common SQL Server types
            if (t.StartsWith("int") || t.StartsWith("bigint") || t.StartsWith("smallint") || t.StartsWith("tinyint"))
                return "1";
            if (t.StartsWith("bit"))
                return "0";
            if (t.StartsWith("decimal") || t.StartsWith("numeric") || t.StartsWith("money") || t.StartsWith("smallmoney") || t.StartsWith("float") || t.StartsWith("real"))
                return "0";
            if (t.StartsWith("uniqueidentifier"))
                return "NEWID()";
            if (t.StartsWith("date") || t.StartsWith("datetime") || t.StartsWith("datetime2") || t.StartsWith("smalldatetime") || t.StartsWith("datetimeoffset"))
                return "GETUTCDATE()";

            // Strings / text
            if (t.StartsWith("nvarchar") || t.StartsWith("varchar") || t.StartsWith("nchar") || t.StartsWith("char") || t.StartsWith("text") || t.StartsWith("ntext"))
                return "N'TEST'";

            // Fallback: try string
            return "N'TEST'";
        }

        private async Task<List<CustomerColumn>> QueryCustomerColumnsAsync()
        {
            const string sql = @"
SELECT
    c.name                             AS ColumnName,
    t.name                             AS TypeName,
    c.is_nullable                      AS IsNullable,
    c.is_identity                      AS IsIdentity,
    c.is_computed                      AS IsComputed,
    CASE WHEN t.name IN ('timestamp','rowversion') THEN 1 ELSE 0 END AS IsRowVersion,
    CASE WHEN c.generated_always_type IN (1,2) THEN 1 ELSE 0 END     AS IsPeriod
FROM sys.columns c
JOIN sys.types   t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.Customers')
ORDER BY c.column_id;
";

            var result = new List<CustomerColumn>();

            DbConnection conn = DbContext!.Database.GetDbConnection();
            await using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await using DbDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string name = reader.GetString(0);
                string type = reader.GetString(1);
                bool isNullable = reader.GetBoolean(2);
                bool isIdentity = reader.GetBoolean(3);
                bool isComputed = reader.GetBoolean(4);
                bool isRowVersion = reader.GetInt32(5) == 1;
                bool isPeriod = reader.GetInt32(6) == 1;

                result.Add(new CustomerColumn(name, type, isNullable, isIdentity, isComputed, isRowVersion, isPeriod));
            }

            return result;
        }

        // -------------------------
        // Expected: CTE + window function (LAG)
        // -------------------------

        private sealed record ExpectedRow(
            int Year,
            int Quarter,
            decimal Value,
            decimal? PrevYearValue,
            decimal? YoYDelta,
            decimal? YoYDeltaPercent);

        private async Task<List<ExpectedRow>> QueryExpectedGesamtumsatzAsync()
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
Agg AS (
    SELECT
        [Year]    = YEAR(o.OrderDate),
        [Quarter] = DATEPART(QUARTER, o.OrderDate),
        TotalRevenue = CAST(SUM(o.TotalAmount) AS decimal(18,2))
    FROM dbo.Orders o
    WHERE YEAR(o.OrderDate) BETWEEN (@y - 2) AND @y
    GROUP BY YEAR(o.OrderDate), DATEPART(QUARTER, o.OrderDate)
),
Series AS (
    SELECT
        c.[Year],
        c.[Quarter],
        Value = COALESCE(a.TotalRevenue, CAST(0 AS decimal(18,2)))
    FROM Calendar c
    LEFT JOIN Agg a
        ON a.[Year] = c.[Year] AND a.[Quarter] = c.[Quarter]
),
YoY AS (
    SELECT
        [Year],
        [Quarter],
        Value,
        PrevYearValue = LAG(Value, 1) OVER (PARTITION BY [Quarter] ORDER BY [Year]),
        YoYDelta      = Value - LAG(Value, 1) OVER (PARTITION BY [Quarter] ORDER BY [Year]),
        YoYDeltaPercent =
            CASE
                WHEN LAG(Value, 1) OVER (PARTITION BY [Quarter] ORDER BY [Year]) IS NULL THEN NULL
                WHEN LAG(Value, 1) OVER (PARTITION BY [Quarter] ORDER BY [Year]) = 0 THEN NULL
                ELSE CAST(
                    (Value - LAG(Value, 1) OVER (PARTITION BY [Quarter] ORDER BY [Year]))
                    / LAG(Value, 1) OVER (PARTITION BY [Quarter] ORDER BY [Year]) * 100.0
                AS decimal(18,2))
            END
    FROM Series
)
SELECT [Year], [Quarter], Value, PrevYearValue, YoYDelta, YoYDeltaPercent
FROM YoY
ORDER BY [Year], [Quarter];
";

            return await QueryExpectedRowsAsync(sql);
        }

        // -------------------------
        // Low-level helpers
        // -------------------------

        private async Task<int> ExecuteScalarIntAsync(string sql)
            => Convert.ToInt32(await ExecuteScalarAsync(sql));

        private async Task<object?> ExecuteScalarAsync(string sql)
        {
            DbConnection conn = DbContext!.Database.GetDbConnection();
            await using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            return await cmd.ExecuteScalarAsync();
        }

        private async Task<List<ExpectedRow>> QueryExpectedRowsAsync(string sql)
        {
            var result = new List<ExpectedRow>();

            DbConnection conn = DbContext!.Database.GetDbConnection();
            await using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            await using DbDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int year = reader.GetInt32(0);
                int quarter = reader.GetInt32(1);
                decimal value = reader.GetDecimal(2);

                decimal? prev = reader.IsDBNull(3) ? null : reader.GetDecimal(3);
                decimal? delta = reader.IsDBNull(4) ? null : reader.GetDecimal(4);
                decimal? pct = reader.IsDBNull(5) ? null : reader.GetDecimal(5);

                result.Add(new ExpectedRow(year, quarter, value, prev, delta, pct));
            }

            return result;
        }
    }
}
