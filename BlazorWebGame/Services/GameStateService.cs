using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using BlazorWebGame.Models;
using BlazorWebGame.Utils;

namespace BlazorWebGame.Services;

public class GameStateService : IAsyncDisposable
{
    private readonly GameStorage _gameStorage;
    private System.Timers.Timer? _gameLoopTimer;
    private const int GameLoopIntervalMs = 100;
    private const double RevivalDuration = 60;

    private double _playerAttackCooldown = 0;
    private double _enemyAttackCooldown = 0;

    public Player Player { get; private set; } = new();
    public Enemy? CurrentEnemy { get; private set; }
    public bool IsPlayerDead { get; private set; } = false;
    public double RevivalTimeRemaining { get; private set; } = 0;

    public double PlayerAttackProgress => GetAttackProgress(_playerAttackCooldown, Player.AttacksPerSecond);
    public double EnemyAttackProgress => CurrentEnemy == null ? 0 : GetAttackProgress(_enemyAttackCooldown, CurrentEnemy.AttacksPerSecond);

    public List<Enemy> AvailableMonsters => MonsterTemplates.All;
    public const int MaxEquippedSkills = 4;

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

        // ��Ϸ����ʱ��Ϊ��ҳ�ʼ��/������м���
        InitializePlayerState();

        if (CurrentEnemy == null && AvailableMonsters.Any())
        {
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

        if (IsPlayerDead)
        {
            RevivalTimeRemaining -= elapsedSeconds;
            if (RevivalTimeRemaining <= 0)
            {
                RevivePlayer();
            }
            NotifyStateChanged();
            return;
        }

        if (Player != null && CurrentEnemy != null)
        {
            _playerAttackCooldown -= elapsedSeconds;
            if (_playerAttackCooldown <= 0)
            {
                PlayerAttackEnemy();
                _playerAttackCooldown += 1.0 / Player.AttacksPerSecond;
            }

            _enemyAttackCooldown -= elapsedSeconds;
            if (_enemyAttackCooldown <= 0)
            {
                EnemyAttackPlayer();
                _enemyAttackCooldown += 1.0 / CurrentEnemy.AttacksPerSecond;
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
        if (Player == null || CurrentEnemy == null) return;

        CurrentEnemy.Health -= Player.GetTotalAttackPower();

        if (CurrentEnemy.Health <= 0)
        {
            Player.Gold += CurrentEnemy.GetGoldDropAmount();

            var profession = Player.SelectedBattleProfession;
            var oldLevel = Player.GetLevel(profession);
            Player.AddBattleXP(profession, CurrentEnemy.XpReward);
            var newLevel = Player.GetLevel(profession);

            if (newLevel > oldLevel)
            {
                // ����ʱ��ֻ����µȼ��ļ��ܼ���
                CheckForNewSkillUnlocks(profession, newLevel);
            }

            SpawnNewEnemy(CurrentEnemy);
        }
    }

    private void EnemyAttackPlayer()
    {
        if (Player == null || CurrentEnemy == null) return;
        Player.Health -= CurrentEnemy.AttackPower;
        if (Player.Health <= 0)
        {
            HandlePlayerDeath();
        }
    }

    /// <summary>
    /// ��ʼ������֤���״̬��ȷ��ӵ������Ӧ�õļ��ܡ�
    /// ����Ϸ���ػ���Ҹ������á�
    /// </summary>
    private void InitializePlayerState()
    {
        if (Player == null) return;

        // ��������ս��ְҵ
        foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
        {
            var currentLevel = Player.GetLevel(profession);
            // ��鲢������ְҵ�ڵ�ǰ�ȼ���Ӧ��ӵ�е����м���
            CheckForNewSkillUnlocks(profession, currentLevel, true);
        }
        NotifyStateChanged();
    }

    /// <summary>
    /// ��鲢��������
    /// </summary>
    /// <param name="profession">Ҫ����ְҵ</param>
    /// <param name="level">Ҫ���ĵȼ�</param>
    /// <param name="checkAllLevels">���Ϊtrue���������е��ڵ��ڸõȼ��ļ��ܣ����ڳ�ʼ����������ֻ���õȼ��ļ��ܣ�����������</param>
    private void CheckForNewSkillUnlocks(BattleProfession profession, int level, bool checkAllLevels = false)
    {
        var skillsToLearnQuery = SkillData.AllSkills.Where(s => s.RequiredProfession == profession);

        if (checkAllLevels)
        {
            // ��ʼ��ʱ��������� <= level �ļ���
            skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel <= level);
        }
        else
        {
            // ����ʱ��ֻ��� == level �ļ���
            skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel == level);
        }

        var newlyLearnedSkills = skillsToLearnQuery.ToList();

        foreach (var skill in newlyLearnedSkills)
        {
            if (skill.Type == SkillType.Shared)
            {
                Player.LearnedSharedSkills.Add(skill.Id);
            }

            if (skill.Type == SkillType.Fixed)
            {
                if (!Player.EquippedSkills.TryGetValue(profession, out var equipped) || !equipped.Contains(skill.Id))
                {
                    Player.EquippedSkills[profession].Insert(0, skill.Id);
                }
            }
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
        if (Player != null)
        {
            Player.Health = Player.MaxHealth;
        }
        RevivalTimeRemaining = 0;

        // ��Ҹ����Ҳ���һ�¼���״̬���Է���һ
        InitializePlayerState();
    }

    public void SetBattleProfession(BattleProfession profession)
    {
        if (Player != null)
        {
            Player.SelectedBattleProfession = profession;
            NotifyStateChanged();
        }
    }

    public void SpawnNewEnemy(Enemy enemyTemplate)
    {
        var originalTemplate = AvailableMonsters.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
        CurrentEnemy = originalTemplate.Clone();
        if (CurrentEnemy != null)
        {
            _enemyAttackCooldown = 1.0 / CurrentEnemy.AttacksPerSecond;
        }
        NotifyStateChanged();
    }

    /// <summary>
    /// ���װ��һ������
    /// </summary>
    public void EquipSkill(string skillId)
    {
        if (Player == null) return;
        var profession = Player.SelectedBattleProfession;
        var equipped = Player.EquippedSkills[profession];
        var skill = SkillData.GetSkillById(skillId);

        if (skill == null || skill.Type == SkillType.Fixed) return; // ����װ�������ڻ�̶�����

        if (equipped.Contains(skillId)) return;

        var currentSelectableSkills = equipped.Count(id => SkillData.GetSkillById(id)?.Type != SkillType.Fixed);

        if (currentSelectableSkills < MaxEquippedSkills)
        {
            equipped.Add(skillId);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// ���ж��һ������
    /// </summary>
    public void UnequipSkill(string skillId)
    {
        if (Player == null) return;
        var profession = Player.SelectedBattleProfession;
        var skill = SkillData.GetSkillById(skillId);

        if (skill == null || skill.Type == SkillType.Fixed) return; // ����ж�¹̶�����

        if (Player.EquippedSkills[profession].Remove(skillId))
        {
            NotifyStateChanged();
        }
    }

    public async Task SaveStateAsync()
    {
        if (Player != null)
        {
            await _gameStorage.SavePlayerAsync(Player);
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public async ValueTask DisposeAsync()
    {
        if (_gameLoopTimer != null)
        {
            _gameLoopTimer.Stop();
            _gameLoopTimer.Dispose();
        }
        await SaveStateAsync();
    }
}