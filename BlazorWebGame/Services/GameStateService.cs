using System.Timers;
using BlazorWebGame.Utils;
using BlazorWebGame.Models;

namespace BlazorWebGame.Services;

/// <summary>
/// ����ȫ����Ϸ״̬�ͺ�̨�߼��ĵ�������
/// </summary>
public class GameStateService : IAsyncDisposable
{
    private readonly GameStorage _gameStorage;
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100; // ��Ϸѭ����������룩��ԽСԽ��ȷ
    private const double RevivalDuration = 60; // ��������ʱ�䣨�룩

    private double _playerAttackCooldown = 0;
    private double _enemyAttackCooldown = 0;

    public Player Player { get; private set; } = new();
    public Enemy? CurrentEnemy { get; private set; }
    public bool IsPlayerDead { get; private set; } = false;
    public double RevivalTimeRemaining { get; private set; } = 0;

    // ����������UI�󶨵Ĺ�������
    public double PlayerAttackProgress => GetAttackProgress(_playerAttackCooldown, Player.AttacksPerSecond);
    public double EnemyAttackProgress => CurrentEnemy == null ? 0 : GetAttackProgress(_enemyAttackCooldown, CurrentEnemy.AttacksPerSecond);

    // �������ɹ�ѡ��Ĺ����б�
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
            // Ĭ�����ɵ�һ��ģ��Ĺ���
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

        // 1. ������������͸���
        if (IsPlayerDead)
        {
            RevivalTimeRemaining -= elapsedSeconds;
            if (RevivalTimeRemaining <= 0)
            {
                RevivePlayer();
            }
            NotifyStateChanged();
            return; // �������ʱ��ֹͣ���������߼�
        }

        // 2. �Զ�ս���߼�
        if (CurrentEnemy != null)
        {
            // ��ҹ���
            _playerAttackCooldown -= elapsedSeconds;
            if (_playerAttackCooldown <= 0)
            {
                PlayerAttackEnemy();
                _playerAttackCooldown += 1.0 / Player.AttacksPerSecond; // ���ݹ���������ȴ
            }

            // ���˹���
            _enemyAttackCooldown -= elapsedSeconds;
            if (_enemyAttackCooldown <= 0)
            {
                EnemyAttackPlayer();
                _enemyAttackCooldown += 1.0 / CurrentEnemy.AttacksPerSecond; // ���ݹ���������ȴ
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
            // ���ܺ�����ͬһ������
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
    /// ����ģ������һ���µ���
    /// </summary>
    public void SpawnNewEnemy(Enemy enemyTemplate)
    {
        CurrentEnemy = enemyTemplate.Clone();
        // ���ù�����ȴ
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