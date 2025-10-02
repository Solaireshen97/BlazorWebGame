using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlazorIdleGame.Client.Services.Core;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.DTOs.Battles;
using Microsoft.Extensions.Logging;
using ActiveBuffDto = BlazorWebGame.Shared.DTOs.Battles.ActiveBuffDto;

namespace BlazorIdleGame.Client.Services.Battle
{
    /// <summary>
    /// 增强版战斗服务 - 提供战斗系统的客户端功能
    /// </summary>
    public class EnhancedBattleService : IBattleService
    {
        private readonly IGameCommunicationService _communication;
        private readonly ILogger<EnhancedBattleService> _logger;
        
        // 战斗状态缓存
        private BattleDto? _currentBattle;
        private BattleStatusDto? _currentBattleStatus;
        private DateTime _lastStatusRefreshTime = DateTime.MinValue;
        
        // 自动刷新相关
        private Timer? _statusRefreshTimer;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(2);
        private bool _autoRefreshEnabled = false;
        
        // 事件
        public event EventHandler<BattleDto>? BattleCreated;
        public event EventHandler<BattleDto>? BattleStarted;
        public event EventHandler<BattleStatusDto>? BattleStatusUpdated;
        public event EventHandler<BattleActionResultDto>? ActionPerformed;
        public event EventHandler<string>? BattleError;
        public event EventHandler<BattleStatusDto>? BattleEnded;
        
        // 属性
        public BattleDto? CurrentBattle => _currentBattle;
        public BattleStatusDto? CurrentBattleStatus => _currentBattleStatus;
        
        public EnhancedBattleService(
            IGameCommunicationService communication,
            ILogger<EnhancedBattleService> logger)
        {
            _communication = communication;
            _logger = logger;
        }

        /// <summary>
        /// 创建战斗
        /// </summary>
        /// <summary>
        /// 创建战斗
        /// </summary>
        public async Task<BattleDto?> CreateBattleAsync(
            string characterId,
            string enemyId,
            string? battleType = null,
            string? regionId = null)
        {
            try
            {
                var request = new CreateBattleRequest
                {
                    CharacterId = characterId,
                    EnemyId = enemyId,
                    BattleType = battleType,
                    RegionId = regionId
                };

                var response = await _communication.PostAsync<CreateBattleRequest, ApiResponse<string>>(
                    "api/battle/create", request);

                if (response?.IsSuccess == true && !string.IsNullOrEmpty(response.Data))
                {
                    _logger.LogInformation("战斗创建成功: {BattleId}", response.Data);

                    // 创建战斗实体，但不获取战斗状态
                    var battleId = response.Data;
                    var battle = new BattleDto
                    {
                        Id = battleId,
                        BattleType = battleType ?? "Normal",
                        Status = "Created",
                        CreatedAt = DateTime.Now
                    };

                    // 只缓存战斗信息，不设置战斗状态
                    _currentBattle = battle;
                    // 重置战斗状态，确保需要点击开始战斗
                    _currentBattleStatus = null;

                    // 触发事件
                    BattleCreated?.Invoke(this, battle);

                    return battle;
                }
                else
                {
                    _logger.LogWarning("创建战斗失败: {Message}", response?.Message);
                    BattleError?.Invoke(this, response?.Message ?? "创建战斗失败");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建战斗时发生异常");
                BattleError?.Invoke(this, $"创建战斗异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        /// <summary>
        /// 开始战斗
        /// </summary>
        public async Task<bool> StartBattleAsync(string battleId)
        {
            try
            {
                var response = await _communication.PostAsync<object, ApiResponse<bool>>(
                    $"api/battle/{battleId}/start", new { });

                if (response?.IsSuccess == true && response.Data)
                {
                    _logger.LogInformation("战斗开始: {BattleId}", battleId);

                    // 更新战斗状态
                    if (_currentBattle != null && _currentBattle.Id == battleId)
                    {
                        _currentBattle.Status = "InProgress";
                        BattleStarted?.Invoke(this, _currentBattle);

                        // 获取战斗状态
                        _currentBattleStatus = await GetBattleStatusAsync(battleId);

                        // 开始自动刷新战斗状态
                        StartAutoRefresh();
                    }

                    return true;
                }
                else
                {
                    _logger.LogWarning("开始战斗失败: {Message}", response?.Message);
                    BattleError?.Invoke(this, response?.Message ?? "开始战斗失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始战斗时发生异常");
                BattleError?.Invoke(this, $"开始战斗异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用技能
        /// </summary>
        public async Task<BattleActionResultDto?> UseSkillAsync(
            string battleId, 
            string casterId, 
            string skillId, 
            string? targetId = null)
        {
            try
            {
                var request = new UseSkillRequest
                {
                    CasterId = casterId,
                    SkillId = skillId,
                    TargetId = targetId
                };
                
                var response = await _communication.PostAsync<UseSkillRequest, ApiResponse<BattleActionResultDto>>(
                    $"api/battle/{battleId}/skill", request);
                
                if (response?.IsSuccess == true && response.Data != null)
                {
                    _logger.LogInformation("技能使用成功: {SkillId}", skillId);
                    
                    // 触发事件
                    ActionPerformed?.Invoke(this, response.Data);
                    
                    // 刷新战斗状态
                    await RefreshBattleStatusAsync();
                    
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("使用技能失败: {Message}", response?.Message);
                    BattleError?.Invoke(this, response?.Message ?? "使用技能失败");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使用技能时发生异常");
                BattleError?.Invoke(this, $"使用技能异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取战斗状态
        /// </summary>
        /// <summary>
        /// 获取战斗状态
        /// </summary>
        public async Task<BattleStatusDto?> GetBattleStatusAsync(string battleId)
        {
            try
            {
                // 避免频繁刷新，如果上次刷新在1秒内，直接返回缓存的状态
                if (_currentBattleStatus != null &&
                    (DateTime.Now - _lastStatusRefreshTime).TotalSeconds < 1)
                {
                    return _currentBattleStatus;
                }

                var response = await _communication.GetAsync<ApiResponse<object>>(
                    $"api/battle/{battleId}/status");

                if (response?.IsSuccess == true && response.Data != null)
                {
                    try
                    {
                        // 将response.Data强制转换为JsonElement类型
                        if (response.Data is System.Text.Json.JsonElement element)
                        {
                            _logger.LogInformation("战斗状态原始数据: {Data}",
                                System.Text.Json.JsonSerializer.Serialize(element));

                            // 直接从JsonElement中提取数据
                            var battleStatus = new BattleStatusDto
                            {
                                CurrentRound = element.TryGetProperty("turn", out var turnElement) ?
                                    turnElement.GetInt32() : 0,
                                CurrentTurnEntityId = "",
                                State = element.TryGetProperty("status", out var statusElement) ?
                                    statusElement.GetString() ?? "Unknown" : "Unknown",
                                IsEnded = element.TryGetProperty("status", out var isEndedElement) &&
                                    (isEndedElement.GetString() == "Completed" ||
                                     isEndedElement.GetString() == "Victory" ||
                                     isEndedElement.GetString() == "Defeat"),
                                Entities = new List<BattleEntityStatusDto>(),
                                WinnerSide = element.TryGetProperty("status", out var winnerElement) ?
                                    (winnerElement.GetString() == "Victory" ? "Player" :
                                     winnerElement.GetString() == "Defeat" ? "Enemy" : null) : null
                            };

                            // 解析战斗参与者
                            if (element.TryGetProperty("participants", out var participantsElement) &&
                                participantsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var participant in participantsElement.EnumerateArray())
                                {
                                    var entityStatus = new BattleEntityStatusDto
                                    {
                                        Id = participant.TryGetProperty("id", out var idElement) ?
                                            idElement.GetString() ?? "" : "",
                                        Name = participant.TryGetProperty("name", out var nameElement) ?
                                            nameElement.GetString() ?? "" : "",
                                        CurrentHp = participant.TryGetProperty("health", out var healthElement) ?
                                            healthElement.GetInt32() : 0,
                                        MaxHp = participant.TryGetProperty("maxHealth", out var maxHealthElement) ?
                                            maxHealthElement.GetInt32() : 0,
                                        IsAlive = participant.TryGetProperty("isAlive", out var isAliveElement) &&
                                            isAliveElement.GetBoolean(),
                                        Side = participant.TryGetProperty("team", out var teamElement) ?
                                            (teamElement.GetInt32() == 0 ? "Player" : "Enemy") : "Unknown",
                                        Buffs = new List<ActiveBuffDto>()
                                    };

                                    battleStatus.Entities.Add(entityStatus);
                                }
                            }

                            _lastStatusRefreshTime = DateTime.Now;

                            // 检查战斗是否已结束
                            bool wasEnded = battleStatus.IsEnded &&
                                          (_currentBattleStatus == null || !_currentBattleStatus.IsEnded);

                            // 更新缓存
                            _currentBattleStatus = battleStatus;

                            // 触发状态更新事件
                            BattleStatusUpdated?.Invoke(this, battleStatus);

                            // 如果战斗已结束，触发战斗结束事件
                            if (wasEnded)
                            {
                                BattleEnded?.Invoke(this, battleStatus);
                                StopAutoRefresh();
                            }

                            return battleStatus;
                        }
                        else
                        {
                            _logger.LogWarning("战斗状态数据不是预期的JsonElement类型");
                            BattleError?.Invoke(this, "无法解析战斗状态: 数据格式不正确");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理战斗状态数据失败");
                        BattleError?.Invoke(this, $"解析战斗状态失败: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning("获取战斗状态失败: {Message}", response?.Message);
                    BattleError?.Invoke(this, response?.Message ?? "获取战斗状态失败");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取战斗状态时发生异常");
                BattleError?.Invoke(this, $"获取战斗状态异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 刷新当前战斗状态
        /// </summary>
        public async Task<BattleStatusDto?> RefreshBattleStatusAsync()
        {
            if (_currentBattle != null)
            {
                return await GetBattleStatusAsync(_currentBattle.Id);
            }
            return null;
        }
        
        /// <summary>
        /// 重置当前战斗
        /// </summary>
        public void ResetCurrentBattle()
        {
            _currentBattle = null;
            _currentBattleStatus = null;
            StopAutoRefresh();
            _logger.LogInformation("当前战斗状态已重置");
        }
        
        /// <summary>
        /// 开始自动刷新战斗状态
        /// </summary>
        private void StartAutoRefresh()
        {
            if (!_autoRefreshEnabled && _currentBattle != null)
            {
                _autoRefreshEnabled = true;
                _statusRefreshTimer = new Timer(
                    async _ => await RefreshBattleStatusAsync(),
                    null,
                    TimeSpan.Zero,
                    _refreshInterval);
                
                _logger.LogDebug("已启动战斗状态自动刷新");
            }
        }
        
        /// <summary>
        /// 停止自动刷新战斗状态
        /// </summary>
        private void StopAutoRefresh()
        {
            if (_autoRefreshEnabled)
            {
                _statusRefreshTimer?.Dispose();
                _statusRefreshTimer = null;
                _autoRefreshEnabled = false;
                _logger.LogDebug("已停止战斗状态自动刷新");
            }
        }

        /// <summary>
        /// 匹配后端返回的战斗状态DTO
        /// </summary>
        public class BattleResponseDto
        {
            public string BattleId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int Turn { get; set; }
            public List<ParticipantDto> Participants { get; set; } = new List<ParticipantDto>();
        }

        /// <summary>
        /// 战斗参与者DTO
        /// </summary>
        public class ParticipantDto
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Health { get; set; }
            public int MaxHealth { get; set; }
            public bool IsAlive { get; set; }
            public int Team { get; set; }
        }
    }
}