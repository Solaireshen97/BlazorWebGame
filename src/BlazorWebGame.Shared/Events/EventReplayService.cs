using BlazorWebGame.Shared.Events;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Shared.Events;

/// <summary>
/// 事件重放服务
/// </summary>
public class EventReplayService
{
    private readonly IRedisEventPersistence _persistence;
    private readonly UnifiedEventQueue _eventQueue;
    private readonly ILogger _logger;

    public EventReplayService(IRedisEventPersistence persistence, UnifiedEventQueue eventQueue, ILogger logger)
    {
        _persistence = persistence;
        _eventQueue = eventQueue;
        _logger = logger;
    }

    /// <summary>
    /// 重放指定帧的事件
    /// </summary>
    public async Task ReplayFrameAsync(ulong frame)
    {
        try
        {
            var events = await _persistence.ReplayFrameAsync(frame);
            
            foreach (var evt in events)
            {
                var replayEvent = evt;
                _eventQueue.Enqueue(ref replayEvent);
            }

            _logger.LogInformation("Replayed {Count} events from frame {Frame}", events.Length, frame);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying frame {Frame}", frame);
        }
    }

    /// <summary>
    /// 重放指定时间范围的事件
    /// </summary>
    public async Task ReplayTimeRangeAsync(ulong startFrame, ulong endFrame)
    {
        for (ulong frame = startFrame; frame <= endFrame; frame++)
        {
            await ReplayFrameAsync(frame);
        }
    }
}