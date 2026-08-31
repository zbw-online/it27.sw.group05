using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Catalog.ReconcileInventory
{
    public interface IReconcileInventoryUseCase
    {
        Task<Result<ReconciliationReportDto>> ExecuteAsync(
            ReconcileInventoryCommand command,
            CancellationToken cancellationToken = default);
    }
}
