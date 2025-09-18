using System.Threading.Tasks;
using Microsoft.JSInterop;
using BlazorWebGame.Models; // 引入Models命名空间
using System.Text.Json; // 引入JSON序列化工具

namespace BlazorWebGame.Utils
{
    /// <summary>
    /// 游戏数据存储服务，支持本地存储与后端扩展
    /// </summary>
    public class GameStorage
    {
        private readonly IJSRuntime _js;
        private const string CoinKey = "mygame-coins";
        private const string PlayerDataKey = "mygame-player-data";

        public GameStorage(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>
        /// 加载金币数量（本地存储）
        /// </summary>
        public async Task<int> LoadCoinsAsync()
        {
            var saved = await _js.InvokeAsync<string>("localStorage.getItem", CoinKey);
            if (int.TryParse(saved, out var value))
                return value;
            else
                return 0;
        }

        /// <summary>
        /// 保存金币数量（本地存储）
        /// </summary>
        public async Task SaveCoinsAsync(int coins)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", CoinKey, coins.ToString());
        }

        /// <summary>
        /// 加载玩家数据（本地存储）
        /// </summary>
        public async Task<Player?> LoadPlayerAsync()
        {
            var json = await _js.InvokeAsync<string>("localStorage.getItem", PlayerDataKey);
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<Player>(json);
            }
            return null; // 如果没有数据，返回null
        }

        /// <summary>
        /// 保存玩家数据（本地存储）
        /// </summary>
        public async Task SavePlayerAsync(Player player)
        {
            var json = JsonSerializer.Serialize(player);
            await _js.InvokeVoidAsync("localStorage.setItem", PlayerDataKey, json);
        }

        // 预留服务端接口（后续实现）
        // public async Task<int> LoadCoinsFromServerAsync() { ... }
        // public async Task SaveCoinsToServerAsync(int coins) { ... }
    }
}
