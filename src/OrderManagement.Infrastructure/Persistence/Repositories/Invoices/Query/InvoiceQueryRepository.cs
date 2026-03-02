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
            int? customerId = null,
            CancellationToken ct = default)
        {
            // SQL-Query verwendet temporale Abfrage (FOR SYSTEM_TIME AS OF) um die 
            // zum Rechnungsdatum gültige Adresse zu ermitteln
            const string sql = @"
                SELECT 
                    i.CustomerId AS Kundennummer,
                    c.CompanyName AS Name,
                    ca.Street AS Strasse,
                    ca.PostalCode AS PLZ,
                    ca.City AS Ort,
                    ca.Country AS Land,
                    i.InvoiceDate AS Rechnungsdatum,
                    i.InvoiceNumber AS Rechnungsnummer,
                    i.NetAmount AS RechnungsbetragNetto,
                    i.GrossAmount AS RechnungsbetragBrutto
                FROM dbo.Invoices i
                INNER JOIN dbo.Customers c 
                    ON c.CustomerId = i.CustomerId
                INNER JOIN dbo.CustomerAddresses FOR SYSTEM_TIME AS OF i.InvoiceDate AS ca
                    ON ca.CustomerId = i.CustomerId
                WHERE 
                    (@FromDate IS NULL OR i.InvoiceDate >= @FromDate)
                    AND (@ToDate IS NULL OR i.InvoiceDate <= @ToDate)
                    AND (@CustomerId IS NULL OR i.CustomerId = @CustomerId)
                ORDER BY i.InvoiceDate DESC, i.InvoiceNumber
                ";

            var fromDateParam = new Microsoft.Data.SqlClient.SqlParameter("@FromDate", (object?)fromDate ?? DBNull.Value);
            var toDateParam = new Microsoft.Data.SqlClient.SqlParameter("@ToDate", (object?)toDate ?? DBNull.Value);
            var customerIdParam = new Microsoft.Data.SqlClient.SqlParameter("@CustomerId", (object?)customerId ?? DBNull.Value);

            return await _db.Database
                .SqlQueryRaw<InvoiceDto>(sql, fromDateParam, toDateParam, customerIdParam)
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
