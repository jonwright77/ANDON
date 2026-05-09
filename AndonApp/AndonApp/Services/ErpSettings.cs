namespace AndonApp.Services;

public class ErpSettings
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = string.Empty;

    // Live polling query (auto-refreshed)
    public string Query { get; set; } = string.Empty;
    public string PoolColumn { get; set; } = "Pool";
    public string QuantityColumn { get; set; } = "Quantity";
    public int RefreshIntervalSeconds { get; set; } = 60;

    // Historical build query — cached locally; background service refreshes on this interval
    public string HistoryQuery { get; set; } = string.Empty;
    public string HistoryDateColumn { get; set; } = "Timestamp";
    public int HistoryRefreshIntervalSeconds { get; set; } = 300;
}
