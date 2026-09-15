using System.Collections.Concurrent;
using Bolt.Automation.WorkerAgent.Models;

namespace Bolt.Automation.WorkerAgent.Logging;

/// <summary>
/// Thread-safe ring buffer for log entries.
/// Drops oldest entries when capacity is exceeded.
/// </summary>
public sealed class LogBuffer
{
    private readonly ConcurrentQueue<LogEntryDto> _queue = new();
    private readonly int _maxEntries;
    private int _count;
    private long _droppedCount;

    public LogBuffer(int maxEntries = 10_000)
    {
        _maxEntries = maxEntries;
    }

    public void Add(LogEntryDto entry)
    {
        _queue.Enqueue(entry);
        var current = Interlocked.Increment(ref _count);

        // Trim oldest if over capacity
        while (current > _maxEntries && _queue.TryDequeue(out _))
        {
            Interlocked.Increment(ref _droppedCount);
            current = Interlocked.Decrement(ref _count);
        }
    }

    public LogEntryDto[] Drain(int maxCount)
    {
        var entries = new List<LogEntryDto>(Math.Min(maxCount, 500));

        for (int i = 0; i < maxCount && _queue.TryDequeue(out var entry); i++)
        {
            entries.Add(entry);
            Interlocked.Decrement(ref _count);
        }

        return entries.ToArray();
    }

    public int Count => Volatile.Read(ref _count);

    /// <summary>Total number of entries dropped due to buffer overflow since startup.</summary>
    public long DroppedCount => Volatile.Read(ref _droppedCount);
}
