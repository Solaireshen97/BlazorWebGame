using System.Timers;
using BlazorWebGame.Utils;
using BlazorWebGame.Models;

namespace BlazorWebGame.Services;

/// <summary>
/// 管理全局游戏状态和后台逻辑的单例服务
/// </summary>
public class GameStateService : IAsyncDisposable
{
    private readonly GameStorage _gameStorage;
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100; // 游戏循环间隔（毫秒），越小越精确
    private const double RevivalDuration = 60; // 复活所需时间（秒）

    private double _playerAttackCooldown = 0;
    private double _enemyAttackCooldown = 0;

    public Player Player { get; private set; } = new();
    public Enemy? CurrentEnemy { get; private set; }
    public bool IsPlayerDead { get; private set; } = false;
    public double RevivalTimeRemaining { get; private set; } = 0;

    // 新增：用于UI绑定的攻击进度
    public double PlayerAttackProgress => GetAttackProgress(_playerAttackCooldown, Player.AttacksPerSecond);
    public double EnemyAttackProgress => CurrentEnemy == null ? 0 : GetAttackProgress(_enemyAttackCooldown, CurrentEnemy.AttacksPerSecond);

    // 新增：可供选择的怪物列表
    public List<Enemy> AvailableMonsters => MonsterTemplates.All;

    public event Action? OnStateChanged;

    public GameStateService(GameStorage gameStorage)
    {
        _gameStorage = gameStorage;
    }

    public async Task InitializeAsync()
    {
        var loadedPlayer = await _gameStorage.LoadPlayerAsync();
        if (loadedPlayer != null)
        {
            Player = loadedPlayer;
        }

        if (CurrentEnemy == null)
        {
            // 默认生成第一个模板的怪物
            SpawnNewEnemy(AvailableMonsters.First());
        }

        _gameLoopTimer = new System.Timers.Timer(GameLoopIntervalMs);
        _gameLoopTimer.Elapsed += GameLoopTick;
        _gameLoopTimer.AutoReset = true;
        _gameLoopTimer.Start();
    }

    private void GameLoopTick(object? sender, ElapsedEventArgs e)
    {
        double elapsedSeconds = GameLoopIntervalMs / 1000.0;

        // 1. 处理玩家死亡和复活
        if (IsPlayerDead)
        {
            RevivalTimeRemaining -= elapsedSeconds;
            if (RevivalTimeRemaining <= 0)
            {
                RevivePlayer();
            }
            NotifyStateChanged();
            return; // 玩家死亡时，停止所有其他逻辑
        }

        // 2. 自动战斗逻辑
        if (CurrentEnemy != null)
        {
            // 玩家攻击
            _playerAttackCooldown -= elapsedSeconds;
            if (_playerAttackCooldown <= 0)
            {
                PlayerAttackEnemy();
                _playerAttackCooldown += 1.0 / Player.AttacksPerSecond; // 根据攻速重置冷却
            }

            // 敌人攻击
            _enemyAttackCooldown -= elapsedSeconds;
            if (_enemyAttackCooldown <= 0)
            {
                EnemyAttackPlayer();
                _enemyAttackCooldown += 1.0 / CurrentEnemy.AttacksPerSecond; // 根据攻速重置冷却
            }
        }

        NotifyStateChanged();
    }

    private double GetAttackProgress(double currentCooldown, double attacksPerSecond)
    {
        if (attacksPerSecond <= 0) return 0;
        var totalCooldown = 1.0 / attacksPerSecond;
        var progress = (totalCooldown - currentCooldown) / totalCooldown;
        return Math.Clamp(progress * 100, 0, 100);
    }

    private void PlayerAttackEnemy()
    {
        if (CurrentEnemy == null) return;

        CurrentEnemy.Health -= Player.GetTotalAttackPower();

        if (CurrentEnemy.Health <= 0)
        {
            Player.Gold += CurrentEnemy.GetGoldDropAmount();
            // 击败后重生同一个敌人
            SpawnNewEnemy(CurrentEnemy);
        }
    }

    private void EnemyAttackPlayer()
    {
        if (CurrentEnemy == null) return;

        Player.Health -= CurrentEnemy.AttackPower;

        if (Player.Health <= 0)
        {
            HandlePlayerDeath();
        }
    }

    private void HandlePlayerDeath()
    {
        IsPlayerDead = true;
        Player.Health = 0;
        RevivalTimeRemaining = RevivalDuration;
    }

    private void RevivePlayer()
    {
        IsPlayerDead = false;
        Player.Health = Player.MaxHealth;
        RevivalTimeRemaining = 0;
    }

    /// <summary>
    /// 根据模板生成一个新敌人
    /// </summary>
    public void SpawnNewEnemy(Enemy enemyTemplate)
    {
        CurrentEnemy = enemyTemplate.Clone();
        // 重置攻击冷却
        if (CurrentEnemy != null)
        {
            _enemyAttackCooldown = 1.0 / CurrentEnemy.AttacksPerSecond;
        }
        NotifyStateChanged();
    }

    public async Task SaveStateAsync()
    {
        await _gameStorage.SavePlayerAsync(Player);
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public async ValueTask DisposeAsync()
    {
        _gameLoopTimer?.Stop();
        _gameLoopTimer?.Dispose();
        await SaveStateAsync();
    }
}