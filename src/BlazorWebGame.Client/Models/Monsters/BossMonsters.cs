using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// Boss����ģ������
    /// </summary>
    public static class BossMonsters
    {
        public static readonly List<Enemy> Monsters = new()
        {
            // ʾ��Boss����
            new Enemy
            {
                Name = "GoblinChief",
                Description = "�粼�ֲ�������죬�����Ӵ�װ��������ӵ��ǿ���ս��������",
                Level = 10,
                Type = MonsterType.Boss,
                Race = MonsterRace.Humanoid,
                Health = 500,
                MaxHealth = 500,
                AttackPower = 30,
                AttacksPerSecond = 0.5,
                XpReward = 100,
                MinGold = 50,
                MaxGold = 100,
                SkillIds = new List<string> 
                { 
                    "SKILL_HEAVY_STRIKE", 
                    "SKILL_WAR_CRY", 
                    "SKILL_SUMMON_MINIONS" 
                },
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_003", 0.5 },        // 50% ��ħ֮��
                    { "MAT_DEMON_ESSENCE", 1.0 },  // 100% ��ħ����
                    { "RECIPE_ITEM_GOBLIN_OMELETTE", 1.0 }  // 100% ʳ��
                }
            }
            // ������Ӹ���Boss����...
        };
    }
}