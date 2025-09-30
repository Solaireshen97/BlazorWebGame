using System;
using System.Threading.Tasks;
using BlazorIdleGame.Client.Services.Core;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Battle;
using Microsoft.Extensions.Logging;
using BattleStateDto = BlazorWebGame.Shared.DTOs.Battle.BattleStateDto;
using CombatEventDto = BlazorWebGame.Shared.DTOs.Battle.CombatEventDto;

namespace BlazorIdleGame.Client.Services.Battle
{
    public interface IBattleService
    {
        BattleStateDto? CurrentBattle { get; }
        bool IsInBattle { get; }

        event EventHandler<BattleStateDto>? BattleUpdated;
        event EventHandler<BattleResultDto>? BattleEnded;
        event EventHandler<CombatEventDto>? CombatEventReceived;

        Task<BattleStateDto?> GetCurrentBattleAsync();
        Task<bool> UseSkillAsync(string skillId, string? targetId = null);
        Task<bool> SetAutoModeAsync(bool enabled);
        Task<bool> FleeAsync();
    }

    public class BattleService : IBattleService
    {
        private readonly IGameCommunicationService _communication;
        private readonly ILogger<BattleService> _logger;
        private BattleStateDto? _currentBattle;

        public BattleStateDto? CurrentBattle => _currentBattle;
        public bool IsInBattle => _currentBattle != null && _currentBattle.Status == "InProgress";

        public event EventHandler<BattleStateDto>? BattleUpdated;
        public event EventHandler<BattleResultDto>? BattleEnded;
        public event EventHandler<CombatEventDto>? CombatEventReceived;

        public BattleService(
            IGameCommunicationService communication,
            ILogger<BattleService> logger)
        {
            _communication = communication;
            _logger = logger;
        }

        public async Task<BattleStateDto?> GetCurrentBattleAsync()
        {
            try
            {
                var response = await _communication.GetAsync<ApiResponse<BattleStateDto>>(
                    "api/battle/current");

                if (response?.Success == true && response.Data != null)
                {
                    var previousBattle = _currentBattle;
                    _currentBattle = response.Data;

                    // 检查战斗是否结束
                    if (previousBattle?.Status == "InProgress" &&
                        response.Data.Status != "InProgress")
                    {
                        // 获取战斗结果
                        await GetBattleResultAsync(response.Data.BattleId);
                    }

                    BattleUpdated?.Invoke(this, response.Data);

                    // 处理新的战斗事件
                    if (response.Data.LatestSegment != null)
                    {
                        foreach (var evt in response.Data.LatestSegment.Events)
                        {
                            CombatEventReceived?.Invoke(this, evt);
                        }
                    }

                    return response.Data;
                }

                _currentBattle = null;
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前战斗状态失败");
                return null;
            }
        }

        public async Task<bool> UseSkillAsync(string skillId, string? targetId = null)
        {
            if (!IsInBattle)
            {
                _logger.LogWarning("不在战斗中，无法使用技能");
                return false;
            }

            try
            {
                var request = new UseSkillRequest
                {
                    SkillId = skillId,
                    TargetId = targetId
                };

                var response = await _communication.PostAsync<UseSkillRequest, ApiResponse<bool>>(
                    "api/battle/skill", request);

                if (response?.Success == true)
                {
                    _logger.LogInformation("技能使用成功: {SkillId}", skillId);

                    // 立即更新战斗状态
                    await GetCurrentBattleAsync();
                    return true;
                }

                _logger.LogWarning("技能使用失败: {Message}", response?.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使用技能失败");
                return false;
            }
        }

        public async Task<bool> SetAutoModeAsync(bool enabled)
        {
            try
            {
                var request = new { Enabled = enabled };

                var response = await _communication.PostAsync<object, ApiResponse<bool>>(
                    "api/battle/auto", request);

                if (response?.Success == true)
                {
                    _logger.LogInformation("自动战斗模式: {Enabled}", enabled);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置自动战斗模式失败");
                return false;
            }
        }

        public async Task<bool> FleeAsync()
        {
            if (!IsInBattle)
                return false;

            try
            {
                var response = await _communication.PostAsync<object, ApiResponse<bool>>(
                    "api/battle/flee", new { });

                if (response?.Success == true)
                {
                    _logger.LogInformation("成功逃离战斗");
                    _currentBattle = null;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "逃离战斗失败");
                return false;
            }
        }

        private async Task GetBattleResultAsync(Guid battleId)
        {
            try
            {
                var response = await _communication.GetAsync<ApiResponse<BattleResultDto>>(
                    $"api/battle/{battleId}/result");

                if (response?.Success == true && response.Data != null)
                {
                    BattleEnded?.Invoke(this, response.Data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取战斗结果失败");
            }
        }

        /// <summary>
        /// 使用技能请求
        /// </summary>
        private class UseSkillRequest
        {
            public string SkillId { get; set; } = string.Empty;
            public string? TargetId { get; set; }
        }
    }

    /// <summary>
    /// 战斗结果DTO
    /// </summary>
    public class BattleResultDto
    {
        public Guid BattleId { get; set; }
        public bool Victory { get; set; }
        public int ExperienceGained { get; set; }
        public int GoldGained { get; set; }
        public List<LootItemDto> ItemsLooted { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Statistics { get; set; } = new();
    }

    /// <summary>
    /// 掉落物品DTO
    /// </summary>
    public class LootItemDto
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Rarity { get; set; } = string.Empty;
    }
}