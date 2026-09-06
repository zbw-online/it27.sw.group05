using OrderManagement.Domain.Customers;

namespace OrderManagement.Application.Features.Customers.DataExchange.Contracts
{
    public sealed record CustomerImportPlan(
        IReadOnlyList<Customer> CustomersToImport,
        IReadOnlyList<CustomerImportValidationIssue> Issues,
        int TotalRecordCount)
    {
        public bool IsValid => Issues.Count == 0;
    }
}
