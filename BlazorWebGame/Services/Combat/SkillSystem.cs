using BlazorWebGame.Models;
using BlazorWebGame.Models.Skills;
using BlazorWebGame.Models.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Services.Combat
{
    /// <summary>
    /// 技能系统 - 负责技能的管理、触发和效果处理
    /// </summary>
    public class SkillSystem
    {
        /// <summary>
        /// 初始化敌人的技能冷却
        /// </summary>
        public void InitializeEnemySkills(Enemy enemy)
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

                    // 应用技能效果
                    ApplySkillEffect(skill, character, enemy);
                    
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

                    // 应用技能效果
                    ApplyEnemySkillEffect(skill, enemy, character);
                    
                    enemy.SkillCooldowns[skillId] = skill.CooldownRounds;
                }
                else if (cooldown > 0)
                {
                    enemy.SkillCooldowns[skillId] = cooldown - 1;
                }
            }
        }

        /// <summary>
        /// 应用技能效果（玩家对敌人）
        /// </summary>
        private void ApplySkillEffect(Skill skill, Player character, Enemy enemy)
        {
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
                    
                // 可以在这里添加更多技能效果类型
            }
        }

        /// <summary>
        /// 应用技能效果（敌人对玩家）
        /// </summary>
        private void ApplyEnemySkillEffect(Skill skill, Enemy enemy, Player character)
        {
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
                    
                // 可以在这里添加更多技能效果类型
            }
        }

        /// <summary>
        /// 装备技能
        /// </summary>
        public bool EquipSkill(Player character, string skillId, int maxEquippedSkills)
        {
            if (character == null) return false;
            
            var profession = character.SelectedBattleProfession;
            var equipped = character.EquippedSkills[profession];
            var skill = SkillData.GetSkillById(skillId);
            
            if (skill == null || skill.Type == SkillType.Fixed || equipped.Contains(skillId)) 
                return false;
            
            if (equipped.Count(id => SkillData.GetSkillById(id)?.Type != SkillType.Fixed) < maxEquippedSkills)
            {
                equipped.Add(skillId);
                character.SkillCooldowns[skillId] = skill.InitialCooldownRounds;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 卸下技能
        /// </summary>
        public bool UnequipSkill(Player character, string skillId)
        {
            if (character == null) return false;
            
            var skill = SkillData.GetSkillById(skillId);
            if (skill == null || skill.Type == SkillType.Fixed) 
                return false;
            
            if (character.EquippedSkills[character.SelectedBattleProfession].Remove(skillId))
            {
                character.SkillCooldowns.Remove(skillId);
                return true;
            }
            
            return false;
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
        /// 获取技能的剩余冷却时间
        /// </summary>
        public int GetSkillCooldown(Player character, string skillId)
        {
            return character.SkillCooldowns.GetValueOrDefault(skillId, 0);
        }

        /// <summary>
        /// 获取技能的剩余冷却时间（敌人）
        /// </summary>
        public int GetEnemySkillCooldown(Enemy enemy, string skillId)
        {
            return enemy.SkillCooldowns.GetValueOrDefault(skillId, 0);
        }

        /// <summary>
        /// 检查技能是否可以使用
        /// </summary>
        public bool CanUseSkill(Player character, string skillId)
        {
            return GetSkillCooldown(character, skillId) == 0;
        }

        /// <summary>
        /// 检查技能是否可以使用（敌人）
        /// </summary>
        public bool CanEnemyUseSkill(Enemy enemy, string skillId)
        {
            return GetEnemySkillCooldown(enemy, skillId) == 0;
        }

        /// <summary>
        /// 获取玩家当前职业的所有已装备技能
        /// </summary>
        public List<Skill> GetEquippedSkills(Player character)
        {
            var profession = character.SelectedBattleProfession;
            if (!character.EquippedSkills.ContainsKey(profession))
            {
                character.EquippedSkills[profession] = new List<string>();
            }

            return character.EquippedSkills[profession]
                .Select(id => SkillData.GetSkillById(id))
                .Where(s => s != null)
                .ToList()!;
        }

        /// <summary>
        /// 获取玩家可学习的技能列表
        /// </summary>
        public List<Skill> GetLearnableSkills(Player character, BattleProfession profession)
        {
            var playerLevel = character.GetLevel(profession);
            
            return SkillData.AllSkills
                .Where(s => s.RequiredProfession.HasValue && 
                           s.RequiredProfession.Value == profession &&
                           s.RequiredLevel <= playerLevel)
                .ToList();
        }

        /// <summary>
        /// 获取技能信息
        /// </summary>
        public Skill? GetSkillInfo(string skillId)
        {
            return SkillData.GetSkillById(skillId);
        }
    }
}