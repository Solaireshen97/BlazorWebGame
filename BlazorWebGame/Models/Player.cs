using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Models
{
    public class Player
    {
        public string Name { get; set; } = "英雄";
        public int Gold { get; set; } = 0;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int BaseAttackPower { get; set; } = 10;
        public double AttacksPerSecond { get; set; } = 1.0;

        public BattleProfession SelectedBattleProfession { get; set; } = BattleProfession.Warrior;
        public Dictionary<BattleProfession, int> BattleProfessionXP { get; set; } = new();
        public Dictionary<GatheringProfession, int> GatheringProfessionXP { get; set; } = new();
        public Dictionary<ProductionProfession, int> ProductionProfessionXP { get; set; } = new();

        // --- 技能系统字段 ---

        /// <summary>
        /// 玩家已经解锁的所有共享技能ID
        /// </summary>
        public HashSet<string> LearnedSharedSkills { get; set; } = new();

        /// <summary>
        /// 每个职业装备的技能ID列表。Key是职业，Value是装备的技能ID列表
        /// </summary>
        public Dictionary<BattleProfession, List<string>> EquippedSkills { get; set; } = new();

        public Player()
        {
            // 初始化职业
            foreach (var profession in (BattleProfession[])Enum.GetValues(typeof(BattleProfession)))
            {
                BattleProfessionXP.TryAdd(profession, 0);
                EquippedSkills.TryAdd(profession, new List<string>());
            }
            GatheringProfessionXP.TryAdd(GatheringProfession.Miner, 0);
            ProductionProfessionXP.TryAdd(ProductionProfession.Tailor, 0);
        }

        public void AddBattleXP(BattleProfession profession, int amount)
        {
            if (BattleProfessionXP.ContainsKey(profession))
            {
                BattleProfessionXP[profession] += amount;
            }
        }

        /// <summary>
        /// 获取指定战斗职业的等级
        /// </summary>
        public int GetLevel(BattleProfession profession)
        {
            return BattleProfessionXP.TryGetValue(profession, out var xp) ? 1 + (xp / 100) : 1;
        }

        /// <summary>
        /// 根据经验值计算等级（通常用于UI显示或内部计算）
        /// </summary>
        public int GetLevel(int xp) => 1 + (xp / 100);

        public int GetTotalAttackPower()
        {
            // 未来可以根据技能加成
            return BaseAttackPower;
        }
    }
}