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

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        public BattleManager(
            List<Player> allCharacters,
            CombatEngine combatEngine,
            BattleFlowService battleFlowService,
            CharacterCombatService characterCombatService,
            SkillSystem skillSystem)
        {
            _allCharacters = allCharacters;
            _combatEngine = combatEngine;
            _battleFlowService = battleFlowService;
            _characterCombatService = characterCombatService;
            _skillSystem = skillSystem;
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
            if (battle.State != BattleState.Active)
                return;

            // 处理玩家复活倒计时
            _characterCombatService.ProcessPlayerRevival(battle, elapsedSeconds);

            // 处理玩家攻击
            foreach (var player in battle.Players.Where(p => !p.IsDead))
            {
                _combatEngine.ProcessPlayerAttack(battle, player, elapsedSeconds);
            }

            // 处理敌人攻击
            foreach (var enemy in battle.Enemies.ToList())
            {
                _combatEngine.ProcessEnemyAttack(battle, enemy, elapsedSeconds);
            }

            // 检查战斗状态
            CheckBattleStatus(battle);
        }

        /// <summary>
        /// 检查战斗状态
        /// </summary>
        private void CheckBattleStatus(BattleContext battle)
        {
            if (battle.IsCompleted)
            {
                battle.State = BattleState.Completed;

                // 触发相应的战斗结束事件
                var gameStateService = ServiceLocator.GetService<GameStateService>();
                if (battle.IsVictory)
                {
                    gameStateService?.RaiseEvent(GameEventType.BattleCompleted);
                }
                else
                {
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

            // 获取当前战斗上下文
            var existingBattle = GetBattleContextForPlayer(character.Id);
            if (existingBattle != null)
            {
                // 如果已经在战斗同类型的敌人，不做任何处理
                if (existingBattle.Enemies.Any(e => e.Name == enemyTemplate.Name))
                    return true;

                // 否则结束当前战斗，开始新战斗
                _activeBattles.Remove(existingBattle.Id);
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

            // 所有战斗都使用BattleContext系统
            var battle = new BattleContext
            {
                BattleType = party != null ? BattleType.Party : BattleType.Solo,
                Party = party,
                State = BattleState.Active,
                PlayerTargetStrategy = TargetSelectionStrategy.LowestHealth,
                EnemyTargetStrategy = TargetSelectionStrategy.Random,
                AllowAutoRevive = true
            };

            // 添加玩家
            if (party != null)
            {
                // 团队战斗：添加所有活着的团队成员
                var members = _allCharacters.Where(c => party.MemberIds.Contains(c.Id) && !c.IsDead).ToList();
                foreach (var member in members)
                {
                    battle.Players.Add(member);
                    _characterCombatService.PrepareCharacterForBattle(member);
                }

                // 根据团队规模生成适量敌人
                var enemyCount = _battleFlowService.DetermineEnemyCount(members.Count);
                for (int i = 0; i < enemyCount; i++)
                {
                    var enemy = enemyTemplate.Clone();
                    _skillSystem.InitializeEnemySkills(enemy);
                    battle.Enemies.Add(enemy);
                }
            }
            else
            {
                // 单人战斗：1v1
                battle.Players.Add(character);
                _characterCombatService.PrepareCharacterForBattle(character);

                var enemy = enemyTemplate.Clone();
                _skillSystem.InitializeEnemySkills(enemy);
                battle.Enemies.Add(enemy);
            }

            // 清理旧系统的状态（确保完全移除）
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
                PlayerTargetStrategy = TargetSelectionStrategy.LowestHealth,
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

            // 添加敌人
            foreach (var enemyTemplate in enemies)
            {
                var enemy = enemyTemplate.Clone();
                _skillSystem.InitializeEnemySkills(enemy);
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
        /// 触发状态变更事件
        /// </summary>
        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}