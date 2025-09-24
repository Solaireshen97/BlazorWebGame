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
    /// 战斗流程服务 - 负责战斗刷新、波次管理等流程控制
    /// </summary>
    public class BattleFlowService
    {
        private const double BattleRefreshCooldown = 3.0;
        private const double DungeonWaveRefreshCooldown = 2.0;  // 副本波次间隔时间
        private const double DungeonCompleteRefreshCooldown = 5.0;  // 副本完成后刷新时间
        private readonly Dictionary<Guid, BattleRefreshState> _battleRefreshStates = new();
        private readonly Dictionary<Guid, DungeonWaveRefreshState> _dungeonWaveRefreshStates = new();  // 副本波次刷新状态
        private readonly List<Player> _allCharacters;

        public BattleFlowService(List<Player> allCharacters)
        {
            _allCharacters = allCharacters;
        }

        /// <summary>
        /// 处理战斗完成
        /// </summary>
        public void OnBattleCompleted(BattleContext battle, List<EnemyInfo> enemyInfos)
        {
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

            // 根据战斗类型决定刷新时间
            double refreshCooldown = battle.BattleType == BattleType.Dungeon 
                ? DungeonCompleteRefreshCooldown 
                : BattleRefreshCooldown;

            // 创建刷新状态
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
        /// 处理战斗刷新
        /// </summary>
        public void ProcessBattleRefresh(double elapsedSeconds, BattleManager battleManager)
        {
            // 处理普通战斗和副本完成后的刷新
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

            // 处理副本波次刷新
            ProcessDungeonWaveRefresh(elapsedSeconds, battleManager);
        }

        /// <summary>
        /// 处理副本波次刷新
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
                    // 准备下一波
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
        /// 冷却结束后开始新战斗
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
        /// 开始下一次副本挑战
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
        /// 开启一个与之前相似的战斗
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
        /// 收集战斗中的敌人信息
        /// </summary>
        public List<EnemyInfo> CollectEnemyInfos(BattleContext battle)
        {
            var result = new List<EnemyInfo>();

            // 如果战斗中还有敌人，直接收集
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

            // 从其他来源收集敌人信息
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

            // 副本战斗特殊处理
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
        /// 准备副本战斗波次
        /// </summary>
        public void PrepareDungeonWave(BattleContext battle, Dungeon dungeon, int waveNumber, SkillSystem skillSystem)
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
                        skillSystem.InitializeEnemySkills(enemy);

                        // 初始化敌人攻击冷却（防止立即攻击）
                        enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;

                        // 添加敌人到战斗
                        battle.Enemies.Add(enemy);
                    }
                }
            }

            // 如果有死亡的玩家且允许自动复活，立即复活他们
            if (dungeon.AllowAutoRevive)
            {
                foreach (var player in battle.Players.Where(p => p.IsDead))
                {
                    var characterCombatService = ServiceLocator.GetService<CharacterCombatService>();
                    characterCombatService?.ReviveCharacter(player);
                }
            }

            // 重置所有玩家的攻击冷却，确保新波次不会立即攻击
            foreach (var player in battle.Players)
            {
                player.AttackCooldown = 1.0 / player.AttacksPerSecond;
            }

            // 设置状态为活跃
            battle.State = BattleState.Active;

            // 触发新波次事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.DungeonWaveStarted, battle.Players.FirstOrDefault());
        }

        /// <summary>
        /// 处理副本下一波（带刷新时间）
        /// </summary>
        public void ProcessDungeonNextWave(BattleContext battle, Dungeon dungeon, SkillSystem skillSystem)
        {
            if (battle.WaveNumber >= dungeon.Waves.Count)
            {
                // 副本完成
                battle.State = BattleState.Completed;
            }
            else
            {
                // 先将战斗状态设置为准备中，避免玩家继续攻击
                battle.State = BattleState.Preparing;

                // 清空玩家目标
                battle.PlayerTargets.Clear();

                // 重置所有玩家的攻击冷却
                foreach (var player in battle.Players)
                {
                    player.AttackCooldown = 0;
                }

                // 创建波次刷新状态，等待刷新时间后进入下一波
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
        /// 检查玩家是否处于战斗刷新状态
        /// </summary>
        public bool IsPlayerInBattleRefresh(string playerId)
        {
            // 检查普通战斗刷新
            foreach (var refreshState in _battleRefreshStates.Values)
            {
                if (refreshState.OriginalBattle.Players.Any(p => p.Id == playerId))
                {
                    return true;
                }
            }

            // 检查副本波次刷新
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
        /// 获取玩家战斗刷新剩余时间
        /// </summary>
        public double GetPlayerBattleRefreshTime(string playerId)
        {
            // 检查普通战斗刷新
            foreach (var refreshState in _battleRefreshStates.Values)
            {
                if (refreshState.OriginalBattle.Players.Any(p => p.Id == playerId))
                {
                    return refreshState.RemainingCooldown;
                }
            }

            // 检查副本波次刷新
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
        /// 根据团队规模确定敌人数量
        /// </summary>
        public int DetermineEnemyCount(int memberCount)
        {
            return Math.Max(1, (memberCount + 1) / 2);
        }

        /// <summary>
        /// 根据等级调整属性值
        /// </summary>
        private int AdjustStatByLevel(int baseStat, int levelAdjustment)
        {
            return (int)(baseStat * (1 + 0.1 * levelAdjustment));
        }

        /// <summary>
        /// 取消战斗刷新
        /// </summary>
        public void CancelBattleRefresh(Guid battleId)
        {
            // 移除普通战斗刷新状态
            var refreshToRemove = _battleRefreshStates
                .Where(kvp => kvp.Value.OriginalBattle.Id == battleId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in refreshToRemove)
            {
                _battleRefreshStates.Remove(key);
            }

            // 移除副本波次刷新状态
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
        /// 取消玩家的所有战斗刷新
        /// </summary>
        public void CancelPlayerBattleRefresh(string playerId)
        {
            // 取消普通战斗刷新
            var refreshToRemove = _battleRefreshStates
                .Where(kvp => kvp.Value.OriginalBattle.Players.Any(p => p.Id == playerId))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in refreshToRemove)
            {
                _battleRefreshStates.Remove(key);
            }

            // 取消副本波次刷新
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
    /// 战斗刷新状态
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
    /// 副本波次刷新状态
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
    /// 敌人信息记录
    /// </summary>
    public class EnemyInfo
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}