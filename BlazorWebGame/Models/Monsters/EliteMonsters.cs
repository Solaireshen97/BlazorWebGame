using System.Collections.Generic;

namespace BlazorWebGame.Models.Monsters
{
    /// <summary>
    /// ��Ӣ����ģ������
    /// </summary>
    public static class EliteMonsters
    {
        public static readonly List<Enemy> Monsters = new()
        {
            new Enemy
            {
                Name = "GoblinElite",
                Description = "����ͨ�粼�ָ�ǿ׳���������ľ�Ӣսʿ��ͨ���ǲ����ָ�ӹ١�",
                Level = 5,
                Type = MonsterType.Elite,
                Race = MonsterRace.Humanoid,
                AttackPower = 15,
                Health = 100,
                MaxHealth = 100,
                AttacksPerSecond = 0.7,
                MinGold = 10,
                MaxGold = 20,
                XpReward = 30,
                LootTable = new Dictionary<string, double>
                {
                    { "EQ_WEP_003", 0.1 }, // ��ħ֮�У��滻��Ч��EQUIP_IRON_SWORD
                    { "MAT_DEMON_ESSENCE", 0.05 }, // 5%
                    { "RECIPE_ITEM_GOBLIN_OMELETTE", 0.2 } // 20% ����ͼֽ
                },
                SkillIds = new List<string> { "SKILL_HEAVY_STRIKE" }
            },
            // ������Ӹ��ྫӢ����...
        };
    }
}