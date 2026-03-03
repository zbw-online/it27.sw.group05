using Microsoft.EntityFrameworkCore;

using OrderManagement.Application.Abstractions.Interfaces.Invoices.Query;
using OrderManagement.Application.DTOs.Invoices;

namespace OrderManagement.Infrastructure.Persistence.Repositories.Invoices.Query
{
    public sealed class InvoiceQueryRepository(OrderManagementDbContext db) : IInvoiceQueryRepository
    {
        private readonly OrderManagementDbContext _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<IReadOnlyList<InvoiceDto>> GetOrdersWithHistoricalAddressAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? customerNumber = null,
            CancellationToken ct = default)
        {
            const string sql = @"
                SELECT 
                    c.CustomerNumber AS Kundennummer,
                    CONCAT(c.LastName, ' ', c.SurName) AS Name,
                    CONCAT(ca.Street, ' ', ca.HouseNumber) AS Strasse,
                    ca.PostalCode AS PLZ,
                    ca.City AS Ort,
                    CASE ca.CountryCode 
                        WHEN 'CH' THEN 'Schweiz'
                        WHEN 'DE' THEN 'Deutschland'
                        WHEN 'AT' THEN 'Österreich'
                        ELSE ca.CountryCode 
                    END AS Land,
                    o.OrderDate AS Rechnungsdatum,
                    o.OrderNumber AS Rechnungsnummer,
                    CAST(0 AS DECIMAL(18,2)) AS RechnungsbetragNetto,
                    CAST(0 AS DECIMAL(18,2)) AS RechnungsbetragBrutto
                FROM dbo.Orders o
                INNER JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
                OUTER APPLY (
                    SELECT TOP 1 
                        Street, HouseNumber, PostalCode, City, CountryCode
                    FROM dbo.CustomerAddresses
                    WHERE CustomerId = o.CustomerId
                      AND ValidFrom <= CAST(o.OrderDate AS DATE)
                      AND (ValidTo IS NULL OR ValidTo >= CAST(o.OrderDate AS DATE))
                    ORDER BY ValidFrom DESC
                ) ca
                WHERE 
                    (@FromDate IS NULL OR o.OrderDate >= @FromDate)
                    AND (@ToDate IS NULL OR o.OrderDate <= @ToDate)
                    AND (@CustomerNumber IS NULL OR c.CustomerNumber = @CustomerNumber)
                ORDER BY o.OrderDate DESC, o.OrderNumber
                ";

            var fromDateParam = new Microsoft.Data.SqlClient.SqlParameter("@FromDate", (object?)fromDate ?? DBNull.Value);
            var toDateParam = new Microsoft.Data.SqlClient.SqlParameter("@ToDate", (object?)toDate ?? DBNull.Value);
            var customerNumberParam = new Microsoft.Data.SqlClient.SqlParameter("@CustomerNumber", (object?)customerNumber ?? DBNull.Value);

            return await _db.Database
                .SqlQueryRaw<InvoiceDto>(sql, fromDateParam, toDateParam, customerNumberParam)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
