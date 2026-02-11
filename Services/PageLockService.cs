using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace PageEditingApp.Services;

public class PageLockService
{
    private readonly ConcurrentDictionary<string, PageLock> _locks = new();
    private readonly ConcurrentDictionary<string, StreamWriter> _clients = new();

    public class PageLock
    {
        public string PageId { get; set; } = string.Empty;
        public string LockedBy { get; set; } = string.Empty;
        public DateTime LockedAt { get; set; }
    }

    public async Task RegisterClient(string clientId, Stream responseStream, CancellationToken cancellationToken)
    {
        var writer = new StreamWriter(responseStream, new UTF8Encoding(false))
        {
            AutoFlush = false
        };

        _clients.TryAdd(clientId, writer);

        try
        {
            // Send initial connection message
            await writer.WriteAsync("event: connected\n");
            await writer.WriteAsync("data: {\"type\":\"connected\"}\n\n");
            await writer.FlushAsync(cancellationToken);

            // Send current locks state
            var currentLocks = GetAllLocks();
            if (currentLocks.Count > 0)
            {
                foreach (var item in currentLocks)
                {
                    var lockJson = JsonSerializer.Serialize(new
                    {
                        type = "page-locked",
                        pageId = item.PageId,
                        lockedBy = item.LockedBy,
                        lockedAt = item.LockedAt
                    });
                    await writer.WriteAsync("event: page-locked\n");
                    await writer.WriteAsync($"data: {lockJson}\n\n");
                }
                await writer.FlushAsync(cancellationToken);
            }

            // Keep connection alive
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(15000, cancellationToken); // Heartbeat every 15 seconds
                await writer.WriteAsync(": heartbeat\n\n");
                await writer.FlushAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // Client disconnected
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            await writer.DisposeAsync();
        }
    }

    public async Task<bool> LockPage(string pageId, string userId)
    {
        var pageLock = new PageLock
        {
            PageId = pageId,
            LockedBy = userId,
            LockedAt = DateTime.UtcNow
        };

        var added = _locks.TryAdd(pageId, pageLock);

        if (added)
        {
            await BroadcastEvent(new
            {
                type = "page-locked",
                pageId,
                lockedBy = userId,
                lockedAt = pageLock.LockedAt
            });
        }

        return added;
    }

    public async Task UnlockPage(string pageId)
    {
        if (_locks.TryRemove(pageId, out _))
        {
            await BroadcastEvent(new
            {
                type = "page-unlocked",
                pageId
            });
        }
    }

    public List<PageLock> GetAllLocks()
    {
        return _locks.Values.ToList();
    }

    private async Task BroadcastEvent(object eventData)
    {
        var json = JsonSerializer.Serialize(eventData);
        
        // Extract the type from the event data to use as event name
        using var doc = JsonDocument.Parse(json);
        var eventType = doc.RootElement.GetProperty("type").GetString() ?? "message";
        
        var message = $"event: {eventType}\ndata: {json}\n\n";

        var disconnectedClients = new List<string>();

        foreach (var (clientId, writer) in _clients)
        {
            try
            {
                await writer.WriteAsync(message);
                await writer.FlushAsync();
            }
            catch
            {
                disconnectedClients.Add(clientId);
            }
        }

        // Clean up disconnected clients
        foreach (var clientId in disconnectedClients)
        {
            _clients.TryRemove(clientId, out _);
        }
    }
}
