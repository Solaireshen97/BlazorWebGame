namespace BlazorWebGame.Shared.Enums
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

    /// <summary>
    /// 护甲类型，决定了装备的基本属性和可装备的职业
    /// </summary>
    public enum ArmorType
    {
        None,    // 无类型（如饰品）
        Cloth,   // 布甲（法师等）
        Leather, // 皮甲（猎人等）
        Mail,    // 锁甲（萨满等）
        Plate    // 板甲（战士等）
    }

    /// <summary>
    /// 武器类型，决定了武器的基本属性和攻击方式
    /// </summary>
    public enum WeaponType
    {
        None,         // 无类型（非武器）
        Sword,        // 剑
        Dagger,       // 匕首
        Axe,          // 斧
        Mace,         // 锤
        Staff,        // 法杖
        Wand,         // 魔杖
        Bow,          // 弓
        Crossbow,     // 弩
        Gun,          // 枪
        Shield,       // 盾牌
        TwoHandSword, // 双手剑
        TwoHandAxe,   // 双手斧
        TwoHandMace,  // 双手锤
        Polearm       // 长柄武器
    }
}