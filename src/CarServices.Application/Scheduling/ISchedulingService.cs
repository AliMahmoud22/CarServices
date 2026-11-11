namespace CarServices.Application.Scheduling;

public interface ISchedulingService
{
    Task<SlotsQueryResult> GetAvailableSlotsAsync(
        DateOnly date,
        int modelId,
        IReadOnlyCollection<int> serviceIds,
        CancellationToken ct = default);
}

