using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorIdleGame.Client.Services.Core;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Skill;
using Microsoft.Extensions.Logging;
using LearnedSkillDto = BlazorWebGame.Shared.DTOs.Skill.LearnedSkillDto;

namespace BlazorIdleGame.Client.Services.Skill
{
    public interface ISkillService
    {
        // 技能数据
        List<SkillDefinitionDto> AvailableSkills { get; }
        Dictionary<string, LearnedSkillDto> LearnedSkills { get; }
        SkillBarDto? CurrentSkillBar { get; }
        List<SkillTreeDto> SkillTrees { get; }

        // 冷却状态
        Dictionary<string, SkillCooldownDto> Cooldowns { get; }

        // 事件
        event EventHandler<LearnedSkillDto>? SkillLearned;
        event EventHandler<LearnedSkillDto>? SkillUpgraded;
        event EventHandler<SkillBarDto>? SkillBarUpdated;
        event EventHandler<SkillUseResult>? SkillUsed;
        event EventHandler<Dictionary<string, SkillCooldownDto>>? CooldownsUpdated;

        // 技能操作
        Task<bool> LoadSkillDataAsync();
        Task<LearnedSkillDto?> LearnSkillAsync(string skillId);
        Task<LearnedSkillDto?> UpgradeSkillAsync(string skillId);
        Task<bool> EquipSkillAsync(string skillId, int slotIndex, bool isPassive = false);
        Task<bool> UnequipSkillAsync(int slotIndex, bool isPassive = false);
        Task<SkillUseResult?> UseSkillAsync(string skillId, string? targetId = null);

        // 技能栏配置
        Task<bool> ConfigureSkillBarAsync(ConfigureSkillBarRequest request);
        Task<bool> SetAutocastAsync(int slotIndex, AutoCastSettingDto setting);

        // 技能树
        Task<List<SkillTreeDto>> GetSkillTreesAsync();
        Task<bool> InvestSkillPointAsync(string treeId, string nodeId);
        Task<bool> ResetSkillPointsAsync(string? treeId = null);

        // 辅助方法
        bool CanLearnSkill(string skillId);
        bool CanUpgradeSkill(string skillId);
        bool IsSkillOnCooldown(string skillId);
        TimeSpan GetRemainingCooldown(string skillId);
    }

    public class SkillService : ISkillService
    {
        private readonly IGameCommunicationService _communication;
        private readonly ILogger<SkillService> _logger;

        private List<SkillDefinitionDto> _availableSkills = new();
        private Dictionary<string, LearnedSkillDto> _learnedSkills = new();
        private SkillBarDto? _currentSkillBar;
        private List<SkillTreeDto> _skillTrees = new();
        private Dictionary<string, SkillCooldownDto> _cooldowns = new();
        private System.Threading.Timer? _cooldownTimer;

        public List<SkillDefinitionDto> AvailableSkills => _availableSkills;
        public Dictionary<string, LearnedSkillDto> LearnedSkills => _learnedSkills;
        public SkillBarDto? CurrentSkillBar => _currentSkillBar;
        public List<SkillTreeDto> SkillTrees => _skillTrees;
        public Dictionary<string, SkillCooldownDto> Cooldowns => _cooldowns;

        public event EventHandler<LearnedSkillDto>? SkillLearned;
        public event EventHandler<LearnedSkillDto>? SkillUpgraded;
        public event EventHandler<SkillBarDto>? SkillBarUpdated;
        public event EventHandler<SkillUseResult>? SkillUsed;
        public event EventHandler<Dictionary<string, SkillCooldownDto>>? CooldownsUpdated;

        public SkillService(
            IGameCommunicationService communication,
            ILogger<SkillService> logger)
        {
            _communication = communication;
            _logger = logger;

            // 启动冷却更新定时器
            _cooldownTimer = new System.Threading.Timer(
                _ => UpdateCooldowns(),
                null,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(100)
            );
        }

        public async Task<bool> LoadSkillDataAsync()
        {
            try
            {
                // 加载可用技能
                var skillsResponse = await _communication.GetAsync<ApiResponse<List<SkillDefinitionDto>>>(
                    "api/skill/available");

                if (skillsResponse?.IsSuccess == true && skillsResponse.Data != null)
                {
                    _availableSkills = skillsResponse.Data;
                }

                // 加载已学技能
                var learnedResponse = await _communication.GetAsync<ApiResponse<List<LearnedSkillDto>>>(
                    "api/skill/learned");

                if (learnedResponse?.IsSuccess == true && learnedResponse.Data != null)
                {
                    _learnedSkills = learnedResponse.Data.ToDictionary(s => s.SkillId);
                }

                // 加载技能栏配置
                var barResponse = await _communication.GetAsync<ApiResponse<SkillBarDto>>(
                    "api/skill/bar");

                if (barResponse?.IsSuccess == true && barResponse.Data != null)
                {
                    _currentSkillBar = barResponse.Data;
                    SkillBarUpdated?.Invoke(this, barResponse.Data);
                }

                // 加载冷却状态
                var cooldownResponse = await _communication.GetAsync<ApiResponse<List<SkillCooldownDto>>>(
                    "api/skill/cooldowns");

                if (cooldownResponse?.IsSuccess == true && cooldownResponse.Data != null)
                {
                    _cooldowns = cooldownResponse.Data.ToDictionary(c => c.SkillId);
                }

                _logger.LogInformation("技能数据加载完成: {AvailableCount} 可用, {LearnedCount} 已学",
                    _availableSkills.Count, _learnedSkills.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载技能数据失败");
                return false;
            }
        }

        public async Task<LearnedSkillDto?> LearnSkillAsync(string skillId)
        {
            try
            {
                var request = new LearnSkillRequest { SkillId = skillId };
                var response = await _communication.PostAsync<LearnSkillRequest, ApiResponse<LearnedSkillDto>>(
                    "api/skill/learn", request);

                if (response?.IsSuccess == true && response.Data != null)
                {
                    _learnedSkills[skillId] = response.Data;
                    SkillLearned?.Invoke(this, response.Data);

                    _logger.LogInformation("成功学习技能: {SkillName}", response.Data.Name);
                    return response.Data;
                }

                _logger.LogWarning("学习技能失败: {Message}", response?.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "学习技能失败");
                return null;
            }
        }

        public async Task<LearnedSkillDto?> UpgradeSkillAsync(string skillId)
        {
            try
            {
                if (!_learnedSkills.ContainsKey(skillId))
                {
                    _logger.LogWarning("尝试升级未学习的技能: {SkillId}", skillId);
                    return null;
                }

                var currentSkill = _learnedSkills[skillId];
                var request = new UpgradeSkillRequest
                {
                    SkillId = skillId,
                    TargetLevel = currentSkill.CurrentLevel + 1
                };

                var response = await _communication.PostAsync<UpgradeSkillRequest, ApiResponse<LearnedSkillDto>>(
                    "api/skill/upgrade", request);

                if (response?.IsSuccess == true && response.Data != null)
                {
                    _learnedSkills[skillId] = response.Data;
                    SkillUpgraded?.Invoke(this, response.Data);

                    _logger.LogInformation("技能升级成功: {SkillName} -> Lv.{Level}",
                        response.Data.Name, response.Data.CurrentLevel);
                    return response.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "升级技能失败");
                return null;
            }
        }

        public async Task<bool> EquipSkillAsync(string skillId, int slotIndex, bool isPassive = false)
        {
            try
            {
                if (!_learnedSkills.ContainsKey(skillId))
                {
                    _logger.LogWarning("尝试装备未学习的技能");
                    return false;
                }

                var request = new EquipSkillRequest
                {
                    SkillId = skillId,
                    SlotIndex = slotIndex,
                    IsPassive = isPassive
                };

                var response = await _communication.PostAsync<EquipSkillRequest, ApiResponse<SkillBarDto>>(
                    "api/skill/equip", request);

                if (response?.IsSuccess == true && response.Data != null)
                {
                    _currentSkillBar = response.Data;
                    SkillBarUpdated?.Invoke(this, response.Data);

                    _logger.LogInformation("技能已装备到槽位 {Slot}", slotIndex);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "装备技能失败");
                return false;
            }
        }

        public async Task<bool> UnequipSkillAsync(int slotIndex, bool isPassive = false)
        {
            try
            {
                var request = new { SlotIndex = slotIndex, IsPassive = isPassive };

                var response = await _communication.PostAsync<object, ApiResponse<SkillBarDto>>(
                    "api/skill/unequip", request);

                if (response?.IsSuccess == true && response.Data != null)
                {
                    _currentSkillBar = response.Data;
                    SkillBarUpdated?.Invoke(this, response.Data);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "卸下技能失败");
                return false;
            }
        }

        public async Task<SkillUseResult?> UseSkillAsync(string skillId, string? targetId = null)
        {
            try
            {
                // 检查冷却
                if (IsSkillOnCooldown(skillId))
                {
                    return new SkillUseResult
                    {
                        Success = false,
                        FailureReason = "技能冷却中"
                    };
                }

                var request = new UseSkillRequest
                {
                    SkillId = skillId,
                    TargetId = targetId
                };

                var response = await _communication.PostAsync<UseSkillRequest, ApiResponse<SkillUseResult>>(
                    "api/skill/use", request);

                if (response?.IsSuccess == true && response.Data != null)
                {
                    // 更新冷却
                    if (response.Data.Cooldown != null)
                    {
                        _cooldowns[skillId] = response.Data.Cooldown;
                    }

                    SkillUsed?.Invoke(this, response.Data);
                    return response.Data;
                }

                return new SkillUseResult
                {
                    Success = false,
                    FailureReason = response?.Message ?? "使用技能失败"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使用技能失败");
                return new SkillUseResult
                {
                    Success = false,
                    FailureReason = ex.Message
                };
            }
        }

        public async Task<bool> ConfigureSkillBarAsync(ConfigureSkillBarRequest request)
        {
            try
            {
                var response = await _communication.PostAsync<ConfigureSkillBarRequest, ApiResponse<SkillBarDto>>(
                    "api/skill/bar/configure", request);

                if (response?.IsSuccess == true && response.Data != null)
                {
                    _currentSkillBar = response.Data;
                    SkillBarUpdated?.Invoke(this, response.Data);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "配置技能栏失败");
                return false;
            }
        }

        public async Task<bool> SetAutocastAsync(int slotIndex, AutoCastSettingDto setting)
        {
            try
            {
                var request = new { SlotIndex = slotIndex, Setting = setting };

                var response = await _communication.PostAsync<object, ApiResponse<bool>>(
                    "api/skill/autocast", request);

                return response?.IsSuccess == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置自动施放失败");
                return false;
            }
        }

        public async Task<List<SkillTreeDto>> GetSkillTreesAsync()
        {
            try
            {
                var response = await _communication.GetAsync<ApiResponse<List<SkillTreeDto>>>(
                    "api/skill/trees");

                if (response?.IsSuccess == true && response.Data != null)
                {
                    _skillTrees = response.Data;
                    return response.Data;
                }

                return new List<SkillTreeDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取技能树失败");
                return new List<SkillTreeDto>();
            }
        }

        public async Task<bool> InvestSkillPointAsync(string treeId, string nodeId)
        {
            try
            {
                var request = new { TreeId = treeId, NodeId = nodeId };

                var response = await _communication.PostAsync<object, ApiResponse<SkillTreeDto>>(
                    "api/skill/tree/invest", request);

                if (response?.IsSuccess == true && response.Data != null)
                {
                    // 更新对应的技能树
                    var index = _skillTrees.FindIndex(t => t.TreeId == treeId);
                    if (index >= 0)
                    {
                        _skillTrees[index] = response.Data;
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "投入技能点失败");
                return false;
            }
        }

        public async Task<bool> ResetSkillPointsAsync(string? treeId = null)
        {
            try
            {
                var request = new ResetSkillPointsRequest { TreeId = treeId };

                var response = await _communication.PostAsync<ResetSkillPointsRequest, ApiResponse<List<SkillTreeDto>>>(
                    "api/skill/tree/reset", request);

                if (response?.IsSuccess == true && response.Data != null)
                {
                    _skillTrees = response.Data;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置技能点失败");
                return false;
            }
        }

        public bool CanLearnSkill(string skillId)
        {
            if (_learnedSkills.ContainsKey(skillId))
                return false;

            var skillDef = _availableSkills.FirstOrDefault(s => s.Id == skillId);
            if (skillDef == null)
                return false;

            // TODO: 检查等级、职业、前置技能等要求
            return true;
        }

        public bool CanUpgradeSkill(string skillId)
        {
            if (!_learnedSkills.TryGetValue(skillId, out var learned))
                return false;

            return !learned.IsMaxLevel;
        }

        public bool IsSkillOnCooldown(string skillId)
        {
            return _cooldowns.TryGetValue(skillId, out var cooldown) && !cooldown.IsReady;
        }

        public TimeSpan GetRemainingCooldown(string skillId)
        {
            if (_cooldowns.TryGetValue(skillId, out var cooldown))
            {
                return cooldown.RemainingCooldown;
            }
            return TimeSpan.Zero;
        }

        private void UpdateCooldowns()
        {
            var hasChanges = false;
            var toRemove = new List<string>();

            foreach (var kvp in _cooldowns)
            {
                if (kvp.Value.IsReady)
                {
                    toRemove.Add(kvp.Key);
                    hasChanges = true;
                }
            }

            foreach (var key in toRemove)
            {
                _cooldowns.Remove(key);
            }

            if (hasChanges)
            {
                CooldownsUpdated?.Invoke(this, _cooldowns);
            }
        }

        public void Dispose()
        {
            _cooldownTimer?.Dispose();
        }
    }
}