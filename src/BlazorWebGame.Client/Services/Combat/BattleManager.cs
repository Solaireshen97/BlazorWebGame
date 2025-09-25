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
    /// 战斗管理器 - 负责战斗实例的生命周期管理
    /// </summary>
    public class BattleManager
    {
        private readonly Dictionary<Guid, BattleContext> _activeBattles = new();
        private readonly List<Player> _allCharacters;
        private readonly CombatEngine _combatEngine;
        private readonly BattleFlowService _battleFlowService;
        private readonly CharacterCombatService _characterCombatService;
        private readonly SkillSystem _skillSystem;
        private readonly LootService _lootService;  // 添加这行

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        public BattleManager(
            List<Player> allCharacters,
            CombatEngine combatEngine,
            BattleFlowService battleFlowService,
            CharacterCombatService characterCombatService,
            SkillSystem skillSystem,
            LootService lootService)  // 添加参数
        {
            _allCharacters = allCharacters;
            _combatEngine = combatEngine;
            _battleFlowService = battleFlowService;
            _characterCombatService = characterCombatService;
            _skillSystem = skillSystem;
            _lootService = lootService;  // 添加赋值
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

            // 处理活跃战斗
            foreach (var battle in _activeBattles.Values)
            {
                ProcessBattle(battle, elapsedSeconds);

                // 检查战斗是否完成
                if (battle.State == BattleState.Completed)
                {
                    // 收集敌人信息用于战斗刷新
                    var enemyInfos = _battleFlowService.CollectEnemyInfos(battle);

                    // 清理所有参与战斗的玩家状态
                    foreach (var player in battle.Players)
                    {
                        player.CurrentAction = PlayerActionState.Idle;
                        player.CurrentEnemy = null;
                        player.AttackCooldown = 0;
                    }

                    // 清理队伍状态
                    if (battle.Party != null)
                    {
                        battle.Party.CurrentEnemy = null;
                    }

                    // 通知战斗流程服务处理战斗结束
                    _battleFlowService.OnBattleCompleted(battle, enemyInfos);

                    battlesToRemove.Add(battle.Id);
                }
            }

            // 移除已完成的战斗
            foreach (var id in battlesToRemove)
            {
                _activeBattles.Remove(id);
            }

            // 处理战斗刷新逻辑
            _battleFlowService.ProcessBattleRefresh(elapsedSeconds, this);

            if (battlesToRemove.Any())
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 处理单个战斗
        /// </summary>
        private void ProcessBattle(BattleContext battle, double elapsedSeconds)
        {
            // 只处理活跃状态的战斗
            if (battle.State != BattleState.Active)
                return;

            // 处理玩家复活倒计时
            _characterCombatService.ProcessPlayerRevival(battle, elapsedSeconds);

            // 检查是否有存活的玩家
            var alivePlayers = battle.Players.Where(p => !p.IsDead).ToList();

            if (alivePlayers.Any())
            {
                // 有存活玩家时，处理玩家攻击
                foreach (var player in alivePlayers)
                {
                    _combatEngine.ProcessPlayerAttack(battle, player, elapsedSeconds);
                }

                // 处理敌人攻击
                foreach (var enemy in battle.Enemies.ToList())
                {
                    _combatEngine.ProcessEnemyAttack(battle, enemy, elapsedSeconds);
                }
            }
            else
            {
                // 所有玩家死亡，重置所有敌人的攻击冷却
                foreach (var enemy in battle.Enemies)
                {
                    enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;
                }
            }

            // 检查战斗状态
            CheckBattleStatus(battle);
        }

        private void CheckBattleStatus(BattleContext battle)
        {
            // 特殊处理：允许自动复活的副本
            if (battle.BattleType == BattleType.Dungeon && battle.AllowAutoRevive)
            {
                // 即使所有玩家死亡，也继续战斗
                if (!battle.Enemies.Any())
                {
                    // 敌人全部死亡，交给 LootService 处理奖励和下一波
                    _lootService.HandleBattleVictory(battle, _battleFlowService);
                }
                // 不检查玩家是否全部死亡
                return;
            }

            // 普通战斗完成检查
            if (battle.IsCompleted)
            {
                battle.State = BattleState.Completed;

                var gameStateService = ServiceLocator.GetService<GameStateService>();
                if (battle.IsVictory)
                {
                    // 所有胜利处理都交给 LootService
                    _lootService.HandleBattleVictory(battle, _battleFlowService);

                    gameStateService?.RaiseEvent(GameEventType.BattleCompleted);
                }
                else
                {
                    // 失败处理
                    _lootService.HandleBattleDefeat(battle);

                    gameStateService?.RaiseEvent(GameEventType.BattleDefeated);
                }
            }
        }
        /// <summary>
        /// 智能开始战斗，根据场景自动选择合适的战斗模式
        /// </summary>
        public bool SmartStartBattle(Player character, Enemy enemyTemplate, Party? party = null, bool ignoreRefreshCheck = false)
        {
            if (character == null || enemyTemplate == null)
                return false;

            // 检查战斗刷新状态
            if (!ignoreRefreshCheck && _battleFlowService.IsPlayerInBattleRefresh(character.Id))
            {
                return false;
            }

            // 结束当前角色（以及队伍成员）的所有战斗
            var playersToCheck = new List<Player> { character };
            if (party != null)
            {
                playersToCheck.AddRange(_allCharacters.Where(c => party.MemberIds.Contains(c.Id)));
            }

            foreach (var player in playersToCheck.Distinct())
            {
                var existingBattle = GetBattleContextForPlayer(player.Id);
                if (existingBattle != null)
                {
                    // 如果是同一个战斗中的同类型敌人，不做处理
                    if (existingBattle.Players.Contains(character) &&
                        existingBattle.Enemies.Any(e => e.Name == enemyTemplate.Name))
                        return true;

                    // 否则结束当前战斗
                    foreach (var p in existingBattle.Players)
                    {
                        p.CurrentAction = PlayerActionState.Idle;
                        p.CurrentEnemy = null;
                        p.AttackCooldown = 0;
                    }
                    _activeBattles.Remove(existingBattle.Id);
                }
            }

            // 团队成员刷新状态检查
            if (!ignoreRefreshCheck && party != null)
            {
                foreach (var memberId in party.MemberIds)
                {
                    if (_battleFlowService.IsPlayerInBattleRefresh(memberId))
                    {
                        return false;
                    }
                }
            }

            // 创建新战斗（保持原有逻辑）
            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active,
                PlayerTargetStrategy = TargetSelectionStrategy.Random,
                EnemyTargetStrategy = TargetSelectionStrategy.Random,
                AllowAutoRevive = true
            };

            // 添加玩家
            if (party != null)
            {
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                foreach (var member in members)
                {
                    battle.Players.Add(member);
                    _characterCombatService.PrepareCharacterForBattle(member);
                }

                var enemyCount = _battleFlowService.DetermineEnemyCount(members.Count);
                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = enemyTemplate.Clone();
                    _skillSystem.InitializeEnemySkills(enemy);

                    // 添加: 初始化怪物战斗属性
                    MonsterTemplates.InitializeCombatAttributes(enemy);

                    // 初始化敌人攻击冷却
                    enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;

                    battle.Enemies.Add(enemy);
                }
            }
            else
            {
                battle.Players.Add(character);
                _characterCombatService.PrepareCharacterForBattle(character);

                // 在单人模式部分
                var enemy = enemyTemplate.Clone();
                _skillSystem.InitializeEnemySkills(enemy);

                // 添加: 初始化怪物战斗属性
                MonsterTemplates.InitializeCombatAttributes(enemy);

                // 初始化敌人攻击冷却
                enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;

                battle.Enemies.Add(enemy);
            }

            // 清理旧系统的状态
            character.CurrentEnemy = null;
            if (party != null)
            {
                party.CurrentEnemy = null;
            }

            _activeBattles[battle.Id] = battle;
            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// 开始副本战斗
        /// </summary>
        public bool StartDungeon(Party party, string dungeonId)
        {
            if (party == null || string.IsNullOrEmpty(dungeonId))
                return false;

            var dungeon = DungeonTemplates.GetDungeonById(dungeonId);
            if (dungeon == null)
                return false;

            // 验证参与人数
            var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id)).ToList();
            if (members.Count < dungeon.MinPlayers || members.Count > dungeon.MaxPlayers)
                return false;

            // 结束所有队伍成员的当前战斗
            foreach (var member in members)
            {
                var existingBattle = GetBattleContextForPlayer(member.Id);
                if (existingBattle != null)
                {
                    // 清理战斗状态
                    foreach (var player in existingBattle.Players)
                    {
                        player.CurrentAction = PlayerActionState.Idle;
                        player.CurrentEnemy = null;
                        player.AttackCooldown = 0;
                    }

                    // 从活跃战斗中移除
                    _activeBattles.Remove(existingBattle.Id);
                }
            }

            // 创建战斗上下文
            var battle = new BattleContext
            {
                BattleType = BattleType.Dungeon,
                Party = party,
                DungeonId = dungeonId,
                State = BattleState.Preparing,
                AllowAutoRevive = dungeon.AllowAutoRevive
            };

            // 添加参与的玩家
            foreach (var member in members.Where(m => !m.IsDead))
            {
                battle.Players.Add(member);
                _characterCombatService.PrepareCharacterForBattle(member);
            }

            // 准备第一波战斗
            _battleFlowService.PrepareDungeonWave(battle, dungeon, 1, _skillSystem);

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

            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active,
                PlayerTargetStrategy = TargetSelectionStrategy.Random,
                EnemyTargetStrategy = TargetSelectionStrategy.Random,
                AllowAutoRevive = true
            };

            // 添加玩家
            if (party != null)
            {
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                foreach (var member in members)
                {
                    battle.Players.Add(member);
                    _characterCombatService.PrepareCharacterForBattle(member);
                }
            }
            else
            {
                battle.Players.Add(character);
                _characterCombatService.PrepareCharacterForBattle(character);
            }

            foreach (var enemyTemplate in enemies)
            {
                var enemy = enemyTemplate.Clone();
                _skillSystem.InitializeEnemySkills(enemy);

                // 添加: 初始化怪物战斗属性
                MonsterTemplates.InitializeCombatAttributes(enemy);

                // 初始化敌人攻击冷却
                enemy.EnemyAttackCooldown = 1.0 / enemy.AttacksPerSecond;

                battle.Enemies.Add(enemy);
            }

            _activeBattles[battle.Id] = battle;
            NotifyStateChanged();
            return true;
        }

        /// <summary>
        /// 添加战斗到活跃列表（供BattleFlowService使用）
        /// </summary>
        internal void AddBattle(BattleContext battle)
        {
            _activeBattles[battle.Id] = battle;
            NotifyStateChanged();
        }

        /// <summary>
        /// 获取所有活跃战斗（供其他服务查询）
        /// </summary>
        public IEnumerable<BattleContext> GetActiveBattles()
        {
            return _activeBattles.Values;
        }

        /// <summary>
        /// 从战斗中移除敌人（供CombatEngine使用）
        /// </summary>
        internal void RemoveEnemyFromBattle(BattleContext battle, Enemy enemy)
        {
            battle.Enemies.Remove(enemy);

            // 更新玩家的目标
            foreach (var playerId in battle.PlayerTargets.Keys.ToList())
            {
                if (battle.PlayerTargets[playerId] == enemy.Name)
                {
                    battle.PlayerTargets.Remove(playerId);
                }
            }
        }

        /// <summary>
        /// 停止战斗
        /// </summary>
        public void StopBattle(BattleContext battle)
        {
            if (battle == null || !_activeBattles.ContainsKey(battle.Id))
                return;

            // 设置战斗状态为已取消
            battle.State = BattleState.Cancelled;

            // 清理所有参与战斗的玩家状态
            foreach (var player in battle.Players)
            {
                player.CurrentAction = PlayerActionState.Idle;
                player.CurrentEnemy = null;
                player.AttackCooldown = 0;
            }

            // 清理队伍状态（如果有）
            if (battle.Party != null)
            {
                battle.Party.CurrentEnemy = null;
            }

            // 从活跃战斗列表中移除
            _activeBattles.Remove(battle.Id);

            // 移除相关的刷新状态
            _battleFlowService.CancelBattleRefresh(battle.Id);

            // 触发状态变更事件
            NotifyStateChanged();

            // 触发战斗取消事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.BattleCancelled, battle.Players.FirstOrDefault());
        }

        /// <summary>
        /// 停止玩家的所有战斗
        /// </summary>
        public void StopPlayerBattles(string playerId)
        {
            var battleToStop = GetBattleContextForPlayer(playerId);
            if (battleToStop != null)
            {
                StopBattle(battleToStop);
            }
        }

        /// <summary>
        /// 停止队伍的所有战斗
        /// </summary>
        public void StopPartyBattles(Guid partyId)
        {
            var battleToStop = GetBattleContextForParty(partyId);
            if (battleToStop != null)
            {
                StopBattle(battleToStop);
            }
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}