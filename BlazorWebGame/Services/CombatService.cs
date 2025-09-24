using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Dungeons;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using BlazorWebGame.Models.Skills;
using BlazorWebGame.Services.Combat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services
{
    /// <summary>
    /// 战斗系统服务门面，协调各个战斗子系统
    /// </summary>
    public class CombatService
    {
        private readonly BattleManager _battleManager;
        private readonly BattleFlowService _battleFlowService;
        private readonly CombatEngine _combatEngine;
        private readonly SkillSystem _skillSystem;
        private readonly LootService _lootService;
        private readonly CharacterCombatService _characterCombatService;

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event Action? OnStateChanged;

        public CombatService(
            InventoryService inventoryService,
            List<Player> allCharacters)
        {
            // 初始化各个子服务
            _skillSystem = new SkillSystem();
            _characterCombatService = new CharacterCombatService();
            _lootService = new LootService(inventoryService, _skillSystem, allCharacters);
            _battleFlowService = new BattleFlowService(allCharacters);

            // 初始化战斗管理器，注入所有依赖
            _battleManager = new BattleManager(
                allCharacters,
                null!, // CombatEngine 会在下面初始化后设置
                _battleFlowService,
                _characterCombatService,
                _skillSystem
            );

            // 初始化战斗引擎
            _combatEngine = new CombatEngine(
                _skillSystem,
                _lootService,
                _characterCombatService,
                _battleManager
            );

            // 通过反射设置 BattleManager 的 CombatEngine（避免循环依赖）
            var field = typeof(BattleManager).GetField("_combatEngine",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_battleManager, _combatEngine);

            // 订阅子服务的状态变更事件
            _battleManager.OnStateChanged += () => OnStateChanged?.Invoke();
        }

        #region 战斗查询接口

        /// <summary>
        /// 获取活跃战斗上下文
        /// </summary>
        public BattleContext? GetBattleContextForPlayer(string playerId)
        {
            return _battleManager.GetBattleContextForPlayer(playerId);
        }

        /// <summary>
        /// 获取活跃战斗上下文
        /// </summary>
        public BattleContext? GetBattleContextForParty(Guid partyId)
        {
            return _battleManager.GetBattleContextForParty(partyId);
        }

        /// <summary>
        /// 检查玩家是否处于战斗刷新状态
        /// </summary>
        public bool IsPlayerInBattleRefresh(string playerId)
        {
            return _battleFlowService.IsPlayerInBattleRefresh(playerId);
        }

        /// <summary>
        /// 获取玩家战斗刷新剩余时间
        /// </summary>
        public double GetPlayerBattleRefreshTime(string playerId)
        {
            return _battleFlowService.GetPlayerBattleRefreshTime(playerId);
        }

        #endregion

        #region 战斗处理

        /// <summary>
        /// 处理所有活跃战斗
        /// </summary>
        public void ProcessAllBattles(double elapsedSeconds)
        {
            _battleManager.ProcessAllBattles(elapsedSeconds);
        }

        /// <summary>
        /// 处理角色的战斗（已废弃 - 请使用新的战斗系统）
        /// </summary>
        [Obsolete("使用新的战斗系统，此方法仅为兼容性保留")]
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
            // 不再执行任何逻辑
            return;
        }

        #endregion

        #region 战斗控制

        /// <summary>
        /// 智能开始战斗
        /// </summary>
        public bool SmartStartBattle(Player character, Enemy enemyTemplate, Party? party = null, bool ignoreRefreshCheck = false)
        {
            return _battleManager.SmartStartBattle(character, enemyTemplate, party, ignoreRefreshCheck);
        }

        /// <summary>
        /// 开始战斗（已废弃 - 请使用 SmartStartBattle）
        /// </summary>
        [Obsolete("使用 SmartStartBattle 方法")]
        public void StartCombat(Player character, Enemy enemyTemplate, Party? party)
        {
            // 直接调用新系统
            SmartStartBattle(character, enemyTemplate, party);
        }


        /// <summary>
        /// 开始副本战斗
        /// </summary>
        public bool StartDungeon(Party party, string dungeonId)
        {
            return _battleManager.StartDungeon(party, dungeonId);
        }

        /// <summary>
        /// 开始多对多普通战斗
        /// </summary>
        public bool StartMultiEnemyBattle(Player character, List<Enemy> enemies, Party? party = null)
        {
            return _battleManager.StartMultiEnemyBattle(character, enemies, party);
        }

        #endregion

        #region 角色状态管理

        /// <summary>
        /// 玩家攻击敌人
        /// </summary>
        public void PlayerAttackEnemy(Player character, Enemy enemy, Party? party)
        {
            _combatEngine.PlayerAttackEnemy(character, enemy, party);
        }

        /// <summary>
        /// 敌人攻击玩家
        /// </summary>
        public void EnemyAttackPlayer(Enemy enemy, Player character)
        {
            _combatEngine.EnemyAttackPlayer(enemy, character);
        }

        /// <summary>
        /// 处理角色死亡
        /// </summary>
        public void HandleCharacterDeath(Player character)
        {
            var battle = _battleManager.GetBattleContextForPlayer(character.Id);
            _characterCombatService.HandleCharacterDeath(character, battle);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// 角色复活
        /// </summary>
        public void ReviveCharacter(Player character)
        {
            _characterCombatService.ReviveCharacter(character);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// 为角色生成新的敌人实例（已废弃）
        /// </summary>
        [Obsolete("新战斗系统会自动处理敌人生成")]
        public void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate)
        {
            // 不再需要
            return;
        }

        #endregion

        #region 技能系统

        /// <summary>
        /// 应用角色技能效果
        /// </summary>
        public void ApplyCharacterSkills(Player character, Enemy enemy)
        {
            _skillSystem.ApplyCharacterSkills(character, enemy);
        }

        /// <summary>
        /// 应用敌人技能效果
        /// </summary>
        public void ApplyEnemySkills(Enemy enemy, Player character)
        {
            _skillSystem.ApplyEnemySkills(enemy, character);
        }

        /// <summary>
        /// 装备技能
        /// </summary>
        public void EquipSkill(Player character, string skillId, int maxEquippedSkills)
        {
            if (_skillSystem.EquipSkill(character, skillId, maxEquippedSkills))
            {
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// 卸下技能
        /// </summary>
        public void UnequipSkill(Player character, string skillId)
        {
            if (_skillSystem.UnequipSkill(character, skillId))
            {
                OnStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// 检查是否有新技能解锁
        /// </summary>
        public void CheckForNewSkillUnlocks(Player character, BattleProfession profession, int level, bool checkAllLevels = false)
        {
            _skillSystem.CheckForNewSkillUnlocks(character, profession, level, checkAllLevels);
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// 重置玩家技能冷却
        /// </summary>
        public void ResetPlayerSkillCooldowns(Player character)
        {
            _skillSystem.ResetPlayerSkillCooldowns(character);
        }

        /// <summary>
        /// 设置战斗职业
        /// </summary>
        public void SetBattleProfession(Player character, BattleProfession profession)
        {
            _characterCombatService.SetBattleProfession(character, profession);
            OnStateChanged?.Invoke();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置所有角色列表（用于更新内部状态）
        /// </summary>
        public void SetAllCharacters(List<Player> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));

            // 更新所有子服务的角色列表
            var allCharactersField = typeof(BattleFlowService).GetField("_allCharacters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            allCharactersField?.SetValue(_battleFlowService, characters);

            var lootAllCharactersField = typeof(LootService).GetField("_allCharacters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lootAllCharactersField?.SetValue(_lootService, characters);

            var battleAllCharactersField = typeof(BattleManager).GetField("_allCharacters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            battleAllCharactersField?.SetValue(_battleManager, characters);
        }

        #endregion
    }
}