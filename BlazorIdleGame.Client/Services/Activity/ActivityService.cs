using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorIdleGame.Client.Services.Core;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Activity;
using Microsoft.Extensions.Logging;

namespace BlazorIdleGame.Client.Services.Activity
{
    public interface IActivityService
    {
        ActivitySystemDto? CurrentSystem { get; }
        event EventHandler<ActivitySystemDto>? SystemUpdated;
        event EventHandler<ActivityPlanDto>? ActivityCompleted;

        Task<ActivitySystemDto?> GetActivitySystemAsync();
        Task<bool> StartActivityAsync(CreateActivityPlanRequest request);
        Task<bool> CancelActivityAsync(string planId, bool clearQueue = false);
        Task<bool> MoveToSlotAsync(string planId, int targetSlot);
        Task<List<ActivityDefinitionDto>> GetAvailableActivitiesAsync();
    }

    public class ActivityService : IActivityService
    {
        private readonly IGameCommunicationService _communication;
        private readonly ILogger<ActivityService> _logger;
        private ActivitySystemDto? _currentSystem;

        public ActivitySystemDto? CurrentSystem => _currentSystem;

        public event EventHandler<ActivitySystemDto>? SystemUpdated;
        public event EventHandler<ActivityPlanDto>? ActivityCompleted;

        public ActivityService(
            IGameCommunicationService communication,
            ILogger<ActivityService> logger)
        {
            _communication = communication;
            _logger = logger;
        }

        public async Task<ActivitySystemDto?> GetActivitySystemAsync()
        {
            try
            {
                var response = await _communication.GetAsync<ApiResponse<ActivitySystemDto>>(
                    "api/activity/system");

                if (response?.Success == true && response.Data != null)
                {
                    _currentSystem = response.Data;
                    SystemUpdated?.Invoke(this, response.Data);

                    // 检查已完成的活动
                    CheckCompletedActivities(response.Data);

                    return response.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活动系统状态失败");
                return null;
            }
        }

        public async Task<bool> StartActivityAsync(CreateActivityPlanRequest request)
        {
            try
            {
                var response = await _communication.PostAsync<CreateActivityPlanRequest, ApiResponse<ActivityPlanDto>>(
                    "api/activity/start", request);

                if (response?.Success == true)
                {
                    _logger.LogInformation("活动已启动: {ActivityId} in Slot {Slot}",
                        request.ActivityId, request.SlotIndex);

                    // 刷新系统状态
                    await GetActivitySystemAsync();
                    return true;
                }

                _logger.LogWarning("启动活动失败: {Message}", response?.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动活动失败");
                return false;
            }
        }

        public async Task<bool> CancelActivityAsync(string planId, bool clearQueue = false)
        {
            try
            {
                var request = new CancelActivityRequest
                {
                    PlanId = planId,
                    ClearQueue = clearQueue
                };

                var response = await _communication.PostAsync<CancelActivityRequest, ApiResponse<bool>>(
                    "api/activity/cancel", request);

                if (response?.Success == true)
                {
                    _logger.LogInformation("活动已取消: {PlanId}", planId);
                    await GetActivitySystemAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消活动失败");
                return false;
            }
        }

        public async Task<bool> MoveToSlotAsync(string planId, int targetSlot)
        {
            try
            {
                var request = new { PlanId = planId, TargetSlot = targetSlot };

                var response = await _communication.PostAsync<object, ApiResponse<bool>>(
                    "api/activity/move", request);

                if (response?.Success == true)
                {
                    await GetActivitySystemAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移动活动失败");
                return false;
            }
        }

        public async Task<List<ActivityDefinitionDto>> GetAvailableActivitiesAsync()
        {
            try
            {
                var response = await _communication.GetAsync<ApiResponse<List<ActivityDefinitionDto>>>(
                    "api/activity/available");

                return response?.Data ?? new List<ActivityDefinitionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用活动列表失败");
                return new List<ActivityDefinitionDto>();
            }
        }

        private void CheckCompletedActivities(ActivitySystemDto system)
        {
            foreach (var slot in system.Slots)
            {
                if (slot.CurrentPlan?.State == "Completed")
                {
                    ActivityCompleted?.Invoke(this, slot.CurrentPlan);
                }
            }
        }
    }

    /// <summary>
    /// 活动定义DTO
    /// </summary>
    public class ActivityDefinitionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public TimeSpan BaseDuration { get; set; }
        public Dictionary<string, object> Requirements { get; set; } = new();
        public Dictionary<string, object> Rewards { get; set; } = new();
        public bool IsRepeatable { get; set; }
        public int? MaxRepeatCount { get; set; }
    }
}