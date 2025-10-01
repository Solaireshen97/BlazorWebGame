using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorWebGame.Shared.Mappers
{
    /// <summary>
    /// 技能模型映射工具类 - 在领域模型和DTO之间转换
    /// </summary>
    public static class SkillMapper
    {
        /// <summary>
        /// 将技能管理器转换为DTO集合
        /// </summary>
        public static Dictionary<string, LearnedSkillDto> ToDto(this CharacterSkillManager skillManager)
        {
            var result = new Dictionary<string, LearnedSkillDto>();
            
            foreach (var skill in skillManager.LearnedSkills)
            {
                result[skill.Key] = new LearnedSkillDto
                {
                    SkillId = skill.Value.SkillId,
                    CurrentLevel = 1, // 技能等级默认为1，可以根据需要修改
                    TimesUsed = skill.Value.UsageCount,
                    LearnedAt = skill.Value.LearnedAt
                };
            }
            
            return result;
        }
        
        /// <summary>
        /// 将装备技能转换为DTO映射
        /// </summary>
        public static Dictionary<string, List<string>> EquippedSkillsToDto(this CharacterSkillManager skillManager)
        {
            return skillManager.EquippedSkills.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            );
        }
        
        /// <summary>
        /// 从DTO恢复技能管理器状态
        /// </summary>
        public static void RestoreFromDto(this CharacterSkillManager skillManager, 
            Dictionary<string, LearnedSkillDto> learnedSkills,
            Dictionary<string, List<string>> equippedSkills)
        {
            if (learnedSkills == null || equippedSkills == null)
                return;
                
            // 获取私有字段
            var learnedSkillsField = GetPrivateField<CharacterSkillManager, Dictionary<string, LearnedSkill>>(skillManager, "_learnedSkills");
            var equippedSkillsField = GetPrivateField<CharacterSkillManager, Dictionary<string, List<string>>>(skillManager, "_equippedSkills");
            
            // 清空现有数据
            learnedSkillsField?.Clear();
            equippedSkillsField?.Clear();
            
            // 恢复已学习技能
            foreach (var skill in learnedSkills)
            {
                var learnedSkill = new LearnedSkill(skill.Value.SkillId);
                
                // 使用反射设置私有属性
                SetPrivateProperty(learnedSkill, "UsageCount", skill.Value.TimesUsed);
                SetPrivateProperty(learnedSkill, "LearnedAt", skill.Value.LearnedAt);
                if (skill.Value.TimesUsed > 0)
                    SetPrivateProperty(learnedSkill, "LastUsedAt", DateTime.UtcNow);
                
                learnedSkillsField?.Add(skill.Key, learnedSkill);
            }
            
            // 恢复已装备技能
            foreach (var category in equippedSkills)
            {
                equippedSkillsField?[category.Key] = category.Value.ToList();
            }
        }
        
        /// <summary>
        /// 辅助方法：获取私有字段
        /// </summary>
        private static T GetPrivateField<TObject, T>(TObject obj, string fieldName)
        {
            var field = typeof(TObject).GetField(fieldName, 
                BindingFlags.Instance | BindingFlags.NonPublic);
                
            return field != null ? (T)field.GetValue(obj) : default;
        }
        
        /// <summary>
        /// 辅助方法：设置私有属性
        /// </summary>
        private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
        {
            var property = obj.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                    
                field?.SetValue(obj, value);
            }
        }
    }
}