using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services.Combat
{
    /// <summary>
    /// ս�����̷��� - ����ս��ˢ�¡����ι�������̿���
    /// </summary>
    public class BattleFlowService
    {
        private const double BattleRefreshCooldown = 3.0;
        private const double DungeonWaveRefreshCooldown = 2.0;  // �������μ��ʱ��
        private const double DungeonCompleteRefreshCooldown = 5.0;  // ������ɺ�ˢ��ʱ��
        private readonly Dictionary<Guid, BattleRefreshState> _battleRefreshStates = new();
        private readonly Dictionary<Guid, DungeonWaveRefreshState> _dungeonWaveRefreshStates = new();  // ��������ˢ��״̬
        private readonly List<Player> _allCharacters;

        public BattleFlowService(List<Player> allCharacters)
        {
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// ����ս�����
        /// </summary>
        public void OnBattleCompleted(BattleContext battle, List<EnemyInfo> enemyInfos)
        {
            // �������û�е�����Ϣ������Ĭ�ϵ�
            if (!enemyInfos.Any() && battle.BattleType != BattleType.Dungeon)
            {
                if (battle.Players.Any())
                {
                    var player = battle.Players.First();
                    var playerLevel = player.GetLevel(player.SelectedBattleProfession);

                    var suitableEnemy = MonsterTemplates.All
                        .Where(m => Math.Abs(m.Level - playerLevel) <= 2)
                        .OrderBy(m => Math.Abs(m.Level - playerLevel))
                        .FirstOrDefault();

                    if (suitableEnemy != null)
                    {
                        enemyInfos.Add(new EnemyInfo
                        {
                            Name = suitableEnemy.Name,
                            Count = battle.BattleType == BattleType.Party ?
                                DetermineEnemyCount(battle.Players.Count) : 1
                        });
                    }
                }
            }

            // ����ս�����;���ˢ��ʱ��
            double refreshCooldown = battle.BattleType == BattleType.Dungeon 
                ? DungeonCompleteRefreshCooldown 
                : BattleRefreshCooldown;

            // ����ˢ��״̬
            _battleRefreshStates[battle.Id] = new BattleRefreshState
            {
                OriginalBattle = battle,
                RemainingCooldown = refreshCooldown,
                BattleType = battle.BattleType,
                EnemyInfos = enemyInfos,
                DungeonId = battle.DungeonId
            };
        }

        /// <summary>
        /// ����ս��ˢ��
        /// </summary>
        public void ProcessBattleRefresh(double elapsedSeconds, BattleManager battleManager)
        {
            // ������ͨս���͸�����ɺ��ˢ��
            var refreshesToRemove = new List<Guid>();

            foreach (var kvp in _battleRefreshStates)
            {
                var refreshState = kvp.Value;
                refreshState.RemainingCooldown -= elapsedSeconds;

                if (refreshState.RemainingCooldown <= 0)
                {
                    StartNewBattleAfterCooldown(refreshState, battleManager);
                    refreshesToRemove.Add(kvp.Key);
                }
            }

            foreach (var id in refreshesToRemove)
            {
                _battleRefreshStates.Remove(id);
            }

            // ����������ˢ��
            ProcessDungeonWaveRefresh(elapsedSeconds, battleManager);
        }

        /// <summary>
        /// ����������ˢ��
        /// </summary>
        private void ProcessDungeonWaveRefresh(double elapsedSeconds, BattleManager battleManager)
        {
            var waveRefreshesToRemove = new List<Guid>();

            foreach (var kvp in _dungeonWaveRefreshStates)
            {
                var waveRefreshState = kvp.Value;
                waveRefreshState.RemainingCooldown -= elapsedSeconds;

                if (waveRefreshState.RemainingCooldown <= 0)
                {
                    // ׼����һ��
                    PrepareDungeonWave(
                        waveRefreshState.Battle, 
                        waveRefreshState.Dungeon, 
                        waveRefreshState.NextWaveNumber, 
                        waveRefreshState.SkillSystem
                    );
                    waveRefreshesToRemove.Add(kvp.Key);
                }
            }

            foreach (var id in waveRefreshesToRemove)
            {
                _dungeonWaveRefreshStates.Remove(id);
            }
        }

        /// <summary>
        /// ��ȴ������ʼ��ս��
        /// </summary>
        private void StartNewBattleAfterCooldown(BattleRefreshState refreshState, BattleManager battleManager)
        {
            var originalBattle = refreshState.OriginalBattle;

            if (!originalBattle.Players.Any(p => !p.IsDead))
                return;

            switch (refreshState.BattleType)
            {
                case BattleType.Dungeon:
                    if (!string.IsNullOrEmpty(refreshState.DungeonId))
                    {
                        StartNextDungeonRun(originalBattle, refreshState.DungeonId, battleManager);
                    }
                    break;

                case BattleType.Party:
                case BattleType.Solo:
                    StartSimilarBattle(originalBattle, refreshState.EnemyInfos, battleManager);
                    break;
            }
        }

        /// <summary>
        /// ��ʼ��һ�θ�����ս
        /// </summary>
        private void StartNextDungeonRun(BattleContext originalBattle, string dungeonId, BattleManager battleManager)
        {
            var party = originalBattle.Party;
            if (party == null) return;

            var alivePlayers = originalBattle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any()) return;

            battleManager.StartDungeon(party, dungeonId);
        }

        /// <summary>
        /// ����һ����֮ǰ���Ƶ�ս��
        /// </summary>
        private void StartSimilarBattle(BattleContext originalBattle, List<EnemyInfo> enemyInfos, BattleManager battleManager)
        {
            var party = originalBattle.Party;
            var alivePlayers = originalBattle.Players.Where(p => !p.IsDead).ToList();

            if (!alivePlayers.Any())
                return;

            var enemies = new List<Enemy>();

            if (enemyInfos != null && enemyInfos.Any())
            {
                foreach (var info in enemyInfos)
                {
                    if (string.IsNullOrEmpty(info.Name))
                        continue;

                    var template = MonsterTemplates.All.FirstOrDefault(m => m.Name == info.Name);
                    if (template != null)
                    {
                        for (int i = 0; i < info.Count; i++)
                        {
                            enemies.Add(template.Clone());
                        }
                    }
                }
            }

            if (!enemies.Any())
            {
                var player = alivePlayers.First();
                var playerLevel = player.GetLevel(player.SelectedBattleProfession);

                var suitableEnemy = MonsterTemplates.All
                    .Where(m => Math.Abs(m.Level - playerLevel) <= 2)
                    .OrderBy(m => Math.Abs(m.Level - playerLevel))
                    .FirstOrDefault();

                if (suitableEnemy != null)
                {
                    int count = party != null ?
                        DetermineEnemyCount(alivePlayers.Count) : 1;

                    for (int i = 0; i < count; i++)
                    {
                        enemies.Add(suitableEnemy.Clone());
                    }
                }
            }

            if (!enemies.Any())
                return;

            battleManager.StartMultiEnemyBattle(alivePlayers.First(), enemies, party);
        }

        /// <summary>
        /// �ռ�ս���еĵ�����Ϣ
        /// </summary>
        public List<EnemyInfo> CollectEnemyInfos(BattleContext battle)
        {
            var result = new List<EnemyInfo>();

            // ���ս���л��е��ˣ�ֱ���ռ�
            if (battle.Enemies.Any())
            {
                var groupedEnemies = battle.Enemies.GroupBy(e => e.Name);
                foreach (var group in groupedEnemies)
                {
                    result.Add(new EnemyInfo
                    {
                        Name = group.Key,
                        Count = group.Count()
                    });
                }
                return result;
            }

            // ��������Դ�ռ�������Ϣ
            if (battle.Party?.CurrentEnemy != null)
            {
                result.Add(new EnemyInfo
                {
                    Name = battle.Party.CurrentEnemy.Name,
                    Count = battle.BattleType == BattleType.Party ?
                        DetermineEnemyCount(battle.Players.Count) : 1
                });
                return result;
            }

            foreach (var player in battle.Players)
            {
                if (player.CurrentEnemy != null && !result.Any(e => e.Name == player.CurrentEnemy.Name))
                {
                    result.Add(new EnemyInfo
                    {
                        Name = player.CurrentEnemy.Name,
                        Count = 1
                    });
                }
            }

            // ����ս�����⴦��
            if (result.Count == 0 && battle.BattleType == BattleType.Dungeon && !string.IsNullOrEmpty(battle.DungeonId))
            {
                var dungeon = DungeonTemplates.GetDungeonById(battle.DungeonId);
                if (dungeon != null && battle.WaveNumber > 0 && battle.WaveNumber <= dungeon.Waves.Count)
                {
                    var wave = dungeon.Waves[battle.WaveNumber - 1];
                    foreach (var spawnInfo in wave.Enemies)
                    {
                        result.Add(new EnemyInfo
                        {
                            Name = spawnInfo.EnemyTemplateName,
                            Count = spawnInfo.Count
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// ׼������ս������
        /// </summary>
        public void PrepareDungeonWave(BattleContext battle, Dungeon dungeon, int waveNumber, SkillSystem skillSystem)
        {
            if (waveNumber <= 0 || waveNumber > dungeon.Waves.Count)
                return;

            var wave = dungeon.Waves[waveNumber - 1];
            battle.WaveNumber = waveNumber;
            battle.Enemies.Clear();
            battle.AllowAutoRevive = dungeon.AllowAutoRevive;

            // ���ɲ��ε���
            foreach (var spawnInfo in wave.Enemies)
            {
                var template = MonsterTemplates.All.FirstOrDefault(m => m.Name == spawnInfo.EnemyTemplateName);
                if (template != null)
                {
                    for (int i = 0; i < spawnInfo.Count; i++)
                    {
                        var enemy = template.Clone();

                        // Ӧ�õȼ������Ե���
                        if (spawnInfo.LevelAdjustment != 0)
                        {
                            enemy.Level += spawnInfo.LevelAdjustment;
                            enemy.AttackPower = AdjustStatByLevel(enemy.AttackPower, spawnInfo.LevelAdjustment);
                        }

                        // Ӧ��Ѫ������
                        if (spawnInfo.HealthMultiplier != 1.0)
                        {
                            enemy.MaxHealth = (int)(enemy.MaxHealth * spawnInfo.HealthMultiplier);
                            enemy.Health = enemy.MaxHealth;
                        }

                        // ��ʼ��������ȴ
                        skillSystem.InitializeEnemySkills(enemy);

                        // ��ʼ�����˹�����ȴ����ֹ����������
                        enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;

                        // ��ӵ��˵�ս��
                        battle.Enemies.Add(enemy);
                    }
                }
            }

            // ���������������������Զ����������������
            if (dungeon.AllowAutoRevive)
            {
                foreach (var player in battle.Players.Where(p => p.IsDead))
                {
                    var characterCombatService = ServiceLocator.GetService<CharacterCombatService>();
                    characterCombatService?.ReviveCharacter(player);
                }
            }

            // ����������ҵĹ�����ȴ��ȷ���²��β�����������
            foreach (var player in battle.Players)
            {
                player.AttackCooldown = 1.0 / player.AttacksPerSecond;
            }

            // ����״̬Ϊ��Ծ
            battle.State = BattleState.Active;

            // �����²����¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.DungeonWaveStarted, battle.Players.FirstOrDefault());
        }

        /// <summary>
        /// ��������һ������ˢ��ʱ�䣩
        /// </summary>
        public void ProcessDungeonNextWave(BattleContext battle, Dungeon dungeon, SkillSystem skillSystem)
        {
            if (battle.WaveNumber >= dungeon.Waves.Count)
            {
                // �������
                battle.State = BattleState.Completed;
            }
            else
            {
                // �Ƚ�ս��״̬����Ϊ׼���У�������Ҽ�������
                battle.State = BattleState.Preparing;

                // ������Ŀ��
                battle.PlayerTargets.Clear();

                // ����������ҵĹ�����ȴ
                foreach (var player in battle.Players)
                {
                    player.AttackCooldown = 0;
                }

                // ��������ˢ��״̬���ȴ�ˢ��ʱ��������һ��
                _dungeonWaveRefreshStates[Guid.NewGuid()] = new DungeonWaveRefreshState
                {
                    Battle = battle,
                    Dungeon = dungeon,
                    NextWaveNumber = battle.WaveNumber + 1,
                    RemainingCooldown = DungeonWaveRefreshCooldown,
                    SkillSystem = skillSystem
                };
            }
        }

        /// <summary>
        /// �������Ƿ���ս��ˢ��״̬
        /// </summary>
        public bool IsPlayerInBattleRefresh(string playerId)
        {
            // �����ͨս��ˢ��
            foreach (var refreshState in _battleRefreshStates.Values)
            {
                if (refreshState.OriginalBattle.Players.Any(p => p.Id == playerId))
                {
                    return true;
                }
            }

            // ��鸱������ˢ��
            foreach (var waveRefreshState in _dungeonWaveRefreshStates.Values)
            {
                if (waveRefreshState.Battle.Players.Any(p => p.Id == playerId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ��ȡ���ս��ˢ��ʣ��ʱ��
        /// </summary>
        public double GetPlayerBattleRefreshTime(string playerId)
        {
            // �����ͨս��ˢ��
            foreach (var refreshState in _battleRefreshStates.Values)
            {
                if (refreshState.OriginalBattle.Players.Any(p => p.Id == playerId))
                {
                    return refreshState.RemainingCooldown;
                }
            }

            // ��鸱������ˢ��
            foreach (var waveRefreshState in _dungeonWaveRefreshStates.Values)
            {
                if (waveRefreshState.Battle.Players.Any(p => p.Id == playerId))
                {
                    return waveRefreshState.RemainingCooldown;
                }
            }

            return 0;
        }

        /// <summary>
        /// �����Ŷӹ�ģȷ����������
        /// </summary>
        public int DetermineEnemyCount(int memberCount)
        {
            return Math.Max(1, (memberCount + 1) / 2);
        }

        /// <summary>
        /// ���ݵȼ���������ֵ
        /// </summary>
        private int AdjustStatByLevel(int baseStat, int levelAdjustment)
        {
            return (int)(baseStat * (1 + 0.1 * levelAdjustment));
        }

        /// <summary>
        /// ȡ��ս��ˢ��
        /// </summary>
        public void CancelBattleRefresh(Guid battleId)
        {
            // �Ƴ���ͨս��ˢ��״̬
            var refreshToRemove = _battleRefreshStates
                .Where(kvp => kvp.Value.OriginalBattle.Id == battleId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in refreshToRemove)
            {
                _battleRefreshStates.Remove(key);
            }

            // �Ƴ���������ˢ��״̬
            var waveRefreshToRemove = _dungeonWaveRefreshStates
                .Where(kvp => kvp.Value.Battle.Id == battleId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in waveRefreshToRemove)
            {
                _dungeonWaveRefreshStates.Remove(key);
            }
        }

        /// <summary>
        /// ȡ����ҵ�����ս��ˢ��
        /// </summary>
        public void CancelPlayerBattleRefresh(string playerId)
        {
            // ȡ����ͨս��ˢ��
            var refreshToRemove = _battleRefreshStates
                .Where(kvp => kvp.Value.OriginalBattle.Players.Any(p => p.Id == playerId))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in refreshToRemove)
            {
                _battleRefreshStates.Remove(key);
            }

            // ȡ����������ˢ��
            var waveRefreshToRemove = _dungeonWaveRefreshStates
                .Where(kvp => kvp.Value.Battle.Players.Any(p => p.Id == playerId))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in waveRefreshToRemove)
            {
                _dungeonWaveRefreshStates.Remove(key);
            }
        }
    }

    /// <summary>
    /// ս��ˢ��״̬
    /// </summary>
    public class BattleRefreshState
    {
        public BattleContext OriginalBattle { get; set; }
        public double RemainingCooldown { get; set; }
        public BattleType BattleType { get; set; }
        public List<EnemyInfo> EnemyInfos { get; set; } = new();
        public string? DungeonId { get; set; }
    }

    /// <summary>
    /// ��������ˢ��״̬
    /// </summary>
    public class DungeonWaveRefreshState
    {
        public BattleContext Battle { get; set; }
        public Dungeon Dungeon { get; set; }
        public int NextWaveNumber { get; set; }
        public double RemainingCooldown { get; set; }
        public SkillSystem SkillSystem { get; set; }
    }



    /// <summary>
    /// ������Ϣ��¼
    /// </summary>
    public class EnemyInfo
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}