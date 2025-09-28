namespace BlazorIdleGame.Client.Models.Core
{
    public class PlayerInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public long Experience { get; set; }
        public string ActiveProfessionId { get; set; } = "";
    }
}