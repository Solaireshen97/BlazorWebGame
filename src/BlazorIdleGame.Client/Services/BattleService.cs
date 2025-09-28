using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;
using BlazorIdleGame.Client.Models;
using Microsoft.Extensions.Logging;

namespace BlazorIdleGame.Client.Services
{
    public interface IBattleService
    {
        BattleState? CurrentBattle { get; }
        
        event EventHandler<BattleState>? BattleUpdated;
        event EventHandler<BattleLog>? BattleLogAdded;
        event EventHandler<BattleRewards>? BattleCompleted;
        
        Task InitializeAsync();
        Task<bool> StartBattleAsync(string enemyId, bool isPartyBattle = false);
        Task<bool> FleeBattleAsync();
        Task<bool> UseSkillAsync(string skillId, string targetId);
        void UpdateBattleState(BattleState battle);
        void Dispose();
    }
    
    public class BattleService : IBattleService, IDisposable
    {
        private readonly HttpClient _http;
        private readonly ILogger<BattleService> _logger;
        private BattleState? _currentBattle;
        
        public BattleState? CurrentBattle => _currentBattle;
        
        public event EventHandler<BattleState>? BattleUpdated;
        public event EventHandler<BattleLog>? BattleLogAdded;
        public event EventHandler<BattleRewards>? BattleCompleted;
        
        public BattleService(HttpClient http, ILogger<BattleService> logger)
        {
            _http = http;
            _logger = logger;
        }
        
        public Task InitializeAsync()
        {
            // 初始化战斗服务
            return Task.CompletedTask;
        }
        
        public async Task<bool> StartBattleAsync(string enemyId, bool isPartyBattle = false)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/battle/start", new
                {
                    EnemyId = enemyId,
                    IsPartyBattle = isPartyBattle
                });
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<BattleState>>();
                    if (result?.Success == true && result.Data != null)
                    {
                        _currentBattle = result.Data;
                        BattleUpdated?.Invoke(this, _currentBattle);
                        return true;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始战斗失败");
                return false;
            }
        }
        
        public async Task<bool> FleeBattleAsync()
        {
            if (_currentBattle == null) return false;
            
            try
            {
                var response = await _http.PostAsync($"api/battle/flee/{_currentBattle.BattleId}", null);
                
                if (response.IsSuccessStatusCode)
                {
                    _currentBattle = null;
                    BattleUpdated?.Invoke(this, null!);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "逃离战斗失败");
                return false;
            }
        }
        
        public async Task<bool> UseSkillAsync(string skillId, string targetId)
        {
            if (_currentBattle == null) return false;
            
            try
            {
                var response = await _http.PostAsJsonAsync("api/battle/skill", new
                {
                    BattleId = _currentBattle.BattleId,
                    SkillId = skillId,
                    TargetId = targetId
                });
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "使用技能失败");
                return false;
            }
        }
        
        public void UpdateBattleState(BattleState battle)
        {
            var previousBattle = _currentBattle;
            _currentBattle = battle;
            
            // 检测新的战斗日志
            if (previousBattle != null && battle.Logs.Count > previousBattle.Logs.Count)
            {
                var newLogs = battle.Logs.Skip(previousBattle.Logs.Count);
                foreach (var log in newLogs)
                {
                    BattleLogAdded?.Invoke(this, log);
                }
            }
            
            // 检测战斗结束
            if (battle.Status == BattleStatus.Victory || battle.Status == BattleStatus.Defeat)
            {
                if (battle.Rewards != null)
                {
                    BattleCompleted?.Invoke(this, battle.Rewards);
                }
            }
            
            BattleUpdated?.Invoke(this, battle);
        }
        
        public void Dispose()
        {
            // 清理资源
        }
    }
}