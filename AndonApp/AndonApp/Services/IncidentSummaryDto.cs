using AndonApp.Data.Models;

namespace AndonApp.Services;

public record IncidentSummaryDto(
    int Id,
    int ProductionLineId,
    Severity Severity,
    string AndonCode,
    string? AndonCodeName,
    string? AdditionalInfo,
    DateTime CreatedAt
);
