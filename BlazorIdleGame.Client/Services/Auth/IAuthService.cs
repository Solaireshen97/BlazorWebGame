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
        /// �û���¼
        /// </summary>
        /// <param name="username">�û���</param>
        /// <param name="password">����</param>
        /// <param name="rememberMe">�Ƿ��ס��¼״̬</param>
        /// <returns>��¼���</returns>
        Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false);

        /// <summary>
        /// �û�ע��
        /// </summary>
        /// <param name="username">�û���</param>
        /// <param name="password">����</param>
        /// <param name="email">��������</param>
        /// <returns>ע����</returns>
        Task<RegisterResult> RegisterAsync(string username, string password, string email);

        /// <summary>
        /// �û��ǳ�
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// ����û��Ƿ�����֤
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// ��ǰ��¼�������Ϣ
        /// </summary>
        PlayerInfo CurrentPlayer { get; }

        /// <summary>
        /// �����֤�ɹ��¼�
        /// </summary>
        event EventHandler<PlayerInfo>? PlayerAuthenticated;

        /// <summary>
        /// �û��ǳ��¼�
        /// </summary>
        event EventHandler? LoggedOut;

        /// <summary>
        /// ����֪ͨ�κ������֤״̬�ı仯
        /// </summary>
        event Action? AuthenticationStateChanged;
    }
}