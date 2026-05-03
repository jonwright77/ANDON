using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AndonApp.Services;

public class ErpDataService : IErpDataService
{
    private readonly IOptionsMonitor<ErpSettings> _options;
    private readonly ILogger<ErpDataService> _logger;

    public ErpDataService(IOptionsMonitor<ErpSettings> options, ILogger<ErpDataService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task<Dictionary<string, int>> FetchBuiltByPoolAsync()
        => FetchAsync(_options.CurrentValue);

    public async Task<List<Dictionary<string, object?>>> TestConnectionAsync(ErpSettings settings, int maxRows = 5)
    {
        ValidateSelectQuery(settings.Query);
        var rows = new List<Dictionary<string, object?>>();
        await using var conn = new SqlConnection(settings.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(settings.Query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        int count = 0;
        while (await reader.ReadAsync() && count < maxRows)
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
            count++;
        }
        return rows;
    }

    private static void ValidateSelectQuery(string query)
    {
        var trimmed = query.Trim();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ERP query must start with SELECT.");
        if (trimmed.Contains(';'))
            throw new InvalidOperationException("ERP query must not contain semicolons (multi-statement queries are not permitted).");
    }

    private static async Task<Dictionary<string, int>> FetchAsync(ErpSettings settings)
    {
        ValidateSelectQuery(settings.Query);
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using var conn = new SqlConnection(settings.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(settings.Query, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var pool = reader[settings.PoolColumn]?.ToString();
            if (string.IsNullOrEmpty(pool)) continue;
            if (int.TryParse(reader[settings.QuantityColumn]?.ToString(), out var qty))
                result[pool] = qty;
        }
        return result;
    }
}
