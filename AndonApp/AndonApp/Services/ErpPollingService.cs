using AndonApp.Data;
using AndonApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AndonApp.Services;

public class ErpPollingService : BackgroundService
{
    private readonly IOptionsMonitor<ErpSettings> _options;
    private readonly ErpPollStatus _pollStatus;
    private readonly IHubContext<AndonHub> _hubContext;
    private readonly IDbContextFactory<AndonDbContext> _dbFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErpPollingService> _logger;

    public ErpPollingService(
        IOptionsMonitor<ErpSettings> options,
        ErpPollStatus pollStatus,
        IHubContext<AndonHub> hubContext,
        IDbContextFactory<AndonDbContext> dbFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<ErpPollingService> logger)
    {
        _options = options;
        _pollStatus = pollStatus;
        _hubContext = hubContext;
        _dbFactory = dbFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = _options.CurrentValue;

            if (!settings.Enabled
                || string.IsNullOrWhiteSpace(settings.ConnectionString)
                || string.IsNullOrWhiteSpace(settings.Query))
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                continue;
            }

            try
            {
                Dictionary<string, int> results;
                await using (var scope = _scopeFactory.CreateAsyncScope())
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IErpDataService>();
                    results = await svc.FetchBuiltByPoolAsync();
                }

                _pollStatus.Update(true, null, results);
                _logger.LogDebug("ERP poll succeeded: {RowCount} pool(s) returned", results.Count);

                await BroadcastResultsAsync(results, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERP poll failed");
                _pollStatus.Update(false, ex.Message, null);
            }

            var interval = Math.Max(10, _options.CurrentValue.RefreshIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }

    private async Task BroadcastResultsAsync(Dictionary<string, int> results, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var lines = await db.ProductionLines
            .Where(l => l.IsActive && l.Pool != null)
            .Select(l => new { l.Slug, l.Pool })
            .ToListAsync(ct);

        foreach (var line in lines)
        {
            if (line.Pool != null && results.TryGetValue(line.Pool, out var qty))
            {
                await _hubContext.Clients
                    .Group($"line:{line.Slug}")
                    .SendAsync("BuiltUpdated", line.Slug, qty, ct);
            }
        }
    }
}
