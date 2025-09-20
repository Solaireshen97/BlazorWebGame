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

        var equippedSkillIds = Player.EquippedSkills[Player.SelectedBattleProfession];

        // 1. ��������װ���ļ���
        foreach (var skillId in equippedSkillIds)
        {
            var currentCooldown = Player.SkillCooldowns.GetValueOrDefault(skillId);

            // 2. ��鼼���Ƿ���ȴ���
            if (currentCooldown <= 0)
            {
                // �� -> �������ܲ�������ȴ
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    ApplySkillEffect(skill, isPlayerSkill: true);
                    Player.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
            }
            else
            {
                // �� -> ��ȴʱ���1
                Player.SkillCooldowns[skillId]--;
            }
        }

        // 3. ִ����ҵ���ͨ����
        CurrentEnemy.Health -= Player.GetTotalAttackPower();

        // 4. �������Ƿ�����
        if (CurrentEnemy.Health <= 0)
        {
            Player.Gold += CurrentEnemy.GetGoldDropAmount();

            var profession = Player.SelectedBattleProfession;
            var oldLevel = Player.GetLevel(profession);
            Player.AddBattleXP(profession, CurrentEnemy.XpReward);
            var newLevel = Player.GetLevel(profession);

            if (newLevel > oldLevel)
            {
                CheckForNewSkillUnlocks(profession, newLevel);
            }

            SpawnNewEnemy(CurrentEnemy);
        }
    }

    private void EnemyAttackPlayer()
    {
        if (Player == null || CurrentEnemy == null) return;

        // 1. �����������м���
        foreach (var skillId in CurrentEnemy.SkillIds)
        {
            var currentCooldown = CurrentEnemy.SkillCooldowns.GetValueOrDefault(skillId);

            // 2. ��鼼���Ƿ���ȴ���
            if (currentCooldown <= 0)
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    ApplySkillEffect(skill, isPlayerSkill: false);
                    CurrentEnemy.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
            }
            else
            {
                // �� -> ��ȴʱ���1
                CurrentEnemy.SkillCooldowns[skillId]--;
            }
        }

        // 3. ִ�й�����ͨ����
        Player.Health -= CurrentEnemy.AttackPower;
        if (Player.Health <= 0)
        {
            HandlePlayerDeath();
        }
    }

    private void ApplySkillEffect(Skill skill, bool isPlayerSkill)
    {
        if (Player == null || CurrentEnemy == null) return;

        var caster = isPlayerSkill ? (object)Player : CurrentEnemy;
        var target = isPlayerSkill ? (object)CurrentEnemy : Player;

        switch (skill.EffectType)
        {
            case SkillEffectType.DirectDamage:
                if (target is Player p) p.Health -= (int)skill.EffectValue;
                if (target is Enemy e) e.Health -= (int)skill.EffectValue;
                break;
            case SkillEffectType.Heal:
                if (caster is Player pCaster)
                {
                    var healAmount = skill.EffectValue < 1.0 ? (int)(pCaster.MaxHealth * skill.EffectValue) : (int)skill.EffectValue;
                    pCaster.Health = Math.Min(pCaster.MaxHealth, pCaster.Health + healAmount);
                }
                if (caster is Enemy eCaster)
                {
                    var healAmount = skill.EffectValue < 1.0 ? (int)(eCaster.MaxHealth * skill.EffectValue) : (int)skill.EffectValue;
                    eCaster.Health = Math.Min(eCaster.MaxHealth, eCaster.Health + healAmount);
                }
                break;
        }
    }

    private void InitializePlayerState()
    {
        if (Player == null) return;

        foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
        {
            var currentLevel = Player.GetLevel(profession);
            CheckForNewSkillUnlocks(profession, currentLevel, true);
        }

        // �����㣺��������ȴ�����߼���ȡ�������ķ�����
        ResetPlayerSkillCooldowns();

        NotifyStateChanged();
    }

    private void CheckForNewSkillUnlocks(BattleProfession profession, int level, bool checkAllLevels = false)
    {
        if (Player == null) return;

        var skillsToLearnQuery = SkillData.AllSkills.Where(s => s.RequiredProfession == profession);

        if (checkAllLevels)
        {
            skillsToLearnQuery = skillsToLearnQuery.Where(s => s.RequiredLevel <= level);
        }
        else
        {
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
        if (Player == null) return;
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

        // �����㣺��Ҹ���ʱ�����������ĳ�ʼ������ȴ�����߼�
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
        // 1. �Ӿ�̬ģ���б����ҵ���ȷ��ԭʼģ��
        var originalTemplate = AvailableMonsters.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;

        // 2. ʹ������������� Clone() ��������һ���ɾ������ʵ��
        CurrentEnemy = originalTemplate.Clone();

        // 3. �����¹���ļ�����ȴ
        CurrentEnemy.SkillCooldowns.Clear();
        foreach (var skillId in CurrentEnemy.SkillIds)
        {
            var skill = SkillData.GetSkillById(skillId);
            if (skill != null)
            {
                CurrentEnemy.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            }
        }

        if (CurrentEnemy != null)
        {
            _enemyAttackCooldown = 1.0 / CurrentEnemy.AttacksPerSecond;
        }
        NotifyStateChanged();
    }

    public void EquipSkill(string skillId)
    {
        if (Player == null) return;
        var profession = Player.SelectedBattleProfession;
        var equipped = Player.EquippedSkills[profession];
        var skill = SkillData.GetSkillById(skillId);

        if (skill == null || skill.Type == SkillType.Fixed) return;
        if (equipped.Contains(skillId)) return;

        var currentSelectableSkills = equipped.Count(id => SkillData.GetSkillById(id)?.Type != SkillType.Fixed);

        if (currentSelectableSkills < MaxEquippedSkills)
        {
            equipped.Add(skillId);
            // װ���¼���ʱ��ҲӦ�������ʼ��ȴ
            Player.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            NotifyStateChanged();
        }
    }

    public void UnequipSkill(string skillId)
    {
        if (Player == null) return;
        var profession = Player.SelectedBattleProfession;
        var skill = SkillData.GetSkillById(skillId);

        if (skill == null || skill.Type == SkillType.Fixed) return;

        if (Player.EquippedSkills[profession].Remove(skillId))
        {
            Player.SkillCooldowns.Remove(skillId);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// �������������������װ�����ܵ���ȴʱ��
    /// </summary>
    private void ResetPlayerSkillCooldowns()
    {
        if (Player == null) return;

        Player.SkillCooldowns.Clear();
        foreach (var skillId in Player.EquippedSkills.Values.SelectMany(s => s))
        {
            var skill = SkillData.GetSkillById(skillId);
            if (skill != null)
            {
                Player.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
            }
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