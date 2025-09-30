using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.DTOs.Activity
{
    /// <summary>
    /// 活动计划DTO
    /// </summary>
    public class ActivityPlanDto
    {
        public string Id { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;
        public string ActivityName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string State { get; set; } = string.Empty;
        public int? RepeatCount { get; set; }
        public TimeSpan? Duration { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public ProgressInfo? Progress { get; set; }
    }

    /// <summary>
    /// 进度信息
    /// </summary>
    public class ProgressInfo
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
        public TimeSpan? EstimatedTimeRemaining { get; set; }
    }

    /// <summary>
    /// 活动槽位DTO
    /// </summary>
    public class ActivitySlotDto
    {
        public int Index { get; set; }
        public bool IsUnlocked { get; set; }
        public ActivityPlanDto? CurrentPlan { get; set; }
        public List<ActivityPlanDto> Queue { get; set; } = new();
        public int MaxQueueSize { get; set; } = 5;
    }

    /// <summary>
    /// 活动系统状态DTO
    /// </summary>
    public class ActivitySystemDto
    {
        public string CharacterId { get; set; } = string.Empty;
        public List<ActivitySlotDto> Slots { get; set; } = new();
        public Dictionary<string, int> ActiveCounts { get; set; } = new();
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// 创建活动计划请求
    /// </summary>
    public class CreateActivityPlanRequest
    {
        public string ActivityId { get; set; } = string.Empty;
        public int SlotIndex { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public int? RepeatCount { get; set; }
        public bool QueueIfBusy { get; set; } = true;
    }

    /// <summary>
    /// 取消活动请求
    /// </summary>
    public class CancelActivityRequest
    {
        public string PlanId { get; set; } = string.Empty;
        public bool ClearQueue { get; set; } = false;
    }
}