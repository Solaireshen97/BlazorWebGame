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
        /// �Զ���¼�����������ã�
        /// </summary>
        /// <param name="username">��ѡ�û�����Ĭ��Ϊ"�������"</param>
        public async Task<bool> AutoLoginAsync(string? username = null)
        {
            try
            {
                // ģ�������ӳ�
                await Task.Delay(300);
                
                // �����������
                _currentPlayer = new PlayerInfo
                {
                    Id = "test-player-id",
                    Name = username ?? "�������",
                    Level = 1,
                    Experience = 0,
                    ActiveProfessionId = "adventurer"
                };
                
                IsAuthenticated = true;
                _logger.LogInformation("�Զ���¼�ɹ�: {PlayerName}", _currentPlayer.Name);
                
                // ������¼�ɹ��¼�
                PlayerAuthenticated?.Invoke(this, _currentPlayer);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�Զ���¼ʧ��");
                return false;
            }
        }
    }
}