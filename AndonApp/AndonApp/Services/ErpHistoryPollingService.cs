using Microsoft.Extensions.Options;

namespace AndonApp.Services;

public class ErpHistoryPollingService : BackgroundService
{
    private readonly ErpHistoryCache _cache;
    private readonly IOptionsMonitor<ErpSettings> _options;
    private readonly ILogger<ErpHistoryPollingService> _logger;

    public ErpHistoryPollingService(
        ErpHistoryCache cache,
        IOptionsMonitor<ErpSettings> options,
        ILogger<ErpHistoryPollingService> logger)
    {
        _cache   = cache;
        _options = options;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Short startup delay so the app is fully ready before the first ERP hit
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await _cache.RefreshAsync(stoppingToken);

            var interval = Math.Max(60, _options.CurrentValue.HistoryRefreshIntervalSeconds);
            _logger.LogDebug("ERP history cache: next refresh in {Interval}s", interval);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }
}
