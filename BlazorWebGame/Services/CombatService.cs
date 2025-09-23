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
    /// 战斗系统服务，负责处理所有与战斗相关的逻辑
    /// </summary>
    public class CombatService
    {
        private readonly InventoryService _inventoryService;
        private List<Player> _allCharacters;
        private const double RevivalDuration = 2;
        private Dictionary<Guid, BattleContext> _activeBattles = new();

        // 新增战斗刷新冷却常量
        private const double BattleRefreshCooldown = 3.0; // 战斗结束后等待3秒再开始新战斗

        // 跟踪战斗刷新状态的字典
        private Dictionary<Guid, BattleRefreshState> _battleRefreshStates = new();

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        public CombatService(InventoryService inventoryService, List<Player> allCharacters)
        {
            _inventoryService = inventoryService;
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// 获取活跃战斗上下文
        /// </summary>
        public BattleContext? GetBattleContextForPlayer(string playerId)
        {
            return _activeBattles.Values.FirstOrDefault(b => b.Players.Any(p => p.Id == playerId));
        }

        /// <summary>
        /// 获取活跃战斗上下文
        /// </summary>
        public BattleContext? GetBattleContextForParty(Guid partyId)
        {
            return _activeBattles.Values.FirstOrDefault(b => b.Party?.Id == partyId);
        }

        /// <summary>
        /// 处理所有活跃战斗
        /// </summary>
        public void ProcessAllBattles(double elapsedSeconds)
        {
            var battlesToRemove = new List<Guid>();
            var refreshesToRemove = new List<Guid>();

            // 处理活跃战斗
            foreach (var battle in _activeBattles.Values)
            {
                // 在处理战斗之前，先保存敌人信息（以防战斗结束后丢失）
                var enemyInfosBeforeBattle = CollectEnemyInfosFromCurrentBattle(battle);

                ProcessBattle(battle, elapsedSeconds);

                // 检查战斗是否完成
                if (battle.State == BattleState.Completed)
                {
                    // 使用之前保存的敌人信息，如果当前收集失败的话
                    var enemyInfos = CollectEnemyInfos(battle);

                    if (!enemyInfos.Any() && enemyInfosBeforeBattle.Any())
                    {
                        enemyInfos = enemyInfosBeforeBattle;
                    }

                    // 如果还是没有敌人信息，创建默认的
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

                    // 创建刷新状态，保存完整的敌人信息
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

            // 移除已完成的战斗
            foreach (var id in battlesToRemove)
            {
                _activeBattles.Remove(id);
            }

            // 处理刷新中的战斗
            foreach (var kvp in _battleRefreshStates)
            {
                var refreshState = kvp.Value;
                refreshState.RemainingCooldown -= elapsedSeconds;

                // 刷新时间到，开始新战斗
                if (refreshState.RemainingCooldown <= 0)
                {
                    StartNewBattleAfterCooldown(refreshState);
                    refreshesToRemove.Add(kvp.Key);
                }
            }

            // 移除已刷新的战斗
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
        /// 从当前战斗中收集敌人信息（战斗进行中版本）
        /// </summary>
        private List<EnemyInfo> CollectEnemyInfosFromCurrentBattle(BattleContext battle)
        {
            var result = new List<EnemyInfo>();

            // 如果战斗中有敌人，直接收集
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
        /// 收集战斗中的敌人信息
        /// </summary>
        private List<EnemyInfo> CollectEnemyInfos(BattleContext battle)
        {
            var result = new List<EnemyInfo>();

            // 如果战斗中还有敌人，直接收集
            if (battle.Enemies.Any())
            {
                // 按敌人类型分组并计数
                var groupedEnemies = battle.Enemies.GroupBy(e => e.Name);
                foreach (var group in groupedEnemies)
                {
                    result.Add(new EnemyInfo
                    {
                        Name = group.Key,
                        Count = group.Count()
                    });
                }
                return result; // 直接返回，不需要继续查找
            }

            // 从玩家当前敌人或队伍当前敌人获取
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

            // 检查玩家的当前敌人
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

            // 如果是副本战斗，尝试从当前波次信息获取
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
        /// 获取最后一个敌人的名称
        /// </summary>
        private string? GetLastEnemyName(BattleContext battle)
        {
            // 首先检查战斗中是否有敌人模板（虽然可能已被击败）
            if (battle.Party?.CurrentEnemy != null)
            {
                return battle.Party.CurrentEnemy.Name;
            }

            // 检查玩家的当前敌人
            foreach (var player in battle.Players)
            {
                if (player.CurrentEnemy != null)
                {
                    return player.CurrentEnemy.Name;
                }
            }

            // 如果都没有，尝试从原始敌人列表中获取（应该已经为空，但以防万一）
            return battle.Enemies.FirstOrDefault()?.Name;
        }
        /// <summary>
        /// 冷却结束后开始新战斗
        /// </summary>
        private void StartNewBattleAfterCooldown(BattleRefreshState refreshState)
        {
            var originalBattle = refreshState.OriginalBattle;

            // 检查是否有有效的玩家
            if (!originalBattle.Players.Any(p => !p.IsDead))
                return;

            // 根据不同的战斗类型处理
            switch (refreshState.BattleType)
            {
                case BattleType.Dungeon:
                    // 副本结束后，尝试开启同一个副本的下一次挑战
                    if (!string.IsNullOrEmpty(refreshState.DungeonId))
                    {
                        StartNextDungeonRun(originalBattle, refreshState.DungeonId);
                    }
                    break;

                case BattleType.Party:
                case BattleType.Solo:
                    // 开启相似的战斗
                    StartSimilarBattle(originalBattle, refreshState.EnemyInfos);
                    break;
            }
        }

        /// <summary>
        /// 开启一个与之前相似的战斗
        /// </summary>
        private void StartSimilarBattle(BattleContext originalBattle, List<EnemyInfo> enemyInfos)
        {
            // 确保有玩家和敌人信息
            var party = originalBattle.Party;
            var alivePlayers = originalBattle.Players.Where(p => !p.IsDead).ToList();

            if (!alivePlayers.Any())
                return;

            // 准备敌人列表
            var enemies = new List<Enemy>();

            // 如果提供了敌人信息，使用它创建敌人
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

            // 如果没有有效的敌人信息，尝试根据玩家等级创建敌人
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

            // 确保至少有一个敌人
            if (!enemies.Any())
                return;

            // 开启新战斗
            StartMultiEnemyBattle(alivePlayers.First(), enemies, party);
        }

        /// <summary>
        /// 开始下一次副本挑战
        /// </summary>
        private void StartNextDungeonRun(BattleContext originalBattle, string dungeonId)
        {
            // 获取有效队伍
            var party = originalBattle.Party;
            if (party == null) return;

            // 只有当队伍中有存活的队员时才开启下一次挑战
            var alivePlayers = originalBattle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any()) return;

            // 启动新的副本挑战
            StartDungeon(party, dungeonId);
        }

        /// <summary>
        /// 开始下一轮团队战斗
        /// </summary>
        private void StartNextPartyBattle(BattleContext originalBattle)
        {
            // 获取有效队伍
            var party = originalBattle.Party;
            if (party == null) return;

            // 只有当队伍中有存活的队员时才开启下一轮战斗
            var alivePlayers = originalBattle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any()) return;

            // 使用相似的敌人重启战斗
            var firstPlayer = alivePlayers.First();
            var enemyTemplate = GetEnemyTemplate(originalBattle);

            if (enemyTemplate != null)
            {
                // 传入ignoreRefreshCheck=true以允许自动战斗系统绕过刷新状态检查
                SmartStartBattle(firstPlayer, enemyTemplate, party, true);
            }
        }

        /// <summary>
        /// 开始下一轮单人战斗
        /// </summary>
        private void StartNextSoloBattle(BattleContext originalBattle)
        {
            // 获取存活的玩家
            var player = originalBattle.Players.FirstOrDefault(p => !p.IsDead);
            if (player == null) return;

            // 使用相似的敌人重启战斗
            var enemyTemplate = GetEnemyTemplate(originalBattle);

            if (enemyTemplate != null)
            {
                // 传入ignoreRefreshCheck=true以允许自动战斗系统绕过刷新状态检查
                SmartStartBattle(player, enemyTemplate, null, true);
            }
        }

        /// <summary>
        /// 获取适合下一轮战斗的敌人模板
        /// </summary>
        private Enemy? GetEnemyTemplate(BattleContext battle)
        {
            // 1. 尝试从刷新状态中获取敌人信息
            var refreshState = _battleRefreshStates.Values
                .FirstOrDefault(rs => rs.OriginalBattle.Id == battle.Id);

            if (refreshState?.EnemyInfos != null && refreshState.EnemyInfos.Any())
            {
                // 从敌人信息列表中随机选择一种类型
                var random = new Random();
                var selectedEnemyInfo = refreshState.EnemyInfos[random.Next(refreshState.EnemyInfos.Count)];

                var enemyFromRefresh = MonsterTemplates.All
                    .FirstOrDefault(m => m.Name == selectedEnemyInfo.Name);
                if (enemyFromRefresh != null)
                    return enemyFromRefresh;
            }

            // 2. 尝试获取战斗中的敌人（如果有）
            if (battle.Enemies.Any())
            {
                // 随机选择一个现有敌人类型
                var random = new Random();
                var enemyIndex = random.Next(battle.Enemies.Count);
                var enemyName = battle.Enemies[enemyIndex].Name;

                return MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyName);
            }

            // 3. 从队伍或玩家当前敌人获取
            if (battle.Party?.CurrentEnemy != null)
            {
                return battle.Party.CurrentEnemy;
            }
            else if (battle.Players.Any())
            {
                // 尝试从任意队员的当前敌人中选择
                var playersWithEnemies = battle.Players.Where(p => p.CurrentEnemy != null).ToList();
                if (playersWithEnemies.Any())
                {
                    var random = new Random();
                    var player = playersWithEnemies[random.Next(playersWithEnemies.Count)];
                    return player.CurrentEnemy;
                }
            }

            // 4. 如果都失败，从总怪物列表中选择一个适合玩家等级的
            if (battle.Players.Any())
            {
                var player = battle.Players.First();
                var playerLevel = player.GetLevel(player.SelectedBattleProfession);

                // 获取所有适合等级的敌人
                var suitableEnemies = MonsterTemplates.All
                    .Where(m => Math.Abs(m.Level - playerLevel) <= 2)
                    .ToList();

                if (suitableEnemies.Any())
                {
                    var random = new Random();
                    return suitableEnemies[random.Next(suitableEnemies.Count)];
                }
            }

            // 没有找到合适的敌人模板
            return null;
        }

        /// <summary>
        /// 处理单个战斗
        /// </summary>
        private void ProcessBattle(BattleContext battle, double elapsedSeconds)
        {
            if (battle.State != BattleState.Active)
                return;

            // 处理玩家复活倒计时
            ProcessPlayerRevival(battle, elapsedSeconds);

            // 处理玩家攻击
            foreach (var player in battle.Players.Where(p => !p.IsDead))
            {
                ProcessPlayerAttack(battle, player, elapsedSeconds);
            }

            // 处理敌人攻击
            foreach (var enemy in battle.Enemies.ToList()) // 使用ToList以避免迭代时集合被修改
            {
                ProcessEnemyAttack(battle, enemy, elapsedSeconds);
            }

            // 检查战斗状态
            CheckBattleStatus(battle);
        }

        /// <summary>
        /// 处理玩家复活
        /// </summary>
        private void ProcessPlayerRevival(BattleContext battle, double elapsedSeconds)
        {
            // 只处理允许自动复活的战斗
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
        /// 处理玩家攻击
        /// </summary>
        private void ProcessPlayerAttack(BattleContext battle, Player player, double elapsedSeconds)
        {
            player.AttackCooldown -= elapsedSeconds;
            if (player.AttackCooldown <= 0)
            {
                // 选择目标
                var targetEnemy = SelectTargetForPlayer(battle, player);
                if (targetEnemy != null)
                {
                    // 记录玩家的目标
                    battle.PlayerTargets[player.Id] = targetEnemy.Name;

                    // 执行攻击
                    PlayerAttackEnemy(player, targetEnemy, battle.Party);
                }

                // 重置冷却
                player.AttackCooldown += 1.0 / player.AttacksPerSecond;
            }
        }

        /// <summary>
        /// 处理敌人攻击
        /// </summary>
        private void ProcessEnemyAttack(BattleContext battle, Enemy enemy, double elapsedSeconds)
        {
            enemy.EnemyAttackCooldown -= elapsedSeconds;
            if (enemy.EnemyAttackCooldown <= 0)
            {
                // 选择目标
                var targetPlayer = SelectTargetForEnemy(battle, enemy);
                if (targetPlayer != null)
                {
                    // 执行攻击
                    EnemyAttackPlayer(enemy, targetPlayer);
                }

                // 重置冷却
                enemy.EnemyAttackCooldown += 1.0 / enemy.AttacksPerSecond;
            }
        }

        /// <summary>
        /// 为玩家选择目标
        /// </summary>
        private Enemy? SelectTargetForPlayer(BattleContext battle, Player player)
        {
            // 如果没有敌人，返回null
            if (!battle.Enemies.Any())
                return null;

            // 检查玩家是否已有目标
            if (battle.PlayerTargets.TryGetValue(player.Id, out var targetName))
            {
                var existingTarget = battle.Enemies.FirstOrDefault(e => e.Name == targetName);
                if (existingTarget != null)
                    return existingTarget;
            }

            // 根据策略选择新目标
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
        /// 为敌人选择目标
        /// </summary>
        private Player? SelectTargetForEnemy(BattleContext battle, Enemy enemy)
        {
            // 获取所有存活的玩家
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return null;

            // 根据策略选择目标
            switch (battle.EnemyTargetStrategy)
            {
                case TargetSelectionStrategy.HighestThreat:
                    // 这里简单实现，后续可以增加玩家威胁值计算
                    return alivePlayers.OrderByDescending(p => p.GetTotalAttackPower()).FirstOrDefault();

                case TargetSelectionStrategy.Random:
                default:
                    return alivePlayers[new Random().Next(alivePlayers.Count)];
            }
        }

        /// <summary>
        /// 检查战斗状态
        /// </summary>
        private void CheckBattleStatus(BattleContext battle)
        {
            if (battle.IsCompleted)
            {
                battle.State = BattleState.Completed;

                // 如果玩家获胜，处理奖励
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
        /// 处理战斗胜利
        /// </summary>
        private void HandleBattleVictory(BattleContext battle)
        {
            // 副本战斗胜利处理
            if (battle.BattleType == BattleType.Dungeon && !string.IsNullOrEmpty(battle.DungeonId))
            {
                var dungeon = DungeonData.GetDungeonById(battle.DungeonId);
                if (dungeon != null)
                {
                    // 检查是否是最后一波
                    if (battle.WaveNumber >= dungeon.Waves.Count)
                    {
                        // 副本完成奖励
                        DistributeDungeonRewards(battle, dungeon);

                        // 标记为完成，等待刷新冷却后自动开始新的副本
                        battle.State = BattleState.Completed;
                    }
                    else
                    {
                        // 进入下一波
                        PrepareDungeonWave(battle, dungeon, battle.WaveNumber + 1);
                    }
                }
            }
            // 普通战斗胜利处理
            else
            {
                // 根据战斗类型分配奖励
                if (battle.BattleType == BattleType.Party && battle.Party != null)
                {
                    // 重置团队敌人
                    battle.Party.CurrentEnemy = battle.Enemies.FirstOrDefault()?.Clone();
                    if (battle.Party.CurrentEnemy != null)
                    {
                        InitializeEnemySkills(battle.Party.CurrentEnemy);
                    }
                }
                else
                {
                    // 单人战斗，为每个玩家生成新敌人
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

                // 标记为完成，等待刷新冷却后自动开始新的战斗
                battle.State = BattleState.Completed;
            }

            // 触发战斗完成事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleCompleted);
        }

        /// <summary>
        /// 处理战斗失败
        /// </summary>
        private void HandleBattleDefeat(BattleContext battle)
        {
            // 触发战斗失败事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleDefeated);

            // 标记为完成，但不会自动开始新战斗（因为全部玩家已死亡）
            battle.State = BattleState.Completed;
        }

        /// <summary>
        /// 分配副本奖励
        /// </summary>
        private void DistributeDungeonRewards(BattleContext battle, Dungeon dungeon)
        {
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();
            if (!alivePlayers.Any())
                return;

            var random = new Random();

            // 分配每个奖励
            foreach (var reward in dungeon.Rewards)
            {
                // 根据概率决定是否掉落
                if (random.NextDouble() <= reward.DropChance)
                {
                    // 随机选择一名玩家获得物品奖励
                    if (!string.IsNullOrEmpty(reward.ItemId) && reward.ItemQuantity > 0)
                    {
                        var luckyPlayer = alivePlayers[random.Next(alivePlayers.Count)];
                        _inventoryService.AddItemToInventory(luckyPlayer, reward.ItemId, reward.ItemQuantity);
                    }

                    // 所有玩家获得金币和经验
                    foreach (var player in alivePlayers)
                    {
                        // 金币奖励
                        if (reward.Gold > 0)
                        {
                            player.Gold += reward.Gold;
                        }

                        // 经验奖励
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

            // 更新副本完成记录
            foreach (var player in alivePlayers)
            {
                //if (!player.CompletedDungeons.Contains(dungeon.Id))
                //{
                //    player.CompletedDungeons.Add(dungeon.Id);
                //}
            }
        }

        /// <summary>
        /// 智能开始战斗，根据场景自动选择合适的战斗模式
        /// </summary>
        public bool SmartStartBattle(Player character, Enemy enemyTemplate, Party? party = null, bool ignoreRefreshCheck = false)
        {
            if (character == null || enemyTemplate == null)
                return false;

            // 检查玩家是否处于战斗刷新冷却状态 - 如果是且不忽略检查，不允许开始新战斗
            if (!ignoreRefreshCheck && IsPlayerInBattleRefresh(character.Id))
            {
                // 玩家正在战斗刷新冷却中，不能开始新战斗
                return false;
            }

            // 获取当前战斗上下文（如果存在）
            var existingBattle = GetBattleContextForPlayer(character.Id);
            if (existingBattle != null)
            {
                // 如果已经在战斗中，并且尝试战斗相同的敌人，什么都不做
                if (existingBattle.Enemies.Any(e => e.Name == enemyTemplate.Name))
                    return true;

                // 如果已经在战斗中，但尝试战斗不同敌人，则先结束当前战斗
                _activeBattles.Remove(existingBattle.Id);
            }

            // 如果是队伍成员，检查队伍是否有人在战斗刷新状态
            if (!ignoreRefreshCheck && party != null)
            {
                // 检查队伍是否有任何成员处于战斗刷新状态
                foreach (var memberId in party.MemberIds)
                {
                    if (IsPlayerInBattleRefresh(memberId))
                    {
                        // 队伍中有人在战斗刷新中，不能开始新战斗
                        return false;
                    }
                }
            }


            // 判断战斗类型
            if (party != null)
            {
                // 团队战斗
                var memberCount = party.MemberIds.Count;

                // 创建战斗上下文
                var battle = new BattleContext
                {
                    BattleType = BattleType.Party,
                    Party = party,
                    State = BattleState.Active,
                    PlayerTargetStrategy = TargetSelectionStrategy.LowestHealth,
                    EnemyTargetStrategy = TargetSelectionStrategy.Random,
                    AllowAutoRevive = true // 默认允许普通战斗自动复活
                };

                // 添加团队成员
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                foreach (var member in members)
                {
                    battle.Players.Add(member);

                    // 重置当前活动
                    ResetPlayerAction(member);

                    // 设置为战斗状态
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;
                }

                // 根据团队规模生成适量敌人
                var enemyCount = DetermineEnemyCount(memberCount);
                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = enemyTemplate.Clone();
                    InitializeEnemySkills(enemy);
                    battle.Enemies.Add(enemy);
                }

                // 添加到活跃战斗
                _activeBattles[battle.Id] = battle;
                NotifyStateChanged();
                return true;
            }
            else
            {
                // 单人战斗 - 简单地使用1v1
                if (character.CurrentAction != PlayerActionState.Combat || character.CurrentEnemy?.Name != enemyTemplate.Name)
                {
                    // 重置当前活动
                    ResetPlayerAction(character);

                    // 设置战斗状态
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
        /// 根据团队规模确定敌人数量
        /// </summary>
        private int DetermineEnemyCount(int memberCount)
        {
            // 简单逻辑：每1-2名队员对应1名敌人
            return Math.Max(1, (memberCount + 1) / 2);
        }

        /// <summary>
        /// 重置玩家当前动作状态
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
        /// 准备副本战斗波次
        /// </summary>
        private void PrepareDungeonWave(BattleContext battle, Dungeon dungeon, int waveNumber)
        {
            if (waveNumber <= 0 || waveNumber > dungeon.Waves.Count)
                return;

            var wave = dungeon.Waves[waveNumber - 1];
            battle.WaveNumber = waveNumber;
            battle.Enemies.Clear();
            battle.AllowAutoRevive = dungeon.AllowAutoRevive;
            // 生成波次敌人
            foreach (var spawnInfo in wave.Enemies)
            {
                var template = MonsterTemplates.All.FirstOrDefault(m => m.Name == spawnInfo.EnemyTemplateName);
                if (template != null)
                {
                    for (int i = 0; i < spawnInfo.Count; i++)
                    {
                        var enemy = template.Clone();

                        // 应用等级和属性调整
                        if (spawnInfo.LevelAdjustment != 0)
                        {
                            enemy.Level += spawnInfo.LevelAdjustment;
                            enemy.AttackPower = AdjustStatByLevel(enemy.AttackPower, spawnInfo.LevelAdjustment);
                        }

                        // 应用血量倍率
                        if (spawnInfo.HealthMultiplier != 1.0)
                        {
                            enemy.MaxHealth = (int)(enemy.MaxHealth * spawnInfo.HealthMultiplier);
                            enemy.Health = enemy.MaxHealth;
                        }

                        // 初始化技能冷却
                        InitializeEnemySkills(enemy);

                        // 添加敌人到战斗
                        battle.Enemies.Add(enemy);
                    }
                }
            }

            // 设置状态为活跃
            battle.State = BattleState.Active;

            // 触发新波次事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.DungeonWaveStarted, battle.Players.FirstOrDefault());
        }

        /// <summary>
        /// 根据等级调整属性值
        /// </summary>
        private int AdjustStatByLevel(int baseStat, int levelAdjustment)
        {
            // 简单实现：每级提升10%
            return (int)(baseStat * (1 + 0.1 * levelAdjustment));
        }

        /// <summary>
        /// 处理角色的战斗
        /// </summary>
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
            // 对于还在使用老系统的战斗，保持兼容
            // 死亡的角色不参与任何战斗计算
            if (character.IsDead)
                return;

            var targetEnemy = party?.CurrentEnemy ?? character.CurrentEnemy;

            if (targetEnemy == null)
                return;

            // 玩家攻击逻辑
            character.AttackCooldown -= elapsedSeconds;
            if (character.AttackCooldown <= 0)
            {
                PlayerAttackEnemy(character, targetEnemy, party);
                character.AttackCooldown += 1.0 / character.AttacksPerSecond;
            }

            // 敌人攻击逻辑
            targetEnemy.EnemyAttackCooldown -= elapsedSeconds;
            if (targetEnemy.EnemyAttackCooldown <= 0)
            {
                Player? playerToAttack = null;
                if (party != null)
                {
                    // 敌人只会选择活着的成员进行攻击
                    var aliveMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                    if (aliveMembers.Any())
                    {
                        playerToAttack = aliveMembers[new Random().Next(aliveMembers.Count)];
                    }
                }
                else
                {
                    playerToAttack = character; // 单人模式
                }

                if (playerToAttack != null)
                {
                    EnemyAttackPlayer(targetEnemy, playerToAttack);
                }

                // 只有当敌人确实攻击了，才重置它的冷却
                if (playerToAttack != null)
                {
                    targetEnemy.EnemyAttackCooldown += 1.0 / targetEnemy.AttacksPerSecond;
                }
            }
        }

        /// <summary>
        /// 玩家攻击敌人
        /// </summary>
        public void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
        {
            // 应用技能和普通攻击
            ApplyCharacterSkills(character, enemy);

            // 记录原始血量用于计算伤害
            int originalHealth = enemy.Health;
            enemy.Health -= character.GetTotalAttackPower();
            int damageDealt = originalHealth - enemy.Health;

            // 触发敌人受伤事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseCombatEvent(
                GameEventType.EnemyDamaged,
                character,
                enemy,
                damageDealt,
                null,
                party
            );

            // 如果敌人血量降至0，处理战利品分配
            if (enemy.Health <= 0)
            {
                // 触发敌人死亡事件
                gameStateService?.RaiseCombatEvent(
                    GameEventType.EnemyKilled,
                    character,
                    enemy,
                    null,
                    null,
                    party
                );

                // 检查是否是新战斗系统中的敌人
                var battle = _activeBattles.Values.FirstOrDefault(b => b.Enemies.Contains(enemy));
                if (battle != null)
                {
                    // 从战斗中移除敌人
                    battle.Enemies.Remove(enemy);

                    // 更新玩家的目标
                    foreach (var playerId in battle.PlayerTargets.Keys.ToList())
                    {
                        if (battle.PlayerTargets[playerId] == enemy.Name)
                        {
                            battle.PlayerTargets.Remove(playerId);
                        }
                    }

                    // 战斗结束检查会在ProcessBattle中进行
                }
                else
                {
                    // 旧战斗系统，使用原来的逻辑
                    var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemy.Name) ?? enemy;

                    if (party != null)
                    {
                        // 团队奖励分配
                        HandlePartyLoot(party, enemy, originalTemplate);
                    }
                    else
                    {
                        // 个人奖励分配
                        HandleSoloLoot(character, enemy, originalTemplate);
                    }
                }
            }
        }

        /// <summary>
        /// 处理团队击败敌人后的战利品分配
        /// </summary>
        private void HandlePartyLoot(Party party, Enemy enemy, Enemy originalTemplate)
        {
            // 获取队伍成员列表
            var partyMembers = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (!partyMembers.Any())
            {
                party.CurrentEnemy = originalTemplate.Clone();
                return;
            }

            var memberCount = partyMembers.Count;
            var random = new Random();

            // 分配金币
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

            // 分配战利品
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    var luckyMemberForLoot = partyMembers[random.Next(memberCount)];
                    _inventoryService.AddItemToInventory(luckyMemberForLoot, lootItem.Key, 1);
                }
            }

            // 分配经验和任务进度
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

            // 为团队生成新敌人
            party.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(party.CurrentEnemy);
        }

        /// <summary>
        /// 处理单人击败敌人后的战利品分配
        /// </summary>
        private void HandleSoloLoot(Player character, Enemy enemy, Enemy originalTemplate)
        {
            // 金币奖励
            character.Gold += enemy.GetGoldDropAmount();
            
            // 掉落物品
            var random = new Random();
            foreach (var lootItem in enemy.LootTable)
            {
                if (random.NextDouble() <= lootItem.Value)
                {
                    _inventoryService.AddItemToInventory(character, lootItem.Key, 1);
                }
            }

            // 经验值和任务进度
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

            // 为玩家生成新敌人
            character.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// 敌人攻击玩家
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
        /// 处理角色死亡
        /// </summary>
        public void HandleCharacterDeath(Player character)
        {
            // 如果角色已经死了，就没必要再执行一次死亡逻辑了
            if (character.IsDead) return;

            character.IsDead = true;
            character.Health = 0;
            character.RevivalTimeRemaining = RevivalDuration;

            // 死亡时移除大部分buff，但保留食物buff
            character.ActiveBuffs.RemoveAll(buff =>
            {
                var item = ItemData.GetItemById(buff.SourceItemId);
                return item is Consumable consumable && consumable.Category != ConsumableCategory.Food;
            });

            // 检查玩家所在的战斗上下文
            var battleContext = _activeBattles.Values.FirstOrDefault(b => b.Players.Contains(character));

            // 如果玩家在多对多战斗中死亡，检查战斗是否应该继续
            if (battleContext != null)
            {
                // 在下一个战斗处理周期中，CheckBattleStatus会检查是否所有玩家都死亡
                // 根据AllowAutoRevive属性决定是否结束战斗
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// 角色复活
        /// </summary>
        public void ReviveCharacter(Player character)
        {
            character.IsDead = false;
            character.Health = character.GetTotalMaxHealth();
            character.RevivalTimeRemaining = 0;
            NotifyStateChanged();
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartCombat(Player character, Enemy enemyTemplate, Party? party)
        {
            if (character == null || enemyTemplate == null) return;

            if (party != null)
            {
                // 团队战斗逻辑
                HandlePartyStartCombat(character, enemyTemplate, party);
            }
            else
            {
                // 个人战斗逻辑
                HandleSoloStartCombat(character, enemyTemplate);
            }

            NotifyStateChanged();
        }

        /// <summary>
        /// 开始副本战斗
        /// </summary>
        public bool StartDungeon(Party party, string dungeonId)
        {
            if (party == null || string.IsNullOrEmpty(dungeonId))
                return false;

            var dungeon = DungeonData.GetDungeonById(dungeonId);
            if (dungeon == null)
                return false;

            // 验证参与人数
            var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (members.Count < dungeon.MinPlayers || members.Count > dungeon.MaxPlayers)
                return false;

            // 创建战斗上下文
            var battle = new BattleContext
            {
                BattleType = BattleType.Dungeon,
                Party = party,
                DungeonId = dungeonId,
                State = BattleState.Preparing,
                AllowAutoRevive = true // 初始设置为允许复活，具体副本可以覆盖此设置
            };

            // 添加参与的玩家
            foreach (var member in members)
            {
                if (!member.IsDead)
                {
                    battle.Players.Add(member);

                    // 设置玩家状态
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;

                    // 重置其他活动
                    member.CurrentGatheringNode = null;
                    member.CurrentRecipe = null;
                    member.GatheringCooldown = 0;
                    member.CraftingCooldown = 0;
                }
            }

            // 准备第一波战斗
            PrepareDungeonWave(battle, dungeon, 1);

            // 添加到活跃战斗列表
            _activeBattles[battle.Id] = battle;

            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// 开始多对多普通战斗
        /// </summary>
        public bool StartMultiEnemyBattle(Player character, List<Enemy> enemies, Party? party = null)
        {
            if (character == null || enemies == null || !enemies.Any())
                return false;

            // 创建战斗上下文
            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active
            };

            // 添加玩家
            if (party != null)
            {
                // 团队战斗
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
                // 单人战斗
                battle.Players.Add(character);
                character.CurrentAction = PlayerActionState.Combat;
                character.AttackCooldown = 0;
            }

            // 添加敌人
            foreach (var enemyTemplate in enemies)
            {
                var enemy = enemyTemplate.Clone();
                InitializeEnemySkills(enemy);
                battle.Enemies.Add(enemy);
            }

            // 添加到活跃战斗
            _activeBattles[battle.Id] = battle;

            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// 处理团队开始战斗
        /// </summary>
        private void HandlePartyStartCombat(Player character, Enemy enemyTemplate, Party party)
        {
            // 只有队长可以发起团队战斗
            if (party.CaptainId != character.Id)
                return;

            // 如果已经在打同一个敌人，则不需要重新开始
            if (party.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // 创建敌人副本
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            party.CurrentEnemy = originalTemplate.Clone();
            InitializeEnemySkills(party.CurrentEnemy);

            // 设置所有队伍成员的战斗状态
            foreach (var memberId in party.MemberIds)
            {
                var member = _allCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null && !member.IsDead)
                {
                    // 如果成员正在做采集或制作等非战斗、非空闲的活动，重置状态
                    if (member.CurrentAction != PlayerActionState.Idle && member.CurrentAction != PlayerActionState.Combat)
                    {
                        member.CurrentGatheringNode = null;
                        member.CurrentRecipe = null;
                        member.GatheringCooldown = 0;
                        member.CraftingCooldown = 0;
                    }

                    // 设置战斗状态
                    member.CurrentAction = PlayerActionState.Combat;
                    member.AttackCooldown = 0;
                }
            }
        }

        /// <summary>
        /// 处理个人开始战斗
        /// </summary>
        private void HandleSoloStartCombat(Player character, Enemy enemyTemplate)
        {
            // 如果已经在打同一个敌人，则不需要重新开始
            if (character.CurrentAction == PlayerActionState.Combat && character.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // 重置当前状态
            character.CurrentGatheringNode = null;
            character.CurrentRecipe = null;
            character.GatheringCooldown = 0;
            character.CraftingCooldown = 0;
            
            // 设置战斗状态
            character.CurrentAction = PlayerActionState.Combat;
            character.CurrentEnemy = enemyTemplate.Clone();
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// 初始化敌人的技能冷却
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
        /// 应用角色技能效果
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

                    // 技能效果处理
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
                    
                    // 技能触发后进入冷却
                    character.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
                else if (cooldown > 0)
                {
                    // 技能冷却时间减少
                    character.SkillCooldowns[skillId] = cooldown - 1;
                }
            }
        }

        /// <summary>
        /// 应用敌人技能效果
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
        /// 装备技能
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
        /// 卸下技能
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
        /// 检查是否有新技能解锁
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
        /// 重置玩家技能冷却
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
        /// 设置战斗职业
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
        /// 更新任务进度
        /// </summary>
        private void UpdateQuestProgress(Player character, QuestType type, string targetId, int amount)
        {
            if (character == null) return;
            
            // 直接使用ServiceLocator获取QuestService
            var questService = ServiceLocator.GetService<QuestService>();
            if (questService != null)
            {
                questService.UpdateQuestProgress(character, type, targetId, amount);
            }
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();

        /// <summary>
        /// 为角色生成新的敌人实例
        /// </summary>
        public void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate)
        {
            if (character == null || enemyTemplate == null) return;
            
            // 查找敌人模板
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            
            // 克隆敌人
            character.CurrentEnemy = originalTemplate.Clone();
            
            // 初始化敌人技能冷却
            InitializeEnemySkills(character.CurrentEnemy);
        }

        /// <summary>
        /// 设置所有角色列表
        /// </summary>
        public void SetAllCharacters(List<Player> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));

            _allCharacters = characters;
        }


        /// <summary>
        /// 检查玩家是否处于战斗刷新状态
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
        /// 获取玩家战斗刷新剩余时间
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
        /// 副本数据静态类
        /// </summary>
        public static class DungeonData
        {
            /// <summary>
            /// 所有可用副本
            /// </summary>
            public static List<Dungeon> AllDungeons { get; } = new();

            /// <summary>
            /// 通过ID获取副本
            /// </summary>
            public static Dungeon? GetDungeonById(string id)
            {
                return AllDungeons.FirstOrDefault(d => d.Id == id);
            }

            /// <summary>
            /// 初始化副本数据
            /// </summary>
            static DungeonData()
            {
                // 示例副本数据
                AllDungeons.Add(new Dungeon
                {
                    Id = "forest_ruins",
                    Name = "森林遗迹",
                    Description = "一个被遗忘的古代遗迹，现在被各种野生动物和强盗占据。",
                    RecommendedLevel = 5,
                    MinPlayers = 1,
                    MaxPlayers = 3,
                    AllowAutoRevive = true, // 森林遗迹允许自动复活
                    Waves = new List<DungeonWave>
                {
                    new DungeonWave
                    {
                        WaveNumber = 1,
                        Description = "入口守卫",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "森林狼", Count = 3 },
                            new EnemySpawnInfo { EnemyTemplateName = "强盗", Count = 1 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 2,
                        Description = "内部巡逻",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "强盗", Count = 2 },
                            new EnemySpawnInfo { EnemyTemplateName = "强盗弓箭手", Count = 2 }
                        }
                    },
                    new DungeonWave
                    {
                        WaveNumber = 3,
                        Description = "最终BOSS",
                        Enemies = new List<EnemySpawnInfo>
                        {
                            new EnemySpawnInfo { EnemyTemplateName = "强盗头目", Count = 1, IsElite = true, HealthMultiplier = 1.5 }
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

                // 可以继续添加更多副本...
            }
        }
    }
    /// <summary>
    /// 战斗刷新状态
    /// </summary>
    public class BattleRefreshState
    {
        /// <summary>
        /// 原始战斗上下文
        /// </summary>
        public BattleContext OriginalBattle { get; set; }

        /// <summary>
        /// 剩余冷却时间
        /// </summary>
        public double RemainingCooldown { get; set; }

        /// <summary>
        /// 战斗类型
        /// </summary>
        public BattleType BattleType { get; set; }

        /// <summary>
        /// 上一场战斗的敌人信息（类型和数量）
        /// </summary>
        public List<EnemyInfo> EnemyInfos { get; set; } = new();

        /// <summary>
        /// 副本ID（如果是副本战斗）
        /// </summary>
        public string? DungeonId { get; set; }
    }

    /// <summary>
    /// 敌人信息记录
    /// </summary>
    public class EnemyInfo
    {
        /// <summary>
        /// 敌人名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 敌人数量
        /// </summary>
        public int Count { get; set; }
    }
}