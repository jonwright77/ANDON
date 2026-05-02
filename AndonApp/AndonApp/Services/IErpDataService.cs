namespace AndonApp.Services;

public interface IErpDataService
{
    Task<Dictionary<string, int>> FetchBuiltByPoolAsync();
    Task<List<Dictionary<string, object?>>> TestConnectionAsync(ErpSettings settings, int maxRows = 5);
}
