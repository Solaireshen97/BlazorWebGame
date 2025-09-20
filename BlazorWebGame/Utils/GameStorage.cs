using System;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorWebGame.Models;
using Microsoft.JSInterop;

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
        /// 从本地存储加载金币数量
        /// </summary>
        public async Task<int> LoadCoinsAsync()
        {
            var saved = await GetItemAsync<string>(CoinKey);
            return int.TryParse(saved, out var value) ? value : 0;
        }

        /// <summary>
        /// 将金币数量保存到本地存储
        /// </summary>
        public Task SaveCoinsAsync(int coins)
        {
            return SetItemAsync(CoinKey, coins.ToString());
        }

        /// <summary>
        /// 从本地存储加载玩家数据
        /// </summary>
        public Task<Player?> LoadPlayerAsync()
        {
            return GetItemAsync<Player>(PlayerDataKey);
        }

        /// <summary>
        /// 将玩家数据保存到本地存储
        /// </summary>
        public Task SavePlayerAsync(Player player)
        {
            return SetItemAsync(PlayerDataKey, player);
        }

        // 泛型方法，用于从 localStorage 获取和反序列化任何 JSON 数据
        private async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var json = await _js.InvokeAsync<string>("localStorage.getItem", key);
                if (string.IsNullOrEmpty(json))
                {
                    return default;
                }
                // 如果 T 是 string 类型，直接返回 json 字符串，避免不必要的反序列化
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)json;
                }
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data from localStorage for key '{key}': {ex.Message}");
                return default;
            }
        }

        // 泛型方法，用于序列化和保存任何数据到 localStorage
        private async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                // 如果值是字符串，直接存储；否则，序列化为 JSON
                var valueToStore = value is string s ? s : JsonSerializer.Serialize(value);
                await _js.InvokeVoidAsync("localStorage.setItem", key, valueToStore);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data to localStorage for key '{key}': {ex.Message}");
            }
        }

        // 预留服务端接口（后续实现）
        // public async Task<int> LoadCoinsFromServerAsync() { ... }
        // public async Task SaveCoinsToServerAsync(int coins) { ... }
    }
}
