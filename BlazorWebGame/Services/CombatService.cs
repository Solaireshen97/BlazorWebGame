using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Models.Skills;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// ս��ϵͳ���񣬸�����������ս����ص��߼�
    /// </summary>
    public class CombatService
    {
        private readonly InventoryService _inventoryService;
        private List<Player> _allCharacters;
        private const double RevivalDuration = 2;
        private Dictionary<Guid, BattleContext> _activeBattles = new();

        // ����ս��ˢ����ȴ����
        private const double BattleRefreshCooldown = 3.0; // ս��������ȴ�3���ٿ�ʼ��ս��

        // ����ս��ˢ��״̬���ֵ�
        private Dictionary<Guid, BattleRefreshState> _battleRefreshStates = new();

        /// <summary>
        /// ״̬����¼�
        /// </summary>
        public event Action? OnStateChanged;

        public CombatService(InventoryService inventoryService, List<Player> allCharacters)
        {
            _inventoryService = inventoryService;
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// ��ȡ��Ծս��������
        /// </summary>
        public BattleContext? GetBattleContextForPlayer(string playerId)
        {
            return _activeBattles.Values.FirstOrDefault(b => b.Players.Any(p => p.Id == playerId));
        }

        /// <summary>
        /// ��ȡ��Ծս��������
        /// </summary>
        public BattleContext? GetBattleContextForParty(Guid partyId)
        {
            return _activeBattles.Values.FirstOrDefault(b => b.Party?.Id == partyId);
        }

        /// <summary>
        /// �������л�Ծս��
        /// </summary>
        public void ProcessAllBattles(double elapsedSeconds)
        {
            var battlesToRemove = new List<Guid>();
            var refreshesToRemove = new List<Guid>();

            // �����Ծս��
            foreach (var battle in _activeBattles.Values)
            {
                // �ڴ���ս��֮ǰ���ȱ��������Ϣ���Է�ս��������ʧ��
                var enemyInfosBeforeBattle = CollectEnemyInfosFromCurrentBattle(battle);

                ProcessBattle(battle, elapsedSeconds);

                // ���ս���Ƿ����
                if (battle.State == BattleState.Completed)
                {
                    // ʹ��֮ǰ����ĵ�����Ϣ�������ǰ�ռ�ʧ�ܵĻ�
                    var enemyInfos = CollectEnemyInfos(battle);

                    if (!enemyInfos.Any() && enemyInfosBeforeBattle.Any())
                    {
                        enemyInfos = enemyInfosBeforeBattle;
                    }

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

                    // ����ˢ��״̬�����������ĵ�����Ϣ
                    _battleRefreshStates[battle.Id] = new BattleRefreshState
                    {
                        OriginalBattle = battle,
                        RemainingCooldown = BattleRefreshCooldown,
                        BattleType = battle.BattleType,
                        EnemyInfos = enemyInfos,
                        DungeonId = battle.DungeonId
                    };

                    battlesToRemove.Add(battle.Id);
                }
            }

            // �Ƴ�����ɵ�ս��
            foreach (var id in battlesToRemove)
            {
                _activeBattles.Remove(id);
            }

            // ����ˢ���е�ս��
            foreach (var kvp in _battleRefreshStates)
            {
                var refreshState = kvp.Value;
                refreshState.RemainingCooldown -= elapsedSeconds;

                // ˢ��ʱ�䵽����ʼ��ս��
                if (refreshState.RemainingCooldown <= 0)
                {
                    StartNewBattleAfterCooldown(refreshState);
                    refreshesToRemove.Add(kvp.Key);
                }
            }

            // �Ƴ���ˢ�µ�ս��
            foreach (var id in refreshesToRemove)
            {
                _battleRefreshStates.Remove(id);
            }

            if (battlesToRemove.Any() || refreshesToRemove.Any())
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// �ӵ�ǰս�����ռ�������Ϣ��ս�������а汾��
        /// </summary>
        private List<EnemyInfo> CollectEnemyInfosFromCurrentBattle(BattleContext battle)
        {
            var result = new List<EnemyInfo>();

            // ���ս�����е��ˣ�ֱ���ռ�
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
            }

            return result;
        }

        /// <summary>
        /// �ռ�ս���еĵ�����Ϣ
        /// </summary>
        private List<EnemyInfo> CollectEnemyInfos(BattleContext battle)
        {
            var result = new List<EnemyInfo>();

            // ���ս���л��е��ˣ�ֱ���ռ�
            if (battle.Enemies.Any())
            {
                // ���������ͷ��鲢����
                var groupedEnemies = battle.Enemies.GroupBy(e => e.Name);
                foreach (var group in groupedEnemies)
                {
                    result.Add(new EnemyInfo
                    {
                        Name = group.Key,
                        Count = group.Count()
                    });
                }
                return result; // ֱ�ӷ��أ�����Ҫ��������
            }

            // ����ҵ�ǰ���˻���鵱ǰ���˻�ȡ
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

            // �����ҵĵ�ǰ����
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

            // ����Ǹ���ս�������Դӵ�ǰ������Ϣ��ȡ
            if (result.Count == 0 && battle.BattleType == BattleType.Dungeon && !string.IsNullOrEmpty(battle.DungeonId))
            {
                var dungeon = DungeonData.GetDungeonById(battle.DungeonId);
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
        /// ��ȡ���һ�����˵�����
        /// </summary>
        private string? GetLastEnemyName(BattleContext battle)
        {
            // ���ȼ��ս�����Ƿ��е���ģ�壨��Ȼ�����ѱ����ܣ�
            if (battle.Party?.CurrentEnemy != null)
            {
                return battle.Party.CurrentEnemy.Name;
            }

            // �����ҵĵ�ǰ����
            foreach (var player in battle.Players)
            {
                if (player.CurrentEnemy != null)
                {
                    return player.CurrentEnemy.Name;
                }
            }

            // �����û�У����Դ�ԭʼ�����б��л�ȡ��Ӧ���Ѿ�Ϊ�գ����Է���һ��
            return battle.Enemies.FirstOrDefault()?.Name;
        }
        /// <summary>
        /// ��ȴ������ʼ��ս��
        /// </summary>
        private void StartNewBattleAfterCooldown(BattleRefreshState refreshState)
        {
            var originalBattle = refreshState.OriginalBattle;

            // ����Ƿ�����Ч�����
            if (!originalBattle.Players.Any(p => !p.IsDead))
                return;

            // ���ݲ�ͬ��ս�����ʹ���
            switch (refreshState.BattleType)
            {
                case BattleType.Dungeon:
                    // ���������󣬳��Կ���ͬһ����������һ����ս
                    if (!string.IsNullOrEmpty(refreshState.DungeonId))
                    {
                        StartNextDungeonRun(originalBattle, refreshState.DungeonId);
                    }
                    break;

                case BattleType.Party:
                case BattleType.Solo:
                    // �������Ƶ�ս��
                    StartSimilarBattle(originalBattle, refreshState.EnemyInfos);
                    break;
            }
        }

        /// <summary>
        /// ����һ����֮ǰ���Ƶ�ս��
        /// </summary>
        private void StartSimilarBattle(BattleContext originalBattle, List<EnemyInfo> enemyInfos)
        {
            // ȷ������Һ͵�����Ϣ
            var party = originalBattle.Party;
            var alivePlayers = originalBattle.Players.Where(p => !p.IsDead).ToList();

            if (!alivePlayers.Any())
                return;

            // ׼�������б�
            var enemies = new List<Enemy>();

            // ����ṩ�˵�����Ϣ��ʹ������������
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

            // ���û����Ч�ĵ�����Ϣ�����Ը�����ҵȼ���������
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

            // ȷ��������һ������
            if (!enemies.Any())
                return;

            // ������ս��
            StartMultiEnemyBattle(alivePlayers.First(), enemies, party);
        }

        /// <summary>
        /// ��ʼ��һ�θ�����ս
        /// </summary>
        private void StartNextDungeonRun(BattleContext originalBattle, string dungeonId)
        {
            // ��ȡ��Ч����
            var party = originalBattle.Party;
            if (party == null) return;

            // ֻ�е��������д��Ķ�Աʱ�ſ�����һ����ս
            var alivePlayers = originalBattle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any()) return;

            // �����µĸ�����ս
            StartDungeon(party, dungeonId);
        }

        /// <summary>
        /// ��ʼ��һ���Ŷ�ս��
        /// </summary>
        private void StartNextPartyBattle(BattleContext originalBattle)
        {
            // ��ȡ��Ч����
            var party = originalBattle.Party;
            if (party == null) return;

            // ֻ�е��������д��Ķ�Աʱ�ſ�����һ��ս��
            var alivePlayers = originalBattle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any()) return;

            // ʹ�����Ƶĵ�������ս��
            var firstPlayer = alivePlayers.First();
            var enemyTemplate = GetEnemyTemplate(originalBattle);

            if (enemyTemplate != null)
            {
                // ����ignoreRefreshCheck=true�������Զ�ս��ϵͳ�ƹ�ˢ��״̬���
                SmartStartBattle(firstPlayer, enemyTemplate, party, true);
            }
        }

        /// <summary>
        /// ��ʼ��һ�ֵ���ս��
        /// </summary>
        private void StartNextSoloBattle(BattleContext originalBattle)
        {
            // ��ȡ�������
            var player = originalBattle.Players.FirstOrDefault(p => !p.IsDead);
            if (player == null) return;

            // ʹ�����Ƶĵ�������ս��
            var enemyTemplate = GetEnemyTemplate(originalBattle);

            if (enemyTemplate != null)
            {
                // ����ignoreRefreshCheck=true�������Զ�ս��ϵͳ�ƹ�ˢ��״̬���
                SmartStartBattle(player, enemyTemplate, null, true);
            }
        }

        /// <summary>
        /// ��ȡ�ʺ���һ��ս���ĵ���ģ��
        /// </summary>
        private Enemy? GetEnemyTemplate(BattleContext battle)
        {
            // 1. ���Դ�ˢ��״̬�л�ȡ������Ϣ
            var refreshState = _battleRefreshStates.Values
                .FirstOrDefault(rs => rs.OriginalBattle.Id == battle.Id);

            if (refreshState?.EnemyInfos != null && refreshState.EnemyInfos.Any())
            {
                // �ӵ�����Ϣ�б������ѡ��һ������
                var random = new Random();
                var selectedEnemyInfo = refreshState.EnemyInfos[random.Next(refreshState.EnemyInfos.Count)];

                var enemyFromRefresh = MonsterTemplates.All
                    .FirstOrDefault(m => m.Name == selectedEnemyInfo.Name);
                if (enemyFromRefresh != null)
                    return enemyFromRefresh;
            }

            // 2. ���Ի�ȡս���еĵ��ˣ�����У�
            if (battle.Enemies.Any())
            {
                // ���ѡ��һ�����е�������
                var random = new Random();
                var enemyIndex = random.Next(battle.Enemies.Count);
                var enemyName = battle.Enemies[enemyIndex].Name;

                return MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyName);
            }

            // 3. �Ӷ������ҵ�ǰ���˻�ȡ
            if (battle.Party?.CurrentEnemy != null)
            {
                return battle.Party.CurrentEnemy;
            }
            else if (battle.Players.Any())
            {
                // ���Դ������Ա�ĵ�ǰ������ѡ��
                var playersWithEnemies = battle.Players.Where(p => p.CurrentEnemy != null).ToList();
                if (playersWithEnemies.Any())
                {
                    var random = new Random();
                    var player = playersWithEnemies[random.Next(playersWithEnemies.Count)];
                    return player.CurrentEnemy;
                }
            }

            // 4. �����ʧ�ܣ����ܹ����б���ѡ��һ���ʺ���ҵȼ���
            if (battle.Players.Any())
            {
                var player = battle.Players.First();
                var playerLevel = player.GetLevel(player.SelectedBattleProfession);

                // ��ȡ�����ʺϵȼ��ĵ���
                var suitableEnemies = MonsterTemplates.All
                    .Where(m => Math.Abs(m.Level - playerLevel) <= 2)
                    .ToList();

                if (suitableEnemies.Any())
                {
                    var random = new Random();
                    return suitableEnemies[random.Next(suitableEnemies.Count)];
                }
            }

            // û���ҵ����ʵĵ���ģ��
            return null;
        }

        /// <summary>
        /// ������ս��
        /// </summary>
        private void ProcessBattle(BattleContext battle, double elapsedSeconds)
        {
            if (battle.State != BattleState.Active)
                return;

            // ������Ҹ����ʱ
            ProcessPlayerRevival(battle, elapsedSeconds);

            // ������ҹ���
            foreach (var player in battle.Players.Where(p => !p.IsDead))
            {
                ProcessPlayerAttack(battle, player, elapsedSeconds);
            }

            // ������˹���
            foreach (var enemy in battle.Enemies.ToList()) // ʹ��ToList�Ա������ʱ���ϱ��޸�
            {
                ProcessEnemyAttack(battle, enemy, elapsedSeconds);
            }

            // ���ս��״̬
            CheckBattleStatus(battle);
        }

        /// <summary>
        /// ������Ҹ���
        /// </summary>
        private void ProcessPlayerRevival(BattleContext battle, double elapsedSeconds)
        {
            // ֻ���������Զ������ս��
            if (!battle.AllowAutoRevive && battle.BattleType == BattleType.Dungeon)
                return;

            foreach (var player in battle.Players.Where(p => p.IsDead))
            {
                player.RevivalTimeRemaining -= elapsedSeconds;

                if (player.RevivalTimeRemaining <= 0)
                {
                    ReviveCharacter(player);
                }
            }
        }

        /// <summary>
        /// ������ҹ���
        /// </summary>
        private void ProcessPlayerAttack(BattleContext battle, Player player, double elapsedSeconds)
        {
            player.AttackCooldown -= elapsedSeconds;
            if (player.AttackCooldown <= 0)
            {
                // ѡ��Ŀ��
                var targetEnemy = SelectTargetForPlayer(battle, player);
                if (targetEnemy != null)
                {
                    // ��¼��ҵ�Ŀ��
                    battle.PlayerTargets[player.Id] = targetEnemy.Name;

                    // ִ�й���
                    PlayerAttackEnemy(player, targetEnemy, battle.Party);
                }

                // ������ȴ
                player.AttackCooldown += 1.0 / player.AttacksPerSecond;
            }
        }

        /// <summary>
        /// ������˹���
        /// </summary>
        private void ProcessEnemyAttack(BattleContext battle, Enemy enemy, double elapsedSeconds)
        {
            enemy.EnemyAttackCooldown -= elapsedSeconds;
            if (enemy.EnemyAttackCooldown <= 0)
            {
                // ѡ��Ŀ��
                var targetPlayer = SelectTargetForEnemy(battle, enemy);
                if (targetPlayer != null)
                {
                    // ִ�й���
                    EnemyAttackPlayer(enemy, targetPlayer);
                }

                // ������ȴ
                enemy.EnemyAttackCooldown += 1.0 / enemy.AttacksPerSecond;
            }
        }

        /// <summary>
        /// Ϊ���ѡ��Ŀ��
        /// </summary>
        private Enemy? SelectTargetForPlayer(BattleContext battle, Player player)
        {
            // ���û�е��ˣ�����null
            if (!battle.Enemies.Any())
                return null;

            // �������Ƿ�����Ŀ��
            if (battle.PlayerTargets.TryGetValue(player.Id, out var targetName))
            {
                var existingTarget = battle.Enemies.FirstOrDefault(e => e.Name == targetName);
                if (existingTarget != null)
                    return existingTarget;
            }

            // ���ݲ���ѡ����Ŀ��
            switch (battle.PlayerTargetStrategy)
            {
                case TargetSelectionStrategy.LowestHealth:
                    return battle.Enemies.OrderBy(e => (double)e.Health / e.MaxHealth).FirstOrDefault();

                case TargetSelectionStrategy.HighestHealth:
                    return battle.Enemies.OrderByDescending(e => (double)e.Health / e.MaxHealth).FirstOrDefault();

                case TargetSelectionStrategy.Random:
                default:
                    return battle.Enemies[new Random().Next(battle.Enemies.Count)];
            }
        }

        /// <summary>
        /// Ϊ����ѡ��Ŀ��
        /// </summary>
        private Player? SelectTargetForEnemy(BattleContext battle, Enemy enemy)
        {
            // ��ȡ���д������
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return null;

            // ���ݲ���ѡ��Ŀ��
            switch (battle.EnemyTargetStrategy)
            {
                case TargetSelectionStrategy.HighestThreat:
                    // �����ʵ�֣������������������вֵ����
                    return alivePlayers.OrderByDescending(p => p.GetTotalAttackPower()).FirstOrDefault();

                case TargetSelectionStrategy.Random:
                default:
                    return alivePlayers[new Random().Next(alivePlayers.Count)];
            }
        }

        /// <summary>
        /// ���ս��״̬
        /// </summary>
        private void CheckBattleStatus(BattleContext battle)
        {
            if (battle.IsCompleted)
            {
                battle.State = BattleState.Completed;

                // �����һ�ʤ��������
                if (battle.IsVictory)
                {
                    HandleBattleVictory(battle);
                }
                else
                {
                    HandleBattleDefeat(battle);
                }
            }
        }

        /// <summary>
        /// ����ս��ʤ��
        /// </summary>
        private void HandleBattleVictory(BattleContext battle)
        {
            // ����ս��ʤ������
            if (battle.BattleType == BattleType.Dungeon && !string.IsNullOrEmpty(battle.DungeonId))
            {
                var dungeon = DungeonData.GetDungeonById(battle.DungeonId);
                if (dungeon != null)
                {
                    // ����Ƿ������һ��
                    if (battle.WaveNumber >= dungeon.Waves.Count)
                    {
                        // ������ɽ���
                        DistributeDungeonRewards(battle, dungeon);

                        // ���Ϊ��ɣ��ȴ�ˢ����ȴ���Զ���ʼ�µĸ���
                        battle.State = BattleState.Completed;
                    }
                    else
                    {
                        // ������һ��
                        PrepareDungeonWave(battle, dungeon, battle.WaveNumber + 1);
                    }
                }
            }
            // ��ͨս��ʤ������
            else
            {
                // ����ս�����ͷ��佱��
                if (battle.BattleType == BattleType.Party && battle.Party != null)
                {
                    // �����Ŷӵ���
                    battle.Party.CurrentEnemy = battle.Enemies.FirstOrDefault()?.Clone();
                    if (battle.Party.CurrentEnemy != null)
                    {
                        InitializeEnemySkills(battle.Party.CurrentEnemy);
                    }
                }
                else
                {
                    // ����ս����Ϊÿ����������µ���
                    foreach (var player in battle.Players)
                    {
                        if (!player.IsDead && battle.Enemies.Any())
                        {
                            player.CurrentEnemy = battle.Enemies.First().Clone();
                            if (player.CurrentEnemy != null)
                            {
                                InitializeEnemySkills(player.CurrentEnemy);
                            }
                        }
                    }
                }

                // ���Ϊ��ɣ��ȴ�ˢ����ȴ���Զ���ʼ�µ�ս��
                battle.State = BattleState.Completed;
            }

            // ����ս������¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleCompleted);
        }

        /// <summary>
        /// ����ս��ʧ��
        /// </summary>
        private void HandleBattleDefeat(BattleContext battle)
        {
            // ����ս��ʧ���¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleDefeated);

            // ���Ϊ��ɣ��������Զ���ʼ��ս������Ϊȫ�������������
            battle.State = BattleState.Completed;
        }

        /// <summary>
        /// ���丱������
        /// </summary>
        private void DistributeDungeonRewards(BattleContext battle, Dungeon dungeon)
        {
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return;

            var random = new Random();

            // ����ÿ������
            foreach (var reward in dungeon.Rewards)
            {
                // ���ݸ��ʾ����Ƿ����
                if (random.NextDouble() <= reward.DropChance)
                {
                    // ���ѡ��һ����һ����Ʒ����
                    if (!string.IsNullOrEmpty(reward.ItemId) && reward.ItemQuantity > 0)
                    {
                        var luckyPlayer = alivePlayers[random.Next(alivePlayers.Count)];
                        _inventoryService.AddItemToInventory(luckyPlayer, reward.ItemId, reward.ItemQuantity);
                    }

                    // ������һ�ý�Һ;���
                    foreach (var player in alivePlayers)
                    {
                        // ��ҽ���
                        if (reward.Gold > 0)
                        {
                            player.Gold += reward.Gold;
                        }

                        // ���齱��
                        if (reward.Experience > 0)
                        {
                            var profession = player.SelectedBattleProfession;
                            var oldLevel = player.GetLevel(profession);
                            player.AddBattleXP(profession, reward.Experience);

                            if (player.GetLevel(profession) > oldLevel)
                            {
                                CheckForNewSkillUnlocks(player, profession, player.GetLevel(profession));
                            }
                        }
                    }
                }
            }

            // ���¸�����ɼ�¼
            foreach (var player in alivePlayers)
            {
                //if (!player.CompletedDungeons.Contains(dungeon.Id))
                //{
                //    player.CompletedDungeons.Add(dungeon.Id);
                //}
            }
        }

        /// <summary>
        /// ���ܿ�ʼս�������ݳ����Զ�ѡ����ʵ�ս��ģʽ
        /// </summary>
        public bool SmartStartBattle(Player character, Enemy enemyTemplate, Party? party = null, bool ignoreRefreshCheck = false)
        {
            if (character == null || enemyTemplate == null)
                return false;

            // �������Ƿ���ս��ˢ����ȴ״̬ - ������Ҳ����Լ�飬������ʼ��ս��
            if (!ignoreRefreshCheck && IsPlayerInBattleRefresh(character.Id))
            {
                // �������ս��ˢ����ȴ�У����ܿ�ʼ��ս��
                return false;
            }

            // ��ȡ��ǰս�������ģ�������ڣ�
            var existingBattle = GetBattleContextForPlayer(character.Id);
            if (existingBattle != null)
            {
                // ����Ѿ���ս���У����ҳ���ս����ͬ�ĵ��ˣ�ʲô������
                if (existingBattle.Enemies.Any(e => e.Name == enemyTemplate.Name))
                    return true;

                // ����Ѿ���ս���У�������ս����ͬ���ˣ����Ƚ�����ǰս��
                _activeBattles.Remove(existingBattle.Id);
            }

            // ����Ƕ����Ա���������Ƿ�������ս��ˢ��״̬
            if (!ignoreRefreshCheck && party != null)
            {
                // �������Ƿ����κγ�Ա����ս��ˢ��״̬
                foreach (var memberId in party.MemberIds)
                {
                    if (IsPlayerInBattleRefresh(memberId))
                    {
                        // ������������ս��ˢ���У����ܿ�ʼ��ս��
                        return false;
                    }
                }
            }


            // �ж�ս������
            if (party != null)
            {
                // �Ŷ�ս��
                var memberCount = party.MemberIds.Count;

                // ����ս��������
                var battle = new BattleContext
                {
                    BattleType = BattleType.Party,
                    Party = party,
                    State = BattleState.Active,
                    PlayerTargetStrategy = TargetSelectionStrategy.LowestHealth,
                    EnemyTargetStrategy = TargetSelectionStrategy.Random,
                    AllowAutoRevive = true // Ĭ��������ͨս���Զ�����
                };

                // ����Ŷӳ�Ա
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                foreach (var member in members)
                {
                    battle.Players.Add(member);

                    // ���õ�ǰ�
                    ResetPlayerAction(member);

                    // ����Ϊս��״̬
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;
                }

                // �����Ŷӹ�ģ������������
                var enemyCount = DetermineEnemyCount(memberCount);
                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = enemyTemplate.Clone();
                    InitializeEnemySkills(enemy);
                    battle.Enemies.Add(enemy);
                }

                // ��ӵ���Ծս��
                _activeBattles[battle.Id] = battle;
                NotifyStateChanged();
                return true;
            }
            else
            {
                // ����ս�� - �򵥵�ʹ��1v1
                if (character.CurrentAction != PlayerActionState.Combat || character.CurrentEnemy?.Name != enemyTemplate.Name)
                {
                    // ���õ�ǰ�
                    ResetPlayerAction(character);

                    // ����ս��״̬
                    character.CurrentAction = PlayerActionState.Combat;
                    character.AttackCooldown = 0;
                    character.CurrentEnemy = enemyTemplate.Clone();
                    InitializeEnemySkills(character.CurrentEnemy);
                }
                NotifyStateChanged();
                return true;
            }
        }
        /// <summary>
        /// �����Ŷӹ�ģȷ����������
        /// </summary>
        private int DetermineEnemyCount(int memberCount)
        {
            // ���߼���ÿ1-2����Ա��Ӧ1������
            return Math.Max(1, (memberCount + 1) / 2);
        }

        /// <summary>
        /// ������ҵ�ǰ����״̬
        /// </summary>
        private void ResetPlayerAction(Player player)
        {
            if (player == null) return;

            player.CurrentGatheringNode = null;
            player.CurrentRecipe = null;
            player.GatheringCooldown = 0;
            player.CraftingCooldown = 0;
        }
        /// <summary>
        /// ׼������ս������
        /// </summary>
        private void PrepareDungeonWave(BattleContext battle, Dungeon dungeon, int waveNumber)
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
                        InitializeEnemySkills(enemy);

                        // ��ӵ��˵�ս��
                        battle.Enemies.Add(enemy);
                    }
                }
            }

            // ����״̬Ϊ��Ծ
            battle.State = BattleState.Active;

            // �����²����¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.DungeonWaveStarted, battle.Players.FirstOrDefault());
        }

        /// <summary>
        /// ���ݵȼ���������ֵ
        /// </summary>
        private int AdjustStatByLevel(int baseStat, int levelAdjustment)
        {
            // ��ʵ�֣�ÿ������10%
            return (int)(baseStat * (1 + 0.1 * levelAdjustment));
        }

        /// <summary>
        /// �����ɫ��ս��
        /// </summary>
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
            // ���ڻ���ʹ����ϵͳ��ս�������ּ���
            // �����Ľ�ɫ�������κ�ս������
            if (character.IsDead)
                return;

            var targetEnemy = party?.CurrentEnemy ?? character.CurrentEnemy;

            if (targetEnemy == null)
                return;

            // ��ҹ����߼�
            character.AttackCooldown -= elapsedSeconds;
            if (character.AttackCooldown <= 0)
            {
                PlayerAttackEnemy(character, targetEnemy, party);
                character.AttackCooldown += 1.0 / character.AttacksPerSecond;
            }

            // ���˹����߼�
            targetEnemy.EnemyAttackCooldown -= elapsedSeconds;
            if (targetEnemy.EnemyAttackCooldown <= 0)
            {
                Player? playerToAttack = null;
                if (party != null)
                {
                    // ����ֻ��ѡ����ŵĳ�Ա���й���
                    var aliveMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                    if (aliveMembers.Any())
                    {
                        playerToAttack = aliveMembers[new Random().Next(aliveMembers.Count)];
                    }
                }
                else
                {
                    playerToAttack = character; // ����ģʽ
                }

                if (playerToAttack != null)
                {
                    EnemyAttackPlayer(targetEnemy, playerToAttack);
                }

                // ֻ�е�����ȷʵ�����ˣ�������������ȴ
                if (playerToAttack != null)
                {
                    targetEnemy.EnemyAttackCooldown += 1.0 / targetEnemy.AttacksPerSecond;
                }
            }
        }

        /// <summary>
        /// ��ҹ�������
        /// </summary>
        public void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
        {
            // Ӧ�ü��ܺ���ͨ����
            ApplyCharacterSkills(character, enemy);

            // ��¼ԭʼѪ�����ڼ����˺�
            int originalHealth = enemy.Health;
            enemy.Health -= character.GetTotalAttackPower();
            int damageDealt = originalHealth - enemy.Health;

            // �������������¼�
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseCombatEvent(
                GameEventType.EnemyDamaged,
                character,
                enemy,
                damageDealt,
                null,
                party
            );

            // �������Ѫ������0������ս��Ʒ����
            if (enemy.Health <= 0)
            {
                // �������������¼�
                gameStateService?.RaiseCombatEvent(
                    GameEventType.EnemyKilled,
                    character,
                    enemy,
                    null,
                    null,
                    party
                );

                // ����Ƿ�����ս��ϵͳ�еĵ���
                var battle = _activeBattles.Values.FirstOrDefault(b => b.Enemies.Contains(enemy));
                if (battle != null)
                {
                    // ��ս�����Ƴ�����
                    battle.Enemies.Remove(enemy);

                    // ������ҵ�Ŀ��
                    foreach (var playerId in battle.PlayerTargets.Keys.ToList())
                    {
                        if (battle.PlayerTargets[playerId] == enemy.Name)
                        {
                            battle.PlayerTargets.Remove(playerId);
                        }
                    }

                    // ս������������ProcessBattle�н���
                }
                else
                {
                    // ��ս��ϵͳ��ʹ��ԭ�����߼�
                    var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;

                    if (party != null)
                    {
                        // �Ŷӽ�������
                        HandlePartyLoot(party, enemy, originalTemplate);
                    }
                    else
                    {
                        // ���˽�������
                        HandleSoloLoot(character, enemy, originalTemplate);
                    }
                }
            }
        }

        /// <summary>
        /// �����Ŷӻ��ܵ��˺��ս��Ʒ����
        /// </summary>
        private void HandlePartyLoot(Party party, Enemy enemy, Enemy originalTemplate)
        {
            // ��ȡ�����Ա�б�
            var partyMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (!partyMembers.Any())
            {
                party.CurrentEnemy = originalTemplate.Clone();
                return;
            }

            var memberCount = partyMembers.Count;
            var random = new Random();

            // ������
            var totalGold = enemy.GetGoldDropAmount();
            var goldPerMember = totalGold / memberCount;
            var remainderGold = totalGold % memberCount;

            foreach (var member in partyMembers)
            {
                member.Gold += goldPerMember;
            }

            if (remainderGold > 0)
            {
                var luckyMemberForGold = partyMembers[random.Next(memberCount)];
                luckyMemberForGold.Gold += remainderGold;
            }

            // ����ս��Ʒ
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    var luckyMemberForLoot = partyMembers[random.Next(memberCount)];
                    _inventoryService.AddItemToInventory(luckyMemberForLoot, lootItem.Key, 1);
                }
            }

            // ���侭����������
            foreach (var member in partyMembers)
            {
                var profession = member.SelectedBattleProfession;
                var oldLevel = member.GetLevel(profession);
                member.AddBattleXP(profession, enemy.XpReward);
                
                if (member.GetLevel(profession) > oldLevel)
                {
                    CheckForNewSkillUnlocks(member, profession, member.GetLevel(profession));
                }

                UpdateQuestProgress(member, QuestType.KillMonster, enemy.Name, 1);
                UpdateQuestProgress(member, QuestType.KillMonster, "any", 1);
                member.DefeatedMonsterIds.Add(enemy.Name);
            }

            // Ϊ�Ŷ������µ���
            party.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(party.CurrentEnemy);
        }

        /// <summary>
        /// �����˻��ܵ��˺��ս��Ʒ����
        /// </summary>
        private void HandleSoloLoot(Player character, Enemy enemy, Enemy originalTemplate)
        {
            // ��ҽ���
            character.Gold += enemy.GetGoldDropAmount();
            
            // ������Ʒ
            var random = new Random();
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    _inventoryService.AddItemToInventory(character, lootItem.Key, 1);
                }
            }

            // ����ֵ���������
            var profession = character.SelectedBattleProfession;
            var oldLevel = character.GetLevel(profession);
            character.AddBattleXP(profession, enemy.XpReward);
            
            if (character.GetLevel(profession) > oldLevel)
            {
                CheckForNewSkillUnlocks(character, profession, character.GetLevel(profession));
            }

            UpdateQuestProgress(character, QuestType.KillMonster, enemy.Name, 1);
            UpdateQuestProgress(character, QuestType.KillMonster, "any", 1);
            character.DefeatedMonsterIds.Add(enemy.Name);

            // Ϊ��������µ���
            character.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// ���˹������
        /// </summary>
        public void EnemyAttackPlayer(Enemy enemy, Player character)
        {
            ApplyEnemySkills(enemy, character);
            character.Health -= enemy.AttackPower;
            
            if (character.Health <= 0)
            {
                HandleCharacterDeath(character);
            }
        }

        /// <summary>
        /// �����ɫ����
        /// </summary>
        public void HandleCharacterDeath(Player character)
        {
            // �����ɫ�Ѿ����ˣ���û��Ҫ��ִ��һ�������߼���
            if (character.IsDead) return;

            character.IsDead = true;
            character.Health = 0;
            character.RevivalTimeRemaining = RevivalDuration;

            // ����ʱ�Ƴ��󲿷�buff��������ʳ��buff
            character.ActiveBuffs.RemoveAll(buff =>
            {
                var item = ItemData.GetItemById(buff.SourceItemId);
                return item is Consumable consumable && consumable.Category != ConsumableCategory.Food;
            });

            // ���������ڵ�ս��������
            var battleContext = _activeBattles.Values.FirstOrDefault(b => b.Players.Contains(character));

            // �������ڶ�Զ�ս�������������ս���Ƿ�Ӧ�ü���
            if (battleContext != null)
            {
                // ����һ��ս�����������У�CheckBattleStatus�����Ƿ�������Ҷ�����
                // ����AllowAutoRevive���Ծ����Ƿ����ս��
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// ��ɫ����
        /// </summary>
        public void ReviveCharacter(Player character)
        {
            character.IsDead = false;
            character.Health = character.GetTotalMaxHealth();
            character.RevivalTimeRemaining = 0;
            NotifyStateChanged();
        }

        /// <summary>
        /// ��ʼս��
        /// </summary>
        public void StartCombat(Player character, Enemy enemyTemplate, Party? party)
        {
            if (character == null || enemyTemplate == null) return;

            if (party != null)
            {
                // �Ŷ�ս���߼�
                HandlePartyStartCombat(character, enemyTemplate, party);
            }
            else
            {
                // ����ս���߼�
                HandleSoloStartCombat(character, enemyTemplate);
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// ��ʼ����ս��
        /// </summary>
        public bool StartDungeon(Party party, string dungeonId)
        {
            if (party == null || string.IsNullOrEmpty(dungeonId))
                return false;

            var dungeon = DungeonData.GetDungeonById(dungeonId);
            if (dungeon == null)
                return false;

            // ��֤��������
            var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (members.Count < dungeon.MinPlayers || members.Count > dungeon.MaxPlayers)
                return false;

            // ����ս��������
            var battle = new BattleContext
            {
                BattleType = BattleType.Dungeon,
                Party = party,
                DungeonId = dungeonId,
                State = BattleState.Preparing,
                AllowAutoRevive = true // ��ʼ����Ϊ��������帱�����Ը��Ǵ�����
            };

            // ��Ӳ�������
            foreach (var member in members)
            {
                if (!member.IsDead)
                {
                    battle.Players.Add(member);

                    // �������״̬
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;

                    // ���������
                    member.CurrentGatheringNode = null;
                    member.CurrentRecipe = null;
                    member.GatheringCooldown = 0;
                    member.CraftingCooldown = 0;
                }
            }

            // ׼����һ��ս��
            PrepareDungeonWave(battle, dungeon, 1);

            // ��ӵ���Ծս���б�
            _activeBattles[battle.Id] = battle;

            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// ��ʼ��Զ���ͨս��
        /// </summary>
        public bool StartMultiEnemyBattle(Player character, List<Enemy> enemies, Party? party = null)
        {
            if (character == null || enemies == null || !enemies.Any())
                return false;

            // ����ս��������
            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active
            };

            // ������
            if (party != null)
            {
                // �Ŷ�ս��
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
                foreach (var member in members)
                {
                    if (!member.IsDead)
                    {
                        battle.Players.Add(member);
                        member.CurrentAction = PlayerActionState.Combat;
                        member.AttackCooldown = 0;
                    }
                }
            }
            else
            {
                // ����ս��
                battle.Players.Add(character);
                character.CurrentAction = PlayerActionState.Combat;
                character.AttackCooldown = 0;
            }

            // ��ӵ���
            foreach (var enemyTemplate in enemies)
            {
                var enemy = enemyTemplate.Clone();
                InitializeEnemySkills(enemy);
                battle.Enemies.Add(enemy);
            }

            // ��ӵ���Ծս��
            _activeBattles[battle.Id] = battle;

            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// �����Ŷӿ�ʼս��
        /// </summary>
        private void HandlePartyStartCombat(Player character, Enemy enemyTemplate, Party party)
        {
            // ֻ�жӳ����Է����Ŷ�ս��
            if (party.CaptainId != character.Id)
                return;

            // ����Ѿ��ڴ�ͬһ�����ˣ�����Ҫ���¿�ʼ
            if (party.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // �������˸���
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            party.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(party.CurrentEnemy);

            // �������ж����Ա��ս��״̬
            foreach (var memberId in party.MemberIds)
            {
                var member = _allCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null && !member.IsDead)
                {
                    // �����Ա�������ɼ��������ȷ�ս�����ǿ��еĻ������״̬
                    if (member.CurrentAction != PlayerActionState.Idle && member.CurrentAction != PlayerActionState.Combat)
                    {
                        member.CurrentGatheringNode = null;
                        member.CurrentRecipe = null;
                        member.GatheringCooldown = 0;
                        member.CraftingCooldown = 0;
                    }

                    // ����ս��״̬
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;
                }
            }
        }

        /// <summary>
        /// ������˿�ʼս��
        /// </summary>
        private void HandleSoloStartCombat(Player character, Enemy enemyTemplate)
        {
            // ����Ѿ��ڴ�ͬһ�����ˣ�����Ҫ���¿�ʼ
            if (character.CurrentAction == PlayerActionState.Combat && character.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // ���õ�ǰ״̬
            character.CurrentGatheringNode = null;
            character.CurrentRecipe = null;
            character.GatheringCooldown = 0;
            character.CraftingCooldown = 0;
            
            // ����ս��״̬
            character.CurrentAction = PlayerActionState.Combat;
            character.CurrentEnemy = enemyTemplate.Clone();
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// ��ʼ�����˵ļ�����ȴ
        /// </summary>
        private void InitializeEnemySkills(Enemy enemy)
        {
            enemy.SkillCooldowns.Clear();
            foreach (var skillId in enemy.SkillIds)
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    enemy.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
                }
            }
            enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;
        }

        /// <summary>
        /// Ӧ�ý�ɫ����Ч��
        /// </summary>
        public void ApplyCharacterSkills(Player character, Enemy enemy)
        {
            var profession = character.SelectedBattleProfession;
            if (!character.EquippedSkills.ContainsKey(profession))
                return;

            var equippedSkillIds = character.EquippedSkills[profession];

            foreach (var skillId in equippedSkillIds)
            {
                var cooldown = character.SkillCooldowns.GetValueOrDefault(skillId, 0);

                if (cooldown == 0)
                {
                    var skill = SkillData.GetSkillById(skillId);
                    if (skill == null) continue;

                    // ����Ч������
                    switch (skill.EffectType)
                    {
                        case SkillEffectType.DirectDamage:
                            enemy.Health -= (int)skill.EffectValue;
                            break;
                        case SkillEffectType.Heal:
                            var healAmount = skill.EffectValue < 1.0
                                ? (int)(character.GetTotalMaxHealth() * skill.EffectValue)
                                : (int)skill.EffectValue;
                            character.Health = Math.Min(character.GetTotalMaxHealth(), character.Health + healAmount);
                            break;
                    }
                    
                    // ���ܴ����������ȴ
                    character.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
                else if (cooldown > 0)
                {
                    // ������ȴʱ�����
                    character.SkillCooldowns[skillId] = cooldown - 1;
                }
            }
        }

        /// <summary>
        /// Ӧ�õ��˼���Ч��
        /// </summary>
        public void ApplyEnemySkills(Enemy enemy, Player character)
        {
            foreach (var skillId in enemy.SkillIds)
            {
                var cooldown = enemy.SkillCooldowns.GetValueOrDefault(skillId, 0);

                if (cooldown == 0)
                {
                    var skill = SkillData.GetSkillById(skillId);
                    if (skill == null) continue;

                    switch (skill.EffectType)
                    {
                        case SkillEffectType.DirectDamage:
                            character.Health -= (int)skill.EffectValue;
                            break;
                        case SkillEffectType.Heal:
                            var healAmount = skill.EffectValue < 1.0
                                ? (int)(enemy.MaxHealth * skill.EffectValue)
                                : (int)skill.EffectValue;
                            enemy.Health = Math.Min(enemy.MaxHealth, enemy.Health + healAmount);
                            break;
                    }
                    enemy.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
                else if (cooldown > 0)
                {
                    enemy.SkillCooldowns[skillId] = cooldown - 1;
                }
            }
        }

        /// <summary>
        /// װ������
        /// </summary>
        public void EquipSkill(Player character, string skillId, int maxEquippedSkills)
        {
            if (character == null) return;
            
            var profession = character.SelectedBattleProfession;
            var equipped = character.EquippedSkills[profession];
            var skill = SkillData.GetSkillById(skillId);
            
            if (skill == null || skill.Type == SkillType.Fixed || equipped.Contains(skillId)) return;
            
            if (equipped.Count(id => SkillData.GetSkillById(id)?.Type != SkillType.Fixed) < maxEquippedSkills)
            {
                equipped.Add(skillId);
                character.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ж�¼���
        /// </summary>
        public void UnequipSkill(Player character, string skillId)
        {
            if (character == null) return;
            
            var skill = SkillData.GetSkillById(skillId);
            if (skill == null || skill.Type == SkillType.Fixed) return;
            
            if (character.EquippedSkills[character.SelectedBattleProfession].Remove(skillId))
            {
                character.SkillCooldowns.Remove(skillId);
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ����Ƿ����¼��ܽ���
        /// </summary>
        public void CheckForNewSkillUnlocks(Player character, BattleProfession profession, int level, bool checkAllLevels = false)
        {
            if (character == null) return;
            
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
                    character.LearnedSharedSkills.Add(skill.Id);
                }
                
                if (skill.Type == SkillType.Fixed)
                {
                    if (!character.EquippedSkills.TryGetValue(profession, out var equipped) || !equipped.Contains(skill.Id))
                    {
                        character.EquippedSkills[profession].Insert(0, skill.Id);
                    }
                }
            }
            
            if (newlyLearnedSkills.Any())
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// ������Ҽ�����ȴ
        /// </summary>
        public void ResetPlayerSkillCooldowns(Player character)
        {
            if (character == null) return;
            
            character.SkillCooldowns.Clear();
            
            foreach (var skillId in character.EquippedSkills.Values.SelectMany(s => s))
            {
                var skill = SkillData.GetSkillById(skillId);
                if (skill != null)
                {
                    character.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
                }
            }
        }

        /// <summary>
        /// ����ս��ְҵ
        /// </summary>
        public void SetBattleProfession(Player character, BattleProfession profession)
        {
            if (character != null)
            {
                character.SelectedBattleProfession = profession;
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// �����������
        /// </summary>
        private void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            if (character == null) return;
            
            // ֱ��ʹ��ServiceLocator��ȡQuestService
            var questService = ServiceLocator.GetService<QuestService>();
            if (questService != null)
            {
                questService.UpdateQuestProgress(character, type, targetId, amount);
            }
        }

        /// <summary>
        /// ����״̬����¼�
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        /// <summary>
        /// Ϊ��ɫ�����µĵ���ʵ��
        /// </summary>
        public void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate)
        {
            if (character == null || enemyTemplate == null) return;
            
            // ���ҵ���ģ��
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            
            // ��¡����
            character.CurrentEnemy = originalTemplate.Clone();
            
            // ��ʼ�����˼�����ȴ
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// �������н�ɫ�б�
        /// </summary>
        public void SetAllCharacters(List<Player> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));

            _allCharacters = characters;
        }


        /// <summary>
        /// �������Ƿ���ս��ˢ��״̬
        /// </summary>
        public bool IsPlayerInBattleRefresh(string playerId)
        {
            foreach (var refreshState in _battleRefreshStates.Values)
            {
                if (refreshState.OriginalBattle.Players.Any(p => p.Id == playerId))
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
            foreach (var refreshState in _battleRefreshStates.Values)
            {
                if (refreshState.OriginalBattle.Players.Any(p => p.Id == playerId))
                {
                    return refreshState.RemainingCooldown;
                }
            }
            return 0;
        }

        /// <summary>
        /// �������ݾ�̬��
        /// </summary>
        public static class DungeonData
        {
            /// <summary>
            /// ���п��ø���
            /// </summary>
            public static List<Dungeon> AllDungeons { get; } = new();

            /// <summary>
            /// ͨ��ID��ȡ����
            /// </summary>
            public static Dungeon? GetDungeonById(string id)
            {
                return AllDungeons.FirstOrDefault(d => d.Id == id);
            }

            /// <summary>
            /// ��ʼ����������
            /// </summary>
            static DungeonData()
            {
                // ʾ����������
                AllDungeons.Add(new Dungeon
                {
                    Id = "forest_ruins",
                    Name = "ɭ���ż�",
                    Description = "һ���������ĹŴ��ż������ڱ�����Ұ�������ǿ��ռ�ݡ�",
                    RecommendedLevel = 5,
                    MinPlayers = 1,
                    MaxPlayers = 3,
                    AllowAutoRevive = true, // ɭ���ż������Զ�����
                    Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "�������",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ɭ����", Count = 3 },
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��", Count = 1 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "�ڲ�Ѳ��",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��", Count = 2 },
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��������", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "����BOSS",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "ǿ��ͷĿ", Count = 1, IsElite = true, HealthMultiplier = 1.5 }
                        }
                    }
                },
                    Rewards = new List<DungeonReward>
                {
                    new DungeonReward { Gold = 500, Experience = 1000 },
                    new DungeonReward { ItemId = "rare_sword", ItemQuantity = 1, DropChance = 0.3 },
                    new DungeonReward { ItemId = "healing_potion", ItemQuantity = 5, DropChance = 0.8 }
                },
                    CooldownHours = 24
                });

                // ���Լ�����Ӹ��ั��...
            }
        }
    }
    /// <summary>
    /// ս��ˢ��״̬
    /// </summary>
    public class BattleRefreshState
    {
        /// <summary>
        /// ԭʼս��������
        /// </summary>
        public BattleContext OriginalBattle { get; set; }

        /// <summary>
        /// ʣ����ȴʱ��
        /// </summary>
        public double RemainingCooldown { get; set; }

        /// <summary>
        /// ս������
        /// </summary>
        public BattleType BattleType { get; set; }

        /// <summary>
        /// ��һ��ս���ĵ�����Ϣ�����ͺ�������
        /// </summary>
        public List<EnemyInfo> EnemyInfos { get; set; } = new();

        /// <summary>
        /// ����ID������Ǹ���ս����
        /// </summary>
        public string? DungeonId { get; set; }
    }

    /// <summary>
    /// ������Ϣ��¼
    /// </summary>
    public class EnemyInfo
    {
        /// <summary>
        /// ��������
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ��������
        /// </summary>
        public int Count { get; set; }
    }
}