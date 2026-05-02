namespace AndonApp.Services;

public class ErpSettings
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string PoolColumn { get; set; } = "Pool";
    public string QuantityColumn { get; set; } = "Quantity";
    public int RefreshIntervalSeconds { get; set; } = 60;
}
