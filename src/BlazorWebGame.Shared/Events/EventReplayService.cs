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
    public async Task<int> ReplayFrameAsync(ulong frame)
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
            return events.Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replaying frame {Frame}", frame);
            return 0;
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

    /// <summary>
    /// 验证帧完整性
    /// </summary>
    public async Task<FrameIntegrityReport> ValidateFrameIntegrityAsync(ulong frameNumber, ulong expectedEventCount)
    {

        try
        {
            var events = await _persistence.LoadFrameAsync(frameNumber);
            var actualCount = (ulong)events.Length;
            
            var report = new FrameIntegrityReport
            {
                StartFrame = frameNumber,
                EndFrame = frameNumber,
                ValidFrames = actualCount == expectedEventCount ? 1 : 0,
                EmptyFrames = actualCount == 0 ? 1 : 0
            };

            if (actualCount != expectedEventCount)
            {
                if (actualCount == 0)
                {
                    report.MissingFrames.Add(frameNumber);
                }
                else
                {
                    report.CorruptFrames.Add(frameNumber);
                }
            }
            
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating frame integrity for frame {Frame}", frameNumber);
            var report = new FrameIntegrityReport
            {
                StartFrame = frameNumber,
                EndFrame = frameNumber,
                ValidFrames = 0,
                EmptyFrames = 0
            };
            report.CorruptFrames.Add(frameNumber);
            return report;
        }
    }
}