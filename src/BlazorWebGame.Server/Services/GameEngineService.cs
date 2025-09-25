using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Server.Services;

/// <summary>
/// 服务端游戏引擎，处理所有游戏逻辑
/// </summary>
public class GameEngineService
{
    private readonly Dictionary<Guid, BattleStateDto> _activeBattles = new();
    private readonly ILogger<GameEngineService> _logger;

    public GameEngineService(ILogger<GameEngineService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 开始新战斗
    /// </summary>
    public BattleStateDto StartBattle(StartBattleRequest request)
    {
        var battleId = Guid.NewGuid();
        var battle = new BattleStateDto
        {
            BattleId = battleId,
            CharacterId = request.CharacterId,
            EnemyId = request.EnemyId,
            PartyId = request.PartyId,
            IsActive = true,
            PlayerHealth = 100, // 暂时硬编码，实际应从数据库获取
            PlayerMaxHealth = 100,
            EnemyHealth = 80,
            EnemyMaxHealth = 80,
            LastUpdated = DateTime.UtcNow,
            BattleType = BattleType.Normal
        };

        _activeBattles[battleId] = battle;
        _logger.LogInformation("Battle started: {BattleId} for character {CharacterId}", battleId, request.CharacterId);
        
        return battle;
    }

    /// <summary>
    /// 获取战斗状态
    /// </summary>
    public BattleStateDto? GetBattleState(Guid battleId)
    {
        return _activeBattles.TryGetValue(battleId, out var battle) ? battle : null;
    }

    /// <summary>
    /// 处理战斗逻辑更新 - 在游戏循环中调用
    /// </summary>
    public void ProcessBattleTick(double deltaTime)
    {
        var battlesToUpdate = _activeBattles.Values.Where(b => b.IsActive).ToList();
        
        foreach (var battle in battlesToUpdate)
        {
            // 简单的战斗逻辑模拟
            ProcessSingleBattle(battle, deltaTime);
        }
    }

    /// <summary>
    /// 处理单个战斗的更新
    /// </summary>
    private void ProcessSingleBattle(BattleStateDto battle, double deltaTime)
    {
        if (!battle.IsActive) return;

        // 简单的战斗模拟：玩家和敌人互相攻击
        var playerDamage = Random.Shared.Next(5, 15);
        var enemyDamage = Random.Shared.Next(3, 12);

        battle.EnemyHealth = Math.Max(0, battle.EnemyHealth - playerDamage);
        battle.PlayerHealth = Math.Max(0, battle.PlayerHealth - enemyDamage);
        battle.LastUpdated = DateTime.UtcNow;

        // 检查战斗是否结束
        if (battle.EnemyHealth <= 0 || battle.PlayerHealth <= 0)    
        {
            battle.IsActive = false;
            _logger.LogInformation("Battle ended: {BattleId}", battle.BattleId);
        }
    }

    /// <summary>
    /// 获取所有需要更新的战斗状态
    /// </summary>
    public List<BattleStateDto> GetAllBattleUpdates()
    {
        return _activeBattles.Values.Where(b => b.IsActive).ToList();
    }

    /// <summary>
    /// 停止战斗
    /// </summary>
    public bool StopBattle(Guid battleId)
    {
        if (_activeBattles.TryGetValue(battleId, out var battle))
        {
            battle.IsActive = false;
            _logger.LogInformation("Battle manually stopped: {BattleId}", battleId);
            return true;
        }
        return false;
    }
}