using System;
using System.Threading;
using System.Threading.Tasks;
using BlazorIdleGame.Client.Services.Core;
using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models.Time;
using Microsoft.Extensions.Logging;

namespace BlazorIdleGame.Client.Services.Time
{
    public interface IGameTimeService
    {
        GameTimeSnapshot? CurrentTime { get; }
        DateTime LocalGameTime { get; }
        event EventHandler<GameTimeSnapshot>? TimeUpdated;
        event EventHandler<TimeJumpResult>? TimeJumped;

        Task InitializeAsync();
        Task<TimeJumpResult> RequestTimeJumpAsync(TimeSpan duration, string reason);
        DateTime ConvertToGameTime(DateTime serverTime);
        void Dispose();
    }

    public class GameTimeService : IGameTimeService, IDisposable
    {
        private readonly IGameCommunicationService _communication;
        private readonly ILogger<GameTimeService> _logger;
        private Timer? _syncTimer;
        private GameTimeSnapshot? _currentTime;
        private DateTime _lastSyncTime;
        private readonly object _timeLock = new object();

        public GameTimeSnapshot? CurrentTime => _currentTime;

        public DateTime LocalGameTime
        {
            get
            {
                lock (_timeLock)
                {
                    if (_currentTime == null) return DateTime.UtcNow;

                    var elapsed = DateTime.UtcNow - _lastSyncTime;
                    var scaledElapsed = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds * _currentTime.TimeScale);
                    return _currentTime.GameTime.Add(scaledElapsed);
                }
            }
        }

        public event EventHandler<GameTimeSnapshot>? TimeUpdated;
        public event EventHandler<TimeJumpResult>? TimeJumped;

        public GameTimeService(
            IGameCommunicationService communication,
            ILogger<GameTimeService> logger)
        {
            _communication = communication;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("初始化游戏时间服务");

            // 获取初始时间
            await SyncTimeAsync();

            // 启动定时同步
            _syncTimer = new Timer(
                async _ => await SyncTimeAsync(),
                null,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30)
            );
        }

        private async Task SyncTimeAsync()
        {
            try
            {
                var response = await _communication.GetAsync<ApiResponse<GameTimeSnapshot>>("api/game/time");

                if (response?.Success == true && response.Data != null)
                {
                    lock (_timeLock)
                    {
                        _currentTime = response.Data;
                        _lastSyncTime = DateTime.UtcNow;
                    }

                    TimeUpdated?.Invoke(this, response.Data);
                    _logger.LogDebug("游戏时间已同步: {GameTime}, Scale: {TimeScale}",
                        response.Data.GameTime, response.Data.TimeScale);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "同步游戏时间失败");
            }
        }

        public async Task<TimeJumpResult> RequestTimeJumpAsync(TimeSpan duration, string reason)
        {
            try
            {
                var request = new TimeJumpRequest
                {
                    Duration = duration,
                    Reason = reason
                };

                var response = await _communication.PostAsync<TimeJumpRequest, ApiResponse<TimeJumpResult>>(
                    "api/game/time/jump", request);

                if (response?.Success == true && response.Data != null)
                {
                    // 立即重新同步时间
                    await SyncTimeAsync();

                    TimeJumped?.Invoke(this, response.Data);
                    return response.Data;
                }

                return new TimeJumpResult
                {
                    Success = false,
                    Error = response?.Message ?? "时间跳跃失败"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求时间跳跃失败");
                return new TimeJumpResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public DateTime ConvertToGameTime(DateTime serverTime)
        {
            lock (_timeLock)
            {
                if (_currentTime == null) return serverTime;

                var serverElapsed = serverTime - _currentTime.ServerTime;
                var gameElapsed = TimeSpan.FromMilliseconds(serverElapsed.TotalMilliseconds * _currentTime.TimeScale);
                return _currentTime.GameTime.Add(gameElapsed);
            }
        }

        public void Dispose()
        {
            _syncTimer?.Dispose();
        }
    }
}