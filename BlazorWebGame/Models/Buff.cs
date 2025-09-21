namespace BlazorWebGame.Models
{
    /// <summary>
    /// 代表一个激活的增益/减益效果
    /// </summary>
    public class Buff
    {
        public string SourceItemId { get; set; } = string.Empty;
        public StatBuffType BuffType { get; set; }
        public int BuffValue { get; set; }
        public double TimeRemainingSeconds { get; set; }

        /// <summary>
        /// 如果此Buff来自食物，记录其类型
        /// </summary>
        public FoodType FoodType { get; set; } = FoodType.None;
    }
}