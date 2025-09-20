namespace BlazorWebGame.Models
{
    public static class ProfessionExtensions
    {
        public static string ToChineseString(this BattleProfession profession)
        {
            return profession switch
            {
                BattleProfession.Warrior => "战士",
                BattleProfession.Mage => "法师",
                _ => profession.ToString() // 作为备用，如果未来有新职业忘记翻译
            };
        }
    }
}