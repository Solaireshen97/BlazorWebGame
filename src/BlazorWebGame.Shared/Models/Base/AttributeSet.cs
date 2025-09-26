namespace BlazorWebGame.Shared.Models.Base
{
    /// <summary>
    /// 角色属性集合，用于储存和操作各种属性值
    /// </summary>
    public class AttributeSet
    {
        public int Strength { get; set; } = 5;     // 力量属性
        public int Agility { get; set; } = 5;      // 敏捷属性  
        public int Intellect { get; set; } = 5;    // 智力属性
        public int Spirit { get; set; } = 5;       // 精神属性
        public int Stamina { get; set; } = 5;      // 耐力属性
        
        // 深拷贝方法，用于创建相同属性值的副本
        public AttributeSet Clone()
        {
            return new AttributeSet
            {
                Strength = this.Strength,
                Agility = this.Agility,
                Intellect = this.Intellect,
                Spirit = this.Spirit,
                Stamina = this.Stamina
            };
        }
        
        // 将另一个属性集合的值添加到当前属性集合
        public void Add(AttributeSet other)
        {
            if (other == null) return;
            
            Strength += other.Strength;
            Agility += other.Agility;
            Intellect += other.Intellect;
            Spirit += other.Spirit;
            Stamina += other.Stamina;
        }
    }
}