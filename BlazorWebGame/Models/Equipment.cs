using System.Text;
using BlazorWebGame.Models;

namespace BlazorWebGame.Models
{
    /// <summary>
    /// 定义所有可用的装备槽位，灵感来源于经典MMORPG
    /// </summary>
    public enum EquipmentSlot
    {
        // --- 核心护甲 (左侧) ---
        Head,     // 头部
        Neck,     // 颈部
        Shoulder, // 肩部
        Back,     // 背部 (披风)
        Chest,    // 胸部
        Wrist,    // 手腕 (护腕)

        // --- 核心护甲 (右侧) ---
        Hands,    // 手部 (手套)
        Waist,    // 腰部 (腰带)
        Legs,     // 腿部
        Feet,     // 脚部

        // --- 饰品和戒指 (右侧) ---
        Finger1,  // 第一个戒指
        Finger2,  // 第二个戒指
        Trinket1, // 第一个饰品
        Trinket2, // 第二个饰品

        // --- 武器 (底部) ---
        MainHand, // 主手武器
        OffHand   // 副手 (可以是盾牌或副手武器)
    }

    public class Equipment : Item
    {
        public EquipmentSlot Slot { get; set; }
        public int AttackBonus { get; set; } = 0;
        public int HealthBonus { get; set; } = 0;
        public double AttackSpeedBonus { get; set; } = 0;
        public double GatheringSpeedBonus { get; set; } = 0;
        public double ExtraLootChanceBonus { get; set; } = 0;
        public int AccuracyBonus { get; set; } = 0;
        
        // 新增：装备的属性加成
        public AttributeSet AttributeBonuses { get; set; } = new AttributeSet();

        public Equipment()
        {
            Type = ItemType.Equipment;
            IsStackable = false;
        }
        
        // 获取装备属性描述
        public override string GetStatsDescription()
        {
            var sb = new StringBuilder();
            
            // 添加现有属性描述
            if (AttackBonus > 0)
                sb.AppendLine($"+{AttackBonus} 攻击力");
                
            if (HealthBonus > 0)
                sb.AppendLine($"+{HealthBonus} 生命值");
                
            if (AttackSpeedBonus > 0)
                sb.AppendLine($"+{AttackSpeedBonus:P0} 攻击速度");
                
            if (GatheringSpeedBonus > 0)
                sb.AppendLine($"+{GatheringSpeedBonus:P0} 采集速度");
                
            if (ExtraLootChanceBonus > 0)
                sb.AppendLine($"+{ExtraLootChanceBonus:P0} 额外战利品几率");
                
            if (AccuracyBonus > 0)
                sb.AppendLine($"+{AccuracyBonus} 命中");
            
            // 添加属性加成描述
            if (AttributeBonuses.Strength > 0)
                sb.AppendLine($"+{AttributeBonuses.Strength} 力量");
                
            if (AttributeBonuses.Agility > 0)
                sb.AppendLine($"+{AttributeBonuses.Agility} 敏捷");
                
            if (AttributeBonuses.Intellect > 0)
                sb.AppendLine($"+{AttributeBonuses.Intellect} 智力");
                
            if (AttributeBonuses.Spirit > 0)
                sb.AppendLine($"+{AttributeBonuses.Spirit} 精神");
                
            if (AttributeBonuses.Stamina > 0)
                sb.AppendLine($"+{AttributeBonuses.Stamina} 耐力");
            
            return sb.ToString();
        }
    }
}