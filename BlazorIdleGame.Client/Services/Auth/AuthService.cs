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
        public event Action? AuthenticationStateChanged;

        private const string AUTH_TOKEN_KEY = "auth_token";
        private const string REFRESH_TOKEN_KEY = "refresh_token";
        private const string USER_INFO_KEY = "user_info";
        private const string REMEMBER_ME_KEY = "remember_me";

        private void NotifyAuthenticationStateChanged()
        {
            AuthenticationStateChanged?.Invoke();
        }

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
        /// ����ʹ�ñ���������Զ���¼
        /// </summary>
        public async Task<bool> AutoLoginAsync(string? username = null)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>(AUTH_TOKEN_KEY);
                var rememberMe = await _localStorage.GetItemAsync<bool>(REMEMBER_ME_KEY);

                // ���û�м�ס��¼״̬�����Ʋ����ڣ����Զ���¼
                if (string.IsNullOrEmpty(token) || !rememberMe)
                {
                    _logger.LogInformation("�ޱ�������ƻ�δѡ���ס��¼����Ҫ�û��ֶ���¼");
                    return false;
                }

                // ������֤ͷ
                _communicationService.SetAuthToken(token);

                // ��ȡ�û���Ϣ
                var userInfo = await _communicationService.GetAsync<PlayerInfo>("api/user/me");

                if (userInfo == null)
                {
                    _logger.LogWarning("���ƿ����ѹ��ڣ���Ҫ�û����µ�¼");
                    await _localStorage.RemoveItemAsync(AUTH_TOKEN_KEY);
                    await _localStorage.RemoveItemAsync(REFRESH_TOKEN_KEY);
                    return false;
                }

                _currentPlayer = userInfo;
                IsAuthenticated = true;

                _logger.LogInformation("������Ч�����Զ���¼: {PlayerName}", _currentPlayer.Name);
                PlayerAuthenticated?.Invoke(this, _currentPlayer);
                NotifyAuthenticationStateChanged();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�Զ���¼ʧ��");
                return false;
            }
        }

        /// <summary>
        /// �û���¼
        /// </summary>
        public async Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false)
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
                        Message = response?.Message ?? "��¼ʧ�ܣ���Ч���û���������"
                    };
                }

                // ��������
                await _localStorage.SetItemAsync(AUTH_TOKEN_KEY, response.Data.AccessToken);
                await _localStorage.SetItemAsync(REMEMBER_ME_KEY, rememberMe);

                if (!string.IsNullOrEmpty(response.Data.RefreshToken))
                {
                    await _localStorage.SetItemAsync(REFRESH_TOKEN_KEY, response.Data.RefreshToken);
                }

                // ������֤ͷ
                _communicationService.SetAuthToken(response.Data.AccessToken);

                // ���õ�ǰ���
                _currentPlayer = new PlayerInfo
                {
                    Id = response.Data.UserId,
                    Name = response.Data.Username,
                    Level = 1,
                    Experience = 0,
                    ActiveProfessionId = "adventurer" // Ĭ��ֵ��������ͨ��API��ȡ������Ϣ
                };

                IsAuthenticated = true;

                // �����û���Ϣ
                await _localStorage.SetItemAsync(USER_INFO_KEY, JsonSerializer.Serialize(_currentPlayer));

                _logger.LogInformation("�û���¼�ɹ�: {Username}", username);
                PlayerAuthenticated?.Invoke(this, _currentPlayer);
                NotifyAuthenticationStateChanged();
                return new LoginResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��¼�����г���");
                return new LoginResult
                {
                    Success = false,
                    Message = "��¼ʧ�ܣ�" + ex.Message
                };
            }
        }

        /// <summary>
        /// �û�ע��
        /// </summary>
        public async Task<RegisterResult> RegisterAsync(string username, string password, string email)
        {
            try
            {
                var registerRequest = new RegisterRequest
                {
                    Username = username,
                    Password = password,
                    Email = email
                };

                var response = await _communicationService.PostAsync<RegisterRequest, ApiResponse<AuthenticationResponse>>(
                    "api/auth/register", registerRequest);

                if (response == null || !response.Success)
                {
                    return new RegisterResult
                    {
                        Success = false,
                        Message = response?.Message ?? "ע��ʧ�ܣ�����������"
                    };
                }

                _logger.LogInformation("�û�ע��ɹ�: {Username}", username);

                return new RegisterResult
                {
                    Success = true,
                    Message = "ע��ɹ������¼"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ע������г���");
                return new RegisterResult
                {
                    Success = false,
                    Message = "ע��ʧ�ܣ�" + ex.Message
                };
            }
        }

        /// <summary>
        /// �û��ǳ�
        /// </summary>
        public async Task LogoutAsync()
        {
            // �Ƴ����ش洢������
            await _localStorage.RemoveItemAsync(AUTH_TOKEN_KEY);
            await _localStorage.RemoveItemAsync(REFRESH_TOKEN_KEY);
            await _localStorage.RemoveItemAsync(USER_INFO_KEY);
            await _localStorage.RemoveItemAsync(REMEMBER_ME_KEY);

            // ����֪ͨ�������û��ǳ�
            try
            {
                await _communicationService.PostAsync("api/auth/logout", new { });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "֪ͨ�������ǳ�ʧ�ܣ��������صǳ�����");
            }

            // ������֤״̬
            _communicationService.ClearAuthToken();
            IsAuthenticated = false;
            _currentPlayer = new PlayerInfo();

            _logger.LogInformation("�û��ѵǳ�");
            LoggedOut?.Invoke(this, EventArgs.Empty);
            NotifyAuthenticationStateChanged();
        }
    }
}