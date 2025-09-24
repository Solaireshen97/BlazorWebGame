using BlazorWebGame.Events;
using BlazorWebGame.Models;
using BlazorWebGame.Models.Battles;
using BlazorWebGame.Models.Items;
using BlazorWebGame.Models.Monsters;
using System.Linq;

namespace BlazorWebGame.Services.Combat
{
    /// <summary>
    /// 角色战斗服务 - 管理角色在战斗中的状态
    /// </summary>
    public class CharacterCombatService
    {
        private const double RevivalDuration = 2.0;

        /// <summary>
        /// 准备角色进入战斗
        /// </summary>
        public void PrepareCharacterForBattle(Player character)
        {
            if (character == null) return;

            // 重置当前非战斗活动
            ResetPlayerAction(character);

            // 设置为战斗状态
            character.CurrentAction = PlayerActionState.Combat;
            character.AttackCooldown = 0;
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
        /// 处理玩家复活
        /// </summary>
        public void ProcessPlayerRevival(BattleContext battle, double elapsedSeconds)
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
        /// 处理角色死亡
        /// </summary>
        public void HandleCharacterDeath(Player character, BattleContext? battleContext = null)
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

            // 触发角色死亡事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.CharacterDeath, character);

            // 如果提供了战斗上下文，可以进行额外的死亡处理
            if (battleContext != null)
            {
                // 在下一个战斗处理周期中，战斗管理器会检查是否所有玩家都死亡
                // 根据AllowAutoRevive属性决定是否结束战斗
            }
        }

        /// <summary>
        /// 角色复活
        /// </summary>
        public void ReviveCharacter(Player character)
        {
            character.IsDead = false;
            character.Health = character.GetTotalMaxHealth();
            character.RevivalTimeRemaining = 0;

            // 触发角色复活事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.CharacterRevived, character);
        }

        /// <summary>
        /// 设置战斗职业
        /// </summary>
        public void SetBattleProfession(Player character, BattleProfession profession)
        {
            if (character != null)
            {
                character.SelectedBattleProfession = profession;
            }
        }

        /// <summary>
        /// 为角色生成新的敌人实例（用于旧系统兼容）
        /// </summary>
        public void SpawnNewEnemyForCharacter(Player character, Enemy enemyTemplate, SkillSystem skillSystem)
        {
            if (character == null || enemyTemplate == null) return;
            
            // 查找敌人模板
            var originalTemplate = MonsterTemplates.All.FirstOrDefault(m => m.Name == enemyTemplate.Name) ?? enemyTemplate;
            
            // 克隆敌人
            character.CurrentEnemy = originalTemplate.Clone();
            
            // 初始化敌人技能冷却
            if (character.CurrentEnemy != null)
            {
                skillSystem.InitializeEnemySkills(character.CurrentEnemy);
            }
        }

        /// <summary>
        /// 处理旧系统的团队开始战斗
        /// </summary>
        public void HandlePartyStartCombat(Player character, Enemy enemyTemplate, Party party, List<Player> allCharacters, SkillSystem skillSystem)
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
            if (party.CurrentEnemy != null)
            {
                skillSystem.InitializeEnemySkills(party.CurrentEnemy);
            }

            // 设置所有队伍成员的战斗状态
            foreach (var memberId in party.MemberIds)
            {
                var member = allCharacters.FirstOrDefault(c => c.Id == memberId);
                if (member != null && !member.IsDead)
                {
                    PrepareCharacterForBattle(member);
                }
            }
        }

        /// <summary>
        /// 处理旧系统的个人开始战斗
        /// </summary>
        public void HandleSoloStartCombat(Player character, Enemy enemyTemplate, SkillSystem skillSystem)
        {
            // 如果已经在打同一个敌人，则不需要重新开始
            if (character.CurrentAction == PlayerActionState.Combat && character.CurrentEnemy?.Name == enemyTemplate.Name)
                return;

            // 重置当前状态
            ResetPlayerAction(character);
            
            // 设置战斗状态
            character.CurrentAction = PlayerActionState.Combat;
            character.AttackCooldown = 0;
            character.CurrentEnemy = enemyTemplate.Clone();
            
            if (character.CurrentEnemy != null)
            {
                skillSystem.InitializeEnemySkills(character.CurrentEnemy);
            }
        }

        /// <summary>
        /// 检查角色是否可以参与战斗
        /// </summary>
        public bool CanCharacterFight(Player character)
        {
            return character != null && !character.IsDead;
        }

        /// <summary>
        /// 获取角色的战斗状态信息
        /// </summary>
        public CharacterCombatStatus GetCombatStatus(Player character)
        {
            return new CharacterCombatStatus
            {
                IsInCombat = character.CurrentAction == PlayerActionState.Combat,
                IsDead = character.IsDead,
                RevivalTimeRemaining = character.RevivalTimeRemaining,
                CurrentHealth = character.Health,
                MaxHealth = character.GetTotalMaxHealth(),
                HealthPercentage = (double)character.Health / character.GetTotalMaxHealth()
            };
        }

        /// <summary>
        /// 更新角色战斗状态（用于UI显示）
        /// </summary>
        public void UpdateCharacterCombatUI(Player character)
        {
            // 触发UI更新事件
            var gameStateService = ServiceLocator.GetService<GameStateService>();
            gameStateService?.RaiseEvent(GameEventType.CombatStatusChanged, character);
        }

        /// <summary>
        /// 应用战斗开始时的效果
        /// </summary>
        public void ApplyBattleStartEffects(Player character)
        {
            // 重置技能冷却（如果需要）
            // 应用战斗开始时的被动效果
            // 检查并应用装备的战斗开始效果
        }

        /// <summary>
        /// 应用战斗结束时的效果
        /// </summary>
        public void ApplyBattleEndEffects(Player character)
        {
            // 清理战斗相关的临时效果
            // 应用战斗结束时的恢复效果
        }

        /// <summary>
        /// 获取角色复活所需时间
        /// </summary>
        public double GetRevivalDuration(Player character)
        {
            // 基础复活时间
            double duration = RevivalDuration;

            // 可以根据装备、技能等因素调整复活时间
            // 例如：某些装备可能减少复活时间

            return duration;
        }

        /// <summary>
        /// 检查并处理自动消耗品使用
        /// </summary>
        public void ProcessAutoConsumables(Player character)
        {
            var inventoryService = ServiceLocator.GetService<InventoryService>();
            inventoryService?.ProcessAutoConsumables(character);
        }
    }

    /// <summary>
    /// 角色战斗状态信息
    /// </summary>
    public class CharacterCombatStatus
    {
        public bool IsInCombat { get; set; }
        public bool IsDead { get; set; }
        public double RevivalTimeRemaining { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public double HealthPercentage { get; set; }
    }
}