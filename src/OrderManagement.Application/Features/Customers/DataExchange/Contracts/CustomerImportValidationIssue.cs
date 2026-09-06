namespace OrderManagement.Application.Features.Customers.DataExchange.Contracts
{
    public sealed record CustomerImportValidationIssue(
        int? RecordIndex,
        string? CustomerNumber,
        string Field,
        string Message);
}
