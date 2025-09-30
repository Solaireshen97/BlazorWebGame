using BlazorIdleGame.Client.Models.Core;
using BlazorWebGame.Shared.DTOs;
using System;
using System.Threading.Tasks;

namespace BlazorIdleGame.Client.Services.Auth
{
    public interface IAuthService
    {
        Task<bool> AutoLoginAsync(string? username = null);

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="rememberMe">是否记住登录状态</param>
        /// <returns>登录结果</returns>
        Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false);

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="email">电子邮箱</param>
        /// <returns>注册结果</returns>
        Task<RegisterResult> RegisterAsync(string username, string password, string email);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// 检查用户是否已认证
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// 当前登录的玩家信息
        /// </summary>
        PlayerInfo CurrentPlayer { get; }

        /// <summary>
        /// 玩家认证成功事件
        /// </summary>
        event EventHandler<PlayerInfo>? PlayerAuthenticated;

        /// <summary>
        /// 用户登出事件
        /// </summary>
        event EventHandler? LoggedOut;

        /// <summary>
        /// 用于通知任何身份验证状态的变化
        /// </summary>
        event Action? AuthenticationStateChanged;
    }
}