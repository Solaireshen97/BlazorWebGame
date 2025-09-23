using BlazorWebGame.Events;
using BlazorWebGame.Models;
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
        /// 处理角色的战斗
        /// </summary>
        public void ProcessCombat(Player character, double elapsedSeconds, Party? party)
        {
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
                
                // 敌人被击败
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
    }
}