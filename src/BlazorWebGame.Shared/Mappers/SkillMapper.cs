using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BlazorWebGame.Shared.Mappers
{
    /// <summary>
    /// ����ģ��ӳ�乤���� - ������ģ�ͺ�DTO֮��ת��
    /// </summary>
    public static class SkillMapper
    {
        /// <summary>
        /// �����ܹ�����ת��ΪDTO����
        /// </summary>
        public static Dictionary<string, LearnedSkillDto> ToDto(this CharacterSkillManager skillManager)
        {
            var result = new Dictionary<string, LearnedSkillDto>();
            
            foreach (var skill in skillManager.LearnedSkills)
            {
                result[skill.Key] = new LearnedSkillDto
                {
                    SkillId = skill.Value.SkillId,
                    CurrentLevel = 1, // ���ܵȼ�Ĭ��Ϊ1�����Ը�����Ҫ�޸�
                    TimesUsed = skill.Value.UsageCount,
                    LearnedAt = skill.Value.LearnedAt
                };
            }
            
            return result;
        }
        
        /// <summary>
        /// ��װ������ת��ΪDTOӳ��
        /// </summary>
        public static Dictionary<string, List<string>> EquippedSkillsToDto(this CharacterSkillManager skillManager)
        {
            return skillManager.EquippedSkills.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            );
        }
        
        /// <summary>
        /// ��DTO�ָ����ܹ�����״̬
        /// </summary>
        public static void RestoreFromDto(this CharacterSkillManager skillManager, 
            Dictionary<string, LearnedSkillDto> learnedSkills,
            Dictionary<string, List<string>> equippedSkills)
        {
            if (learnedSkills == null || equippedSkills == null)
                return;
                
            // ��ȡ˽���ֶ�
            var learnedSkillsField = GetPrivateField<CharacterSkillManager, Dictionary<string, LearnedSkill>>(skillManager, "_learnedSkills");
            var equippedSkillsField = GetPrivateField<CharacterSkillManager, Dictionary<string, List<string>>>(skillManager, "_equippedSkills");
            
            // �����������
            learnedSkillsField?.Clear();
            equippedSkillsField?.Clear();
            
            // �ָ���ѧϰ����
            foreach (var skill in learnedSkills)
            {
                var learnedSkill = new LearnedSkill(skill.Value.SkillId);
                
                // ʹ�÷�������˽������
                SetPrivateProperty(learnedSkill, "UsageCount", skill.Value.TimesUsed);
                SetPrivateProperty(learnedSkill, "LearnedAt", skill.Value.LearnedAt);
                if (skill.Value.TimesUsed > 0)
                    SetPrivateProperty(learnedSkill, "LastUsedAt", DateTime.UtcNow);
                
                learnedSkillsField?.Add(skill.Key, learnedSkill);
            }
            
            // �ָ���װ������
            foreach (var category in equippedSkills)
            {
                equippedSkillsField?[category.Key] = category.Value.ToList();
            }
        }
        
        /// <summary>
        /// ������������ȡ˽���ֶ�
        /// </summary>
        private static T GetPrivateField<TObject, T>(TObject obj, string fieldName)
        {
            var field = typeof(TObject).GetField(fieldName, 
                BindingFlags.Instance | BindingFlags.NonPublic);
                
            return field != null ? (T)field.GetValue(obj) : default;
        }
        
        /// <summary>
        /// ��������������˽������
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