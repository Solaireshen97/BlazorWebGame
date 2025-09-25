using System;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorWebGame.Models;
using Microsoft.JSInterop;

namespace BlazorWebGame.Utils
{
    public class GameStorage
    {
        private readonly IJSRuntime _js;
        private const string PlayerDataKey = "mygame-player-data";

        public GameStorage(IJSRuntime js)
        {
            _js = js;
        }

        public Task<Player?> LoadPlayerAsync()
        {
            return GetItemAsync<Player>(PlayerDataKey);
        }

        public Task SavePlayerAsync(Player player)
        {
            return SetItemAsync(PlayerDataKey, player);
        }

        private async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var json = await _js.InvokeAsync<string>("localStorage.getItem", key);
                if (string.IsNullOrEmpty(json))
                {
                    return default;
                }
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

        private async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                var valueToStore = value is string s ? s : JsonSerializer.Serialize(value);
                await _js.InvokeVoidAsync("localStorage.setItem", key, valueToStore);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data to localStorage for key '{key}': {ex.Message}");
            }
        }
    }
}