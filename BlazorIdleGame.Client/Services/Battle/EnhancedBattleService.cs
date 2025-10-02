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
    /// ��ǿ��ս������ - �ṩս��ϵͳ�Ŀͻ��˹���
    /// </summary>
    public class EnhancedBattleService : IBattleService
    {
        private readonly IGameCommunicationService _communication;
        private readonly ILogger<EnhancedBattleService> _logger;
        
        // ս��״̬����
        private BattleDto? _currentBattle;
        private BattleStatusDto? _currentBattleStatus;
        private DateTime _lastStatusRefreshTime = DateTime.MinValue;
        
        // �Զ�ˢ�����
        private Timer? _statusRefreshTimer;
        private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(2);
        private bool _autoRefreshEnabled = false;
        
        // �¼�
        public event EventHandler<BattleDto>? BattleCreated;
        public event EventHandler<BattleDto>? BattleStarted;
        public event EventHandler<BattleStatusDto>? BattleStatusUpdated;
        public event EventHandler<BattleActionResultDto>? ActionPerformed;
        public event EventHandler<string>? BattleError;
        public event EventHandler<BattleStatusDto>? BattleEnded;
        
        // ����
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
        /// ����ս��
        /// </summary>
        /// <summary>
        /// ����ս��
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
                    _logger.LogInformation("ս�������ɹ�: {BattleId}", response.Data);

                    // ����ս��ʵ�壬������ȡս��״̬
                    var battleId = response.Data;
                    var battle = new BattleDto
                    {
                        Id = battleId,
                        BattleType = battleType ?? "Normal",
                        Status = "Created",
                        CreatedAt = DateTime.Now
                    };

                    // ֻ����ս����Ϣ��������ս��״̬
                    _currentBattle = battle;
                    // ����ս��״̬��ȷ����Ҫ�����ʼս��
                    _currentBattleStatus = null;

                    // �����¼�
                    BattleCreated?.Invoke(this, battle);

                    return battle;
                }
                else
                {
                    _logger.LogWarning("����ս��ʧ��: {Message}", response?.Message);
                    BattleError?.Invoke(this, response?.Message ?? "����ս��ʧ��");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "����ս��ʱ�����쳣");
                BattleError?.Invoke(this, $"����ս���쳣: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ��ʼս��
        /// </summary>
        /// <summary>
        /// ��ʼս��
        /// </summary>
        public async Task<bool> StartBattleAsync(string battleId)
        {
            try
            {
                var response = await _communication.PostAsync<object, ApiResponse<bool>>(
                    $"api/battle/{battleId}/start", new { });

                if (response?.IsSuccess == true && response.Data)
                {
                    _logger.LogInformation("ս����ʼ: {BattleId}", battleId);

                    // ����ս��״̬
                    if (_currentBattle != null && _currentBattle.Id == battleId)
                    {
                        _currentBattle.Status = "InProgress";
                        BattleStarted?.Invoke(this, _currentBattle);

                        // ��ȡս��״̬
                        _currentBattleStatus = await GetBattleStatusAsync(battleId);

                        // ��ʼ�Զ�ˢ��ս��״̬
                        StartAutoRefresh();
                    }

                    return true;
                }
                else
                {
                    _logger.LogWarning("��ʼս��ʧ��: {Message}", response?.Message);
                    BattleError?.Invoke(this, response?.Message ?? "��ʼս��ʧ��");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ʼս��ʱ�����쳣");
                BattleError?.Invoke(this, $"��ʼս���쳣: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ʹ�ü���
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
                    _logger.LogInformation("����ʹ�óɹ�: {SkillId}", skillId);
                    
                    // �����¼�
                    ActionPerformed?.Invoke(this, response.Data);
                    
                    // ˢ��ս��״̬
                    await RefreshBattleStatusAsync();
                    
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("ʹ�ü���ʧ��: {Message}", response?.Message);
                    BattleError?.Invoke(this, response?.Message ?? "ʹ�ü���ʧ��");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ʹ�ü���ʱ�����쳣");
                BattleError?.Invoke(this, $"ʹ�ü����쳣: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ��ȡս��״̬
        /// </summary>
        /// <summary>
        /// ��ȡս��״̬
        /// </summary>
        public async Task<BattleStatusDto?> GetBattleStatusAsync(string battleId)
        {
            try
            {
                // ����Ƶ��ˢ�£�����ϴ�ˢ����1���ڣ�ֱ�ӷ��ػ����״̬
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
                        // ��response.Dataǿ��ת��ΪJsonElement����
                        if (response.Data is System.Text.Json.JsonElement element)
                        {
                            _logger.LogInformation("ս��״̬ԭʼ����: {Data}",
                                System.Text.Json.JsonSerializer.Serialize(element));

                            // ֱ�Ӵ�JsonElement����ȡ����
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

                            // ����ս��������
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

                            // ���ս���Ƿ��ѽ���
                            bool wasEnded = battleStatus.IsEnded &&
                                          (_currentBattleStatus == null || !_currentBattleStatus.IsEnded);

                            // ���»���
                            _currentBattleStatus = battleStatus;

                            // ����״̬�����¼�
                            BattleStatusUpdated?.Invoke(this, battleStatus);

                            // ���ս���ѽ���������ս�������¼�
                            if (wasEnded)
                            {
                                BattleEnded?.Invoke(this, battleStatus);
                                StopAutoRefresh();
                            }

                            return battleStatus;
                        }
                        else
                        {
                            _logger.LogWarning("ս��״̬���ݲ���Ԥ�ڵ�JsonElement����");
                            BattleError?.Invoke(this, "�޷�����ս��״̬: ���ݸ�ʽ����ȷ");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "����ս��״̬����ʧ��");
                        BattleError?.Invoke(this, $"����ս��״̬ʧ��: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning("��ȡս��״̬ʧ��: {Message}", response?.Message);
                    BattleError?.Invoke(this, response?.Message ?? "��ȡս��״̬ʧ��");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡս��״̬ʱ�����쳣");
                BattleError?.Invoke(this, $"��ȡս��״̬�쳣: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ˢ�µ�ǰս��״̬
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
        /// ���õ�ǰս��
        /// </summary>
        public void ResetCurrentBattle()
        {
            _currentBattle = null;
            _currentBattleStatus = null;
            StopAutoRefresh();
            _logger.LogInformation("��ǰս��״̬������");
        }
        
        /// <summary>
        /// ��ʼ�Զ�ˢ��ս��״̬
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
                
                _logger.LogDebug("������ս��״̬�Զ�ˢ��");
            }
        }
        
        /// <summary>
        /// ֹͣ�Զ�ˢ��ս��״̬
        /// </summary>
        private void StopAutoRefresh()
        {
            if (_autoRefreshEnabled)
            {
                _statusRefreshTimer?.Dispose();
                _statusRefreshTimer = null;
                _autoRefreshEnabled = false;
                _logger.LogDebug("��ֹͣս��״̬�Զ�ˢ��");
            }
        }

        /// <summary>
        /// ƥ���˷��ص�ս��״̬DTO
        /// </summary>
        public class BattleResponseDto
        {
            public string BattleId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public int Turn { get; set; }
            public List<ParticipantDto> Participants { get; set; } = new List<ParticipantDto>();
        }

        /// <summary>
        /// ս��������DTO
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