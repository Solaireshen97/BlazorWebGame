using System;

namespace BlazorWebGame.Shared.Models.Time
{
    /// <summary>
    /// 游戏时间快照
    /// </summary>
    public class GameTimeSnapshot
    {
        public DateTime ServerTime { get; set; }
        public DateTime GameTime { get; set; }
        public double TimeScale { get; set; } = 1.0;
        public long Tick { get; set; }
        public bool IsPaused { get; set; }
    }

    /// <summary>
    /// 时间跳跃请求
    /// </summary>
    public class TimeJumpRequest
    {
        public TimeSpan Duration { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 时间跳跃结果
    /// </summary>
    public class TimeJumpResult
    {
        public bool Success { get; set; }
        public TimeSpan ActualDuration { get; set; }
        public DateTime NewGameTime { get; set; }
        public string? Error { get; set; }
    }
}