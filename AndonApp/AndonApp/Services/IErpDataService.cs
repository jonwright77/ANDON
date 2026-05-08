namespace AndonApp.Services;

public record ErpBuildPoint(string Pool, DateTime Timestamp, int Quantity);

public interface IErpDataService
{
    Task<Dictionary<string, int>> FetchBuiltByPoolAsync();
    Task<List<ErpBuildPoint>> FetchBuildHistoryAsync();
    Task<List<Dictionary<string, object?>>> TestConnectionAsync(ErpSettings settings, int maxRows = 5);
    Task<List<Dictionary<string, object?>>> TestHistoryConnectionAsync(ErpSettings settings, int maxRows = 10);
}
