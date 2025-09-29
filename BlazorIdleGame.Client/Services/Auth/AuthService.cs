using BlazorIdleGame.Client.Models.Core;
using BlazorIdleGame.Client.Services.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorWebGame.Shared.DTOs;

namespace BlazorIdleGame.Client.Services.Auth
{
    public interface IAuthService
    {
        Task<bool> AutoLoginAsync(string? username = null);
        Task<LoginResult> LoginAsync(string username, string password);
        Task<RegisterResult> RegisterAsync(string username, string password, string confirmPassword);
        Task LogoutAsync();
        bool IsAuthenticated { get; }
        PlayerInfo CurrentPlayer { get; }
        event EventHandler<PlayerInfo>? PlayerAuthenticated;
        event EventHandler? LoggedOut;
    }

    public class AuthService : IAuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly IGameCommunicationService _communicationService;
        private readonly LocalStorageService _localStorage;
        private PlayerInfo _currentPlayer = new();

        public bool IsAuthenticated { get; private set; } = false;
        public PlayerInfo CurrentPlayer => _currentPlayer;

        public event EventHandler<PlayerInfo>? PlayerAuthenticated;
        public event EventHandler? LoggedOut;

        private const string AUTH_TOKEN_KEY = "auth_token";
        private const string REFRESH_TOKEN_KEY = "refresh_token";
        private const string USER_INFO_KEY = "user_info";

        public AuthService(
            ILogger<AuthService> logger,
            IGameCommunicationService communicationService,
            LocalStorageService localStorage)
        {
            _logger = logger;
            _communicationService = communicationService;
            _localStorage = localStorage;
        }

        /// <summary>
        /// 尝试使用保存的令牌自动登录
        /// </summary>
        public async Task<bool> AutoLoginAsync(string? username = null)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>(AUTH_TOKEN_KEY);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogInformation("无保存的令牌，需要用户手动登录");
                    return false;
                }

                // 设置认证头
                _communicationService.SetAuthToken(token);

                // 获取用户信息
                var userInfo = await _communicationService.GetAsync<PlayerInfo>("api/user/me");

                if (userInfo == null)
                {
                    _logger.LogWarning("令牌可能已过期，需要用户重新登录");
                    await _localStorage.RemoveItemAsync(AUTH_TOKEN_KEY);
                    await _localStorage.RemoveItemAsync(REFRESH_TOKEN_KEY);
                    return false;
                }

                _currentPlayer = userInfo;
                IsAuthenticated = true;

                _logger.LogInformation("令牌有效，已自动登录: {PlayerName}", _currentPlayer.Name);
                PlayerAuthenticated?.Invoke(this, _currentPlayer);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动登录失败");
                return false;
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                var response = await _communicationService.PostAsync<LoginRequest, ApiResponse<AuthenticationResponse>>(
                    "api/auth/login", loginRequest);

                if (response == null || !response.Success || response.Data == null)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = response?.Message ?? "登录失败：无效的用户名或密码"
                    };
                }

                // 保存令牌
                await _localStorage.SetItemAsync(AUTH_TOKEN_KEY, response.Data.AccessToken);

                if (!string.IsNullOrEmpty(response.Data.RefreshToken))
                {
                    await _localStorage.SetItemAsync(REFRESH_TOKEN_KEY, response.Data.RefreshToken);
                }

                // 设置认证头
                _communicationService.SetAuthToken(response.Data.AccessToken);

                // 设置当前玩家
                _currentPlayer = new PlayerInfo
                {
                    Id = response.Data.UserId,
                    Name = response.Data.Username,
                    Level = 1,
                    Experience = 0,
                    ActiveProfessionId = "adventurer" // 默认值，后续可通过API获取完整信息
                };

                IsAuthenticated = true;

                // 保存用户信息
                await _localStorage.SetItemAsync(USER_INFO_KEY, JsonSerializer.Serialize(_currentPlayer));

                _logger.LogInformation("用户登录成功: {Username}", username);
                PlayerAuthenticated?.Invoke(this, _currentPlayer);

                return new LoginResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录过程中出错");
                return new LoginResult
                {
                    Success = false,
                    Message = "登录失败：" + ex.Message
                };
            }
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        public async Task<RegisterResult> RegisterAsync(string username, string password, string confirmPassword)
        {
            try
            {
                if (password != confirmPassword)
                {
                    return new RegisterResult
                    {
                        Success = false,
                        Message = "两次输入的密码不一致"
                    };
                }

                var registerRequest = new RegisterRequest
                {
                    Username = username,
                    Password = password
                };

                var response = await _communicationService.PostAsync<RegisterRequest, ApiResponse<AuthenticationResponse>>(
                    "api/auth/register", registerRequest);

                if (response == null || !response.Success)
                {
                    return new RegisterResult
                    {
                        Success = false,
                        Message = response?.Message ?? "注册失败：服务器错误"
                    };
                }

                _logger.LogInformation("用户注册成功: {Username}", username);

                return new RegisterResult
                {
                    Success = true,
                    Message = "注册成功，请登录"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册过程中出错");
                return new RegisterResult
                {
                    Success = false,
                    Message = "注册失败：" + ex.Message
                };
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task LogoutAsync()
        {
            // 移除本地存储的令牌
            await _localStorage.RemoveItemAsync(AUTH_TOKEN_KEY);
            await _localStorage.RemoveItemAsync(REFRESH_TOKEN_KEY);
            await _localStorage.RemoveItemAsync(USER_INFO_KEY);

            // 重置认证状态
            _communicationService.ClearAuthToken();
            IsAuthenticated = false;
            _currentPlayer = new PlayerInfo();

            _logger.LogInformation("用户已登出");
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }



    public class RegisterResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}