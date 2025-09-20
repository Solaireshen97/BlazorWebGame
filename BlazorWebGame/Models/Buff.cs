namespace BlazorWebGame.Models
{
    /// <summary>
    /// 代表一个激活的增益/减益效果
    /// </summary>
    public class Buff
    {
        /// <summary>
        /// 效果来源的物品ID
        /// </summary>
        public string SourceItemId { get; set; } = string.Empty;
        public StatBuffType BuffType { get; set; }
        public int BuffValue { get; set; }
        public double TimeRemainingSeconds { get; set; }
    }
}