namespace CarServices.Application.Scheduling;

public record SlotDto(
    TimeOnly Time,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    bool IsAvailable);

public record AvailableSlotsResponse(
    DateOnly Date,
    bool IsClosed,
    string? ClosedReason,
    int TotalDurationMinutes,
    List<SlotDto> Slots);

public record SlotsQueryResult(
    bool IsSuccess,
    string? ErrorMessage,
    AvailableSlotsResponse? Response)
{
    public static SlotsQueryResult Success(AvailableSlotsResponse response) =>
        new(true, null, response);

    public static SlotsQueryResult Failure(string errorMessage) =>
        new(false, errorMessage, null);
}

