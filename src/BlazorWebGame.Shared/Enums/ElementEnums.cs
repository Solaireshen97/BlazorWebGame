namespace BlazorWebGame.Shared.Enums
{
    /// <summary>
    /// 元素类型枚举
    /// </summary>
    public enum ElementType
    {
        None,
        Fire,
        Ice,
        Lightning,
        Nature,
        Shadow,
        Holy
    }

    /// <summary>
    /// 角色属性类型枚举
    /// </summary>
    public enum AttributeType
    {
        Strength,    // 力量
        Agility,     // 敏捷
        Intellect,   // 智力
        Spirit,      // 精神
        Stamina      // 耐力（影响生命值）
    }
}