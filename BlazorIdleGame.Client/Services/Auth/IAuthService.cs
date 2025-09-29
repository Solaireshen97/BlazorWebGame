using BlazorIdleGame.Client.Models.Core;
using System;
using System.Threading.Tasks;

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

        //用于通知任何身份验证状态的变化
        event Action? AuthenticationStateChanged;
    }
}