using System.Collections.Concurrent;

namespace AndonApp.Services;

public class LoginAttemptTracker
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public bool IsLockedOut(string username)
    {
        var key = username.ToLowerInvariant();
        if (!_entries.TryGetValue(key, out var entry)) return false;
        if (entry.LockedUntil > DateTime.UtcNow) return true;
        _entries.TryRemove(key, out _);
        return false;
    }

    public TimeSpan LockoutRemaining(string username)
    {
        var key = username.ToLowerInvariant();
        if (!_entries.TryGetValue(key, out var entry)) return TimeSpan.Zero;
        var remaining = entry.LockedUntil - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public void RecordFailure(string username)
    {
        var key = username.ToLowerInvariant();
        var entry = _entries.GetOrAdd(key, _ => new Entry());
        lock (entry)
        {
            entry.FailureCount++;
            if (entry.FailureCount >= MaxFailures)
                entry.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
        }
    }

    public void RecordSuccess(string username)
    {
        _entries.TryRemove(username.ToLowerInvariant(), out _);
    }

    private class Entry
    {
        public int FailureCount { get; set; }
        public DateTime LockedUntil { get; set; } = DateTime.MinValue;
    }
}
