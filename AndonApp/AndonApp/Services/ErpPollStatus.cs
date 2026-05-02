namespace AndonApp.Services;

public class ErpPollStatus
{
    private readonly object _lock = new();

    private DateTime? _lastRunAt;
    private bool _lastRunSucceeded;
    private string? _lastError;
    private int _lastRowCount;
    private Dictionary<string, int> _lastResults = new(StringComparer.OrdinalIgnoreCase);

    public void Update(bool succeeded, string? error, Dictionary<string, int>? results)
    {
        lock (_lock)
        {
            _lastRunAt = DateTime.UtcNow;
            _lastRunSucceeded = succeeded;
            _lastError = error;
            if (results != null)
            {
                _lastResults = results;
                _lastRowCount = results.Count;
            }
            if (!succeeded)
                _lastRowCount = 0;
        }
    }

    public (DateTime? at, bool succeeded, string? error, int rowCount, Dictionary<string, int> results) GetSnapshot()
    {
        lock (_lock)
        {
            return (_lastRunAt, _lastRunSucceeded, _lastError, _lastRowCount,
                new Dictionary<string, int>(_lastResults, StringComparer.OrdinalIgnoreCase));
        }
    }
}
