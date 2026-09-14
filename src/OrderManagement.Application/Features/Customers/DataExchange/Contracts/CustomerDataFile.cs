namespace OrderManagement.Application.Features.Customers.DataExchange.Contracts
{
    public sealed record CustomerDataFile(
        string SafeFileName,
        CustomerDataFormat Format,
        string MediaType,
        byte[] Content);
}
