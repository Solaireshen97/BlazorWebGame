using BlazorIdleGame.Client.Models.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BlazorIdleGame.Client.Services.Auth
{
    public interface IAuthService
    {
        Task<bool> AutoLoginAsync(string? username = null);
        bool IsAuthenticated { get; }
        PlayerInfo CurrentPlayer { get; }
        event EventHandler<PlayerInfo>? PlayerAuthenticated;
    }

    public class AuthService : IAuthService
    {
        private readonly ILogger<AuthService> _logger;
        private PlayerInfo _currentPlayer = new();

        public bool IsAuthenticated { get; private set; } = false;
        public PlayerInfo CurrentPlayer => _currentPlayer;

        public event EventHandler<PlayerInfo>? PlayerAuthenticated;

        public AuthService(ILogger<AuthService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 自动登录（开发测试用）
        /// </summary>
        /// <param name="username">可选用户名，默认为"测试玩家"</param>
        public async Task<bool> AutoLoginAsync(string? username = null)
        {
            try
            {
                // 模拟网络延迟
                await Task.Delay(300);
                
                // 创建测试玩家
                _currentPlayer = new PlayerInfo
                {
                    Id = "test-player-id",
                    Name = username ?? "测试玩家",
                    Level = 1,
                    Experience = 0,
                    ActiveProfessionId = "adventurer"
                };
                
                IsAuthenticated = true;
                _logger.LogInformation("自动登录成功: {PlayerName}", _currentPlayer.Name);
                
                // 触发登录成功事件
                PlayerAuthenticated?.Invoke(this, _currentPlayer);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动登录失败");
                return false;
            }
        }
    }
}