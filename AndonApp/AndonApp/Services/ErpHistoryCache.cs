using AndonApp.Data;
using AndonApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AndonApp.Services;

/// <summary>
/// Singleton that owns the ERP history cache: populates the local ErpBuildCaches table
/// from the configured history SQL query and tracks refresh status.
/// Both the background polling service and the admin UI "Refresh Now" button use this.
/// </summary>
public class ErpHistoryCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<ErpSettings> _options;
    private readonly ILogger<ErpHistoryCache> _logger;
    private readonly SemaphoreSlim _sem = new(1, 1);

    private DateTime? _lastRefreshed;
    private int _cachedCount;
    private string? _lastError;
    private bool _isRefreshing;

    public bool IsRefreshing   => _isRefreshing;
    public DateTime? LastRefreshed => _lastRefreshed;
    public int CachedCount     => _cachedCount;
    public string? LastError   => _lastError;

    public ErpHistoryCache(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<ErpSettings> options,
        ILogger<ErpHistoryCache> logger)
    {
        _scopeFactory = scopeFactory;
        _options      = options;
        _logger       = logger;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        // Only one refresh at a time; skip if already running
        if (!await _sem.WaitAsync(0, ct)) return;
        _isRefreshing = true;
        try
        {
            var settings = _options.CurrentValue;
            if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.HistoryQuery))
            {
                _lastError = "ERP not enabled or history query not configured.";
                return;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var erpService = scope.ServiceProvider.GetRequiredService<IErpDataService>();
            var dbFactory  = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AndonDbContext>>();

            var points = await erpService.FetchBuildHistoryAsync();

            await using var db = await dbFactory.CreateDbContextAsync(ct);
            await db.ErpBuildCaches.ExecuteDeleteAsync(ct);

            if (points.Count > 0)
            {
                db.ErpBuildCaches.AddRange(points.Select(p => new ErpBuildCache
                {
                    Pool      = p.Pool,
                    Timestamp = p.Timestamp,
                    Quantity  = p.Quantity
                }));
                await db.SaveChangesAsync(ct);
            }

            _lastRefreshed = DateTime.UtcNow;
            _cachedCount   = points.Count;
            _lastError     = null;
            _logger.LogInformation("ERP history cache refreshed: {Count} record(s)", points.Count);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            _logger.LogError(ex, "ERP history cache refresh failed");
        }
        finally
        {
            _isRefreshing = false;
            _sem.Release();
        }
    }
}
