using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Utils;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 离线模式服务 - 在服务器不可用时提供基本功能
/// </summary>
public class OfflineService
{
    private readonly ILogger<OfflineService> _logger;
    private readonly GameStorage _storage;
    private bool _isOfflineMode = false;
    private readonly Queue<OfflineAction> _pendingActions = new();

    public bool IsOfflineMode => _isOfflineMode;
    public event Action<bool>? OnOfflineModeChanged;

    public OfflineService(ILogger<OfflineService> logger, GameStorage storage)
    {
        _logger = logger;
        _storage = storage;
    }

    /// <summary>
    /// 进入离线模式
    /// </summary>
    public async Task EnterOfflineMode()
    {
        _isOfflineMode = true;
        _logger.LogWarning("进入离线模式");
        
        // 保存当前状态到本地
        await SaveCurrentStateLocally();
        
        OnOfflineModeChanged?.Invoke(true);
    }

    /// <summary>
    /// 退出离线模式并同步数据
    /// </summary>
    public async Task<bool> ExitOfflineMode(GameApiService apiService)
    {
        try
        {
            _logger.LogInformation("尝试退出离线模式并同步数据");
            
            // 执行所有待处理的操作
            while (_pendingActions.Count > 0)
            {
                var action = _pendingActions.Dequeue();
                await ExecutePendingAction(action, apiService);
            }
            
            _isOfflineMode = false;
            OnOfflineModeChanged?.Invoke(false);
            _logger.LogInformation("成功退出离线模式");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出离线模式失败");
            return false;
        }
    }

    /// <summary>
    /// 记录离线操作
    /// </summary>
    public void RecordOfflineAction(OfflineActionType actionType, object data)
    {
        var action = new OfflineAction
        {
            Type = actionType,
            Timestamp = DateTime.UtcNow,
            Data = System.Text.Json.JsonSerializer.Serialize(data)
        };
        
        _pendingActions.Enqueue(action);
        _logger.LogInformation("记录离线操作: {ActionType} at {Timestamp}", actionType, action.Timestamp);
    }

    /// <summary>
    /// 本地战斗模拟（离线模式）- 向后兼容的同步版本
    /// </summary>
    public BattleStateDto SimulateLocalBattle(string characterId, string enemyId, string? partyId = null)
    {
        // 使用Task.Run来同步等待异步方法，避免死锁
        return Task.Run(async () => await SimulateLocalBattleAsync(characterId, enemyId, partyId)).Result;
    }

    /// <summary>
    /// 本地战斗模拟（离线模式）- 异步版本
    /// </summary>
    public async Task<BattleStateDto> SimulateLocalBattleAsync(string characterId, string enemyId, string? partyId = null)
    {
        _logger.LogInformation("开始本地战斗模拟");
        
        // 从本地存储获取角色数据来计算更准确的战斗状态
        var characterData = await GetLocalCharacterDataAsync(characterId);
        var partyData = !string.IsNullOrEmpty(partyId) ? await GetLocalPartyDataAsync(partyId) : null;
        
        return new BattleStateDto
        {
            BattleId = Guid.NewGuid(),
            CharacterId = characterId,
            EnemyId = enemyId,
            PartyId = partyId,
            IsActive = true,
            PlayerHealth = characterData?.Health ?? 100,
            PlayerMaxHealth = characterData?.MaxHealth ?? 100,
            EnemyHealth = CalculateEnemyHealth(characterData?.Level ?? 1),
            EnemyMaxHealth = CalculateEnemyMaxHealth(characterData?.Level ?? 1),
            LastUpdated = DateTime.UtcNow,
            BattleType = !string.IsNullOrEmpty(partyId) ? BattleType.Normal : BattleType.Normal,
            Players = await CreateLocalPlayersListAsync(characterData, partyData),
            Enemies = CreateLocalEnemiesList(enemyId, characterData?.Level ?? 1),
            Status = BattleStatus.Active
        };
    }

    /// <summary>
    /// 增强的离线战斗进度推算
    /// </summary>
    public async Task<OfflineBattleProgressDto> CalculateOfflineBattleProgress(string characterId, TimeSpan offlineTime)
    {
        var characterData = await GetLocalCharacterDataAsync(characterId);
        if (characterData == null)
        {
            return new OfflineBattleProgressDto
            {
                CharacterId = characterId,
                OfflineTime = offlineTime,
                EstimatedBattles = 0,
                EstimatedExperience = 0,
                EstimatedGold = 0
            };
        }

        var progressCalculator = new LocalBattleProgressCalculator(characterData, offlineTime);
        return await progressCalculator.CalculateProgressAsync();
    }

    /// <summary>
    /// 多人组队离线进度同步
    /// </summary>
    public async Task<TeamOfflineProgressDto> SynchronizeTeamOfflineProgress(string partyId, List<string> memberIds)
    {
        var teamProgress = new TeamOfflineProgressDto
        {
            PartyId = partyId,
            MemberProgresses = new List<OfflineBattleProgressDto>(),
            SyncTime = DateTime.UtcNow
        };

        // 获取所有成员的离线进度
        foreach (var memberId in memberIds)
        {
            var memberData = await GetLocalCharacterDataAsync(memberId);
            if (memberData != null)
            {
                var offlineTime = DateTime.UtcNow - memberData.LastActiveTime;
                var memberProgress = await CalculateOfflineBattleProgress(memberId, offlineTime);
                teamProgress.MemberProgresses.Add(memberProgress);
            }
        }

        // 计算队伍协作加成
        teamProgress.TeamBonus = CalculateTeamCooperationBonus(teamProgress.MemberProgresses);
        teamProgress.SynchronizationLevel = CalculateTeamSynchronizationLevel(teamProgress.MemberProgresses);

        // 保存队伍进度到本地缓存
        await SaveTeamProgressLocally(teamProgress);

        return teamProgress;
    }

    /// <summary>
    /// 获取本地角色数据
    /// </summary>
    private async Task<LocalCharacterData?> GetLocalCharacterDataAsync(string characterId)
    {
        try
        {
            var dataKey = $"character_{characterId}";
            var jsonData = await _storage.GetItemAsync<string>(dataKey);
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                return System.Text.Json.JsonSerializer.Deserialize<LocalCharacterData>(jsonData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取本地角色数据失败: {CharacterId}", characterId);
        }
        
        return null;
    }

    /// <summary>
    /// 获取本地组队数据
    /// </summary>
    private async Task<LocalPartyData?> GetLocalPartyDataAsync(string partyId)
    {
        try
        {
            var dataKey = $"party_{partyId}";
            var jsonData = await _storage.GetItemAsync<string>(dataKey);
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                return System.Text.Json.JsonSerializer.Deserialize<LocalPartyData>(jsonData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取本地组队数据失败: {PartyId}", partyId);
        }
        
        return null;
    }

    /// <summary>
    /// 计算敌人血量
    /// </summary>
    private int CalculateEnemyHealth(int playerLevel)
    {
        return 80 + (playerLevel - 1) * 15; // 基础80血量，每级增加15
    }

    /// <summary>
    /// 计算敌人最大血量
    /// </summary>
    private int CalculateEnemyMaxHealth(int playerLevel)
    {
        return CalculateEnemyHealth(playerLevel);
    }

    /// <summary>
    /// 创建本地玩家列表
    /// </summary>
    private async Task<List<BattleParticipantDto>> CreateLocalPlayersListAsync(LocalCharacterData? characterData, LocalPartyData? partyData)
    {
        var players = new List<BattleParticipantDto>();
        
        if (characterData != null)
        {
            players.Add(new BattleParticipantDto
            {
                Id = characterData.Id,
                Name = characterData.Name,
                Health = characterData.Health,
                MaxHealth = characterData.MaxHealth,
                AttackPower = characterData.AttackPower,
                AttacksPerSecond = 1.2,
                IsPlayer = true
            });
        }

        // 如果有组队数据，添加其他成员
        if (partyData != null)
        {
            foreach (var memberId in partyData.MemberIds.Where(id => id != characterData?.Id))
            {
                var memberData = await GetLocalCharacterDataAsync(memberId);
                if (memberData != null)
                {
                    players.Add(new BattleParticipantDto
                    {
                        Id = memberData.Id,
                        Name = memberData.Name,
                        Health = memberData.Health,
                        MaxHealth = memberData.MaxHealth,
                        AttackPower = memberData.AttackPower,
                        AttacksPerSecond = 1.2,
                        IsPlayer = true
                    });
                }
            }
        }

        return players;
    }

    /// <summary>
    /// 创建本地敌人列表
    /// </summary>
    private List<BattleParticipantDto> CreateLocalEnemiesList(string enemyId, int playerLevel)
    {
        var enemyHealth = CalculateEnemyHealth(playerLevel);
        var enemyAttackPower = 12 + playerLevel * 2;
        
        return new List<BattleParticipantDto>
        {
            new BattleParticipantDto
            {
                Id = enemyId,
                Name = GetEnemyNameByLevel(playerLevel),
                Health = enemyHealth,
                MaxHealth = enemyHealth,
                AttackPower = enemyAttackPower,
                AttacksPerSecond = 1.0,
                IsPlayer = false
            }
        };
    }

    /// <summary>
    /// 根据等级获取敌人名称
    /// </summary>
    private string GetEnemyNameByLevel(int level)
    {
        return level switch
        {
            <= 5 => "哥布林",
            <= 10 => "兽人",
            <= 15 => "巨魔",
            <= 20 => "飞龙",
            _ => "远古巨龙"
        };
    }

    /// <summary>
    /// 计算队伍协作加成
    /// </summary>
    private double CalculateTeamCooperationBonus(List<OfflineBattleProgressDto> memberProgresses)
    {
        if (memberProgresses.Count <= 1) return 1.0;

        var baseBonus = 1.0 + (memberProgresses.Count - 1) * 0.1; // 每个额外成员10%加成
        var avgLevel = memberProgresses.Average(p => p.CharacterLevel);
        var levelBonus = Math.Min(0.5, avgLevel * 0.01); // 等级加成，最多50%

        return baseBonus + levelBonus;
    }

    /// <summary>
    /// 计算队伍同步水平
    /// </summary>
    private double CalculateTeamSynchronizationLevel(List<OfflineBattleProgressDto> memberProgresses)
    {
        if (memberProgresses.Count <= 1) return 1.0;

        var offlineTimes = memberProgresses.Select(p => p.OfflineTime.TotalHours).ToList();
        var avgOfflineTime = offlineTimes.Average();
        var variance = offlineTimes.Sum(t => Math.Pow(t - avgOfflineTime, 2)) / offlineTimes.Count;
        var standardDeviation = Math.Sqrt(variance);

        // 同步水平：标准差越小，同步水平越高
        var maxStdDev = 12.0; // 假设12小时差异为最大不同步
        return Math.Max(0, 1.0 - (standardDeviation / maxStdDev));
    }

    /// <summary>
    /// 保存队伍进度到本地
    /// </summary>
    private async Task SaveTeamProgressLocally(TeamOfflineProgressDto teamProgress)
    {
        try
        {
            var dataKey = $"team_progress_{teamProgress.PartyId}";
            var jsonData = System.Text.Json.JsonSerializer.Serialize(teamProgress);
            await _storage.SetItemAsync(dataKey, jsonData);
            
            _logger.LogInformation("队伍进度已保存到本地: {PartyId}", teamProgress.PartyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存队伍进度到本地失败: {PartyId}", teamProgress.PartyId);
        }
    }

    /// <summary>
    /// 增强的保存当前状态到本地存储
    /// </summary>
    private async Task SaveCurrentStateLocally()
    {
        try
        {
            // 保存离线模式状态
            var offlineState = new OfflineGameState
            {
                IsOfflineMode = _isOfflineMode,
                OfflineStartTime = DateTime.UtcNow,
                PendingActionsCount = _pendingActions.Count,
                Version = 1
            };

            var offlineStateJson = System.Text.Json.JsonSerializer.Serialize(offlineState);
            await _storage.SetItemAsync("offline_game_state", offlineStateJson);

            // 保存待处理操作
            var pendingActionsJson = System.Text.Json.JsonSerializer.Serialize(_pendingActions.ToList());
            await _storage.SetItemAsync("pending_offline_actions", pendingActionsJson);

            _logger.LogInformation("当前状态已保存到本地，待处理操作数量: {Count}", _pendingActions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存本地状态失败");
        }
    }

    /// <summary>
    /// 从本地存储恢复离线状态
    /// </summary>
    public async Task RestoreOfflineStateFromLocal()
    {
        try
        {
            // 恢复离线状态
            var offlineStateJson = await _storage.GetItemAsync<string>("offline_game_state");
            if (!string.IsNullOrEmpty(offlineStateJson))
            {
                var offlineState = System.Text.Json.JsonSerializer.Deserialize<OfflineGameState>(offlineStateJson);
                if (offlineState != null && offlineState.IsOfflineMode)
                {
                    _isOfflineMode = true;
                    _logger.LogInformation("从本地恢复离线模式状态");
                }
            }

            // 恢复待处理操作
            var pendingActionsJson = await _storage.GetItemAsync<string>("pending_offline_actions");
            if (!string.IsNullOrEmpty(pendingActionsJson))
            {
                var pendingActions = System.Text.Json.JsonSerializer.Deserialize<List<OfflineAction>>(pendingActionsJson);
                if (pendingActions != null)
                {
                    _pendingActions.Clear();
                    foreach (var action in pendingActions)
                    {
                        _pendingActions.Enqueue(action);
                    }
                    _logger.LogInformation("从本地恢复 {Count} 个待处理操作", pendingActions.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从本地恢复离线状态失败");
        }
    }

    /// <summary>
    /// 执行待处理的操作
    /// </summary>
    private async Task ExecutePendingAction(OfflineAction action, GameApiService apiService)
    {
        try
        {
            switch (action.Type)
            {
                case OfflineActionType.StartBattle:
                    var battleRequest = System.Text.Json.JsonSerializer.Deserialize<StartBattleRequest>(action.Data);
                    if (battleRequest != null)
                    {
                        await apiService.StartBattleAsync(battleRequest);
                    }
                    break;
                    
                case OfflineActionType.StopBattle:
                    var battleId = System.Text.Json.JsonSerializer.Deserialize<Guid>(action.Data);
                    await apiService.StopBattleAsync(battleId);
                    break;
                    
                case OfflineActionType.UpdateCharacter:
                    await HandleCharacterUpdateSync(action, apiService);
                    break;
                    
                case OfflineActionType.SyncTeamProgress:
                    await HandleTeamProgressSync(action, apiService);
                    break;
                    
                case OfflineActionType.SyncOfflineBattleProgress:
                    await HandleOfflineBattleProgressSync(action, apiService);
                    break;
            }
            
            _logger.LogInformation("成功执行离线操作: {ActionType}", action.Type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行离线操作失败: {ActionType}", action.Type);
            throw;
        }
    }

    /// <summary>
    /// 处理角色更新同步
    /// </summary>
    private async Task HandleCharacterUpdateSync(OfflineAction action, GameApiService apiService)
    {
        var updateData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(action.Data);
        if (updateData != null && updateData.ContainsKey("CharacterId"))
        {
            var characterId = updateData["CharacterId"].ToString();
            if (!string.IsNullOrEmpty(characterId))
            {
                // 获取本地角色数据
                var localData = await GetLocalCharacterDataAsync(characterId);
                if (localData != null)
                {
                    // 计算离线期间的进度
                    var offlineTime = DateTime.UtcNow - localData.LastActiveTime;
                    var battleProgress = await CalculateOfflineBattleProgress(characterId, offlineTime);
                    
                    // 创建角色更新请求
                    var characterUpdate = new CharacterUpdateRequest
                    {
                        CharacterId = characterId,
                        Updates = new Dictionary<string, object>
                        {
                            ["Health"] = localData.Health,
                            ["Level"] = localData.Level,
                            ["LastActiveTime"] = DateTime.UtcNow,
                            ["OfflineBattleProgress"] = battleProgress,
                            ["Equipment"] = localData.Equipment,
                            ["Skills"] = localData.Skills
                        }
                    };
                    
                    await apiService.UpdateCharacterAsync(characterUpdate);
                }
            }
        }
    }

    /// <summary>
    /// 处理队伍进度同步
    /// </summary>
    private async Task HandleTeamProgressSync(OfflineAction action, GameApiService apiService)
    {
        var syncData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(action.Data);
        if (syncData != null && syncData.ContainsKey("PartyId"))
        {
            var partyId = syncData["PartyId"].ToString();
            if (!string.IsNullOrEmpty(partyId))
            {
                var partyData = await GetLocalPartyDataAsync(partyId);
                if (partyData != null)
                {
                    // 同步队伍离线进度
                    var teamProgress = await SynchronizeTeamOfflineProgress(partyId, partyData.MemberIds);
                    
                    // 发送队伍进度到服务器
                    var teamProgressUpdate = new TeamProgressUpdateRequest
                    {
                        PartyId = partyId,
                        MemberProgresses = teamProgress.MemberProgresses,
                        TeamBonus = teamProgress.TeamBonus,
                        SynchronizationLevel = teamProgress.SynchronizationLevel,
                        SyncTime = teamProgress.SyncTime
                    };
                    
                    await apiService.UpdateTeamProgressAsync(teamProgressUpdate);
                }
            }
        }
    }

    /// <summary>
    /// 处理离线战斗进度同步
    /// </summary>
    private async Task HandleOfflineBattleProgressSync(OfflineAction action, GameApiService apiService)
    {
        var progressData = System.Text.Json.JsonSerializer.Deserialize<OfflineBattleProgressDto>(action.Data);
        if (progressData != null)
        {
            // 发送离线战斗进度到服务器进行结算
            var settlementRequest = new OfflineSettlementRequestDto
            {
                PlayerId = progressData.CharacterId,
                LastActiveTime = DateTime.UtcNow.Subtract(progressData.OfflineTime),
                ForceSettlement = true
            };
            
            await apiService.ProcessOfflineSettlementAsync(settlementRequest);
        }
    }
}

/// <summary>
/// 本地角色数据
/// </summary>
public class LocalCharacterData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int AttackPower { get; set; } = 15;
    public string Profession { get; set; } = "Warrior";
    public DateTime LastActiveTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Equipment { get; set; } = new();
    public List<string> Skills { get; set; } = new();
}

/// <summary>
/// 本地组队数据
/// </summary>
public class LocalPartyData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CaptainId { get; set; } = string.Empty;
    public List<string> MemberIds { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 离线战斗进度DTO
/// </summary>
public class OfflineBattleProgressDto
{
    public string CharacterId { get; set; } = string.Empty;
    public TimeSpan OfflineTime { get; set; }
    public int CharacterLevel { get; set; } = 1;
    public int EstimatedBattles { get; set; }
    public int EstimatedExperience { get; set; }
    public int EstimatedGold { get; set; }
    public double WinRate { get; set; } = 0.7;
    public int MaxWaveReached { get; set; } = 1;
    public Dictionary<string, object> ProgressDetails { get; set; } = new();
}

/// <summary>
/// 队伍离线进度DTO
/// </summary>
public class TeamOfflineProgressDto
{
    public string PartyId { get; set; } = string.Empty;
    public List<OfflineBattleProgressDto> MemberProgresses { get; set; } = new();
    public double TeamBonus { get; set; } = 1.0;
    public double SynchronizationLevel { get; set; } = 1.0;
    public DateTime SyncTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> TeamStats { get; set; } = new();
}

/// <summary>
/// 本地战斗进度计算器
/// </summary>
public class LocalBattleProgressCalculator
{
    private readonly LocalCharacterData _character;
    private readonly TimeSpan _offlineTime;

    public LocalBattleProgressCalculator(LocalCharacterData character, TimeSpan offlineTime)
    {
        _character = character;
        _offlineTime = offlineTime;
    }

    public async Task<OfflineBattleProgressDto> CalculateProgressAsync()
    {
        var progress = new OfflineBattleProgressDto
        {
            CharacterId = _character.Id,
            OfflineTime = _offlineTime,
            CharacterLevel = _character.Level
        };

        // 计算战斗效率
        var efficiency = CalculateCharacterEfficiency();
        var totalHours = _offlineTime.TotalHours;

        // 估算战斗次数（考虑疲劳系统）
        progress.EstimatedBattles = CalculateEstimatedBattles(totalHours, efficiency);

        // 估算胜率
        progress.WinRate = CalculateWinRate(efficiency);

        // 估算奖励
        var (experience, gold) = CalculateRewards(progress.EstimatedBattles, progress.WinRate, efficiency);
        progress.EstimatedExperience = experience;
        progress.EstimatedGold = gold;

        // 估算最高波次
        progress.MaxWaveReached = CalculateMaxWave(progress.EstimatedBattles, progress.WinRate);

        // 添加详细信息
        progress.ProgressDetails = new Dictionary<string, object>
        {
            ["Efficiency"] = efficiency,
            ["TotalHours"] = totalHours,
            ["AverageBattleTime"] = CalculateAverageBattleTime(),
            ["FatigueImpact"] = CalculateFatigueImpact(totalHours)
        };

        return progress;
    }

    private double CalculateCharacterEfficiency()
    {
        var baseEfficiency = 1.0;
        var levelBonus = _character.Level * 0.05;
        var professionBonus = _character.Profession.ToLower() switch
        {
            "warrior" => 1.2,
            "mage" => 1.1,
            "archer" => 1.15,
            _ => 1.0
        };

        // 装备加成
        var equipmentBonus = CalculateEquipmentBonus();

        return (baseEfficiency + levelBonus) * professionBonus * equipmentBonus;
    }

    private double CalculateEquipmentBonus()
    {
        var bonus = 1.0;
        var equipmentCount = _character.Equipment.Count;
        
        // 简单的装备加成计算
        bonus += equipmentCount * 0.1; // 每件装备10%加成
        
        return Math.Min(bonus, 2.0); // 最多200%加成
    }

    private int CalculateEstimatedBattles(double totalHours, double efficiency)
    {
        var baseBattlesPerHour = 4.0; // 基础每小时4次战斗
        var adjustedRate = baseBattlesPerHour * efficiency;
        
        // 考虑疲劳影响
        var fatigueReduction = CalculateFatigueImpact(totalHours);
        adjustedRate *= (1.0 - fatigueReduction);
        
        return Math.Max(1, (int)(totalHours * adjustedRate));
    }

    private double CalculateWinRate(double efficiency)
    {
        var baseWinRate = 0.7;
        var adjustedWinRate = baseWinRate * efficiency;
        
        return Math.Max(0.1, Math.Min(0.95, adjustedWinRate));
    }

    private (int experience, int gold) CalculateRewards(int battleCount, double winRate, double efficiency)
    {
        var avgExpPerBattle = 25 * efficiency;
        var avgGoldPerBattle = 8 * efficiency;
        
        var totalExp = (int)(battleCount * avgExpPerBattle * winRate);
        var totalGold = (int)(battleCount * avgGoldPerBattle * winRate);
        
        return (totalExp, totalGold);
    }

    private int CalculateMaxWave(int battleCount, double winRate)
    {
        // 假设每3次胜利进入下一波
        var victoriesNeededPerWave = 3;
        var estimatedVictories = (int)(battleCount * winRate);
        
        return Math.Max(1, 1 + (estimatedVictories / victoriesNeededPerWave));
    }

    private double CalculateAverageBattleTime()
    {
        return 10.0 + _character.Level * 0.5; // 基础10分钟，每级增加0.5分钟
    }

    private double CalculateFatigueImpact(double totalHours)
    {
        // 疲劳影响：超过6小时后开始产生疲劳
        if (totalHours <= 6) return 0;
        
        var excessHours = totalHours - 6;
        var fatigueImpact = Math.Min(0.5, excessHours * 0.05); // 每小时5%疲劳，最多50%
        
        return fatigueImpact;
    }
}

/// <summary>
/// 离线游戏状态
/// </summary>
public class OfflineGameState
{
    public bool IsOfflineMode { get; set; }
    public DateTime OfflineStartTime { get; set; }
    public int PendingActionsCount { get; set; }
    public int Version { get; set; }
}

/// <summary>
/// 角色更新请求
/// </summary>
public class CharacterUpdateRequest
{
    public string CharacterId { get; set; } = string.Empty;
    public Dictionary<string, object> Updates { get; set; } = new();
}

/// <summary>
/// 队伍进度更新请求
/// </summary>
public class TeamProgressUpdateRequest
{
    public string PartyId { get; set; } = string.Empty;
    public List<OfflineBattleProgressDto> MemberProgresses { get; set; } = new();
    public double TeamBonus { get; set; } = 1.0;
    public double SynchronizationLevel { get; set; } = 1.0;
    public DateTime SyncTime { get; set; } = DateTime.UtcNow;
}