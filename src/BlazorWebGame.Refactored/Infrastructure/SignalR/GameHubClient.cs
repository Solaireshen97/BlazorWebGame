using Microsoft.AspNetCore.SignalR.Client;
using BlazorWebGame.Refactored.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BlazorWebGame.Refactored.Infrastructure.SignalR;

public class GameHubClient : ISignalRService, IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly ILogger<GameHubClient> _logger;
    private readonly string _hubUrl;
    private bool _isDisposed;

    public event Func<string, Task>? OnCharacterUpdate;
    public event Func<string, Task>? OnActivityUpdate;
    public event Func<string, Task>? OnBattleUpdate;
    public event Func<string, Task>? OnNotification;
    public event Func<string, object, Task>? OnRealtimeEvent;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public GameHubClient(ILogger<GameHubClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        _hubUrl = configuration.GetValue<string>("SignalR:HubUrl") ?? "https://localhost:7000/gamehub";
    }

    public async Task StartAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
            .Build();

        // 注册事件处理器
        RegisterEventHandlers();

        // 连接事件
        _connection.Reconnecting += OnReconnecting;
        _connection.Reconnected += OnReconnected;
        _connection.Closed += OnClosed;

        try
        {
            await _connection.StartAsync();
            _logger.LogInformation("SignalR connection started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            _logger.LogInformation("SignalR connection stopped");
        }
    }

    public async Task JoinGameAsync(string userId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinGame", userId);
            _logger.LogInformation("Joined game for user: {UserId}", userId);
        }
    }

    public async Task LeaveGameAsync()
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveGame");
            _logger.LogInformation("Left game");
        }
    }

    public async Task SendCharacterActionAsync(Guid characterId, string action, object data)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("CharacterAction", characterId, action, data);
        }
    }

    public async Task JoinCharacterGroupAsync(Guid characterId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinCharacterGroup", characterId);
            _logger.LogInformation("Joined character group: {CharacterId}", characterId);
        }
    }

    public async Task LeaveCharacterGroupAsync(Guid characterId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveCharacterGroup", characterId);
            _logger.LogInformation("Left character group: {CharacterId}", characterId);
        }
    }

    public async Task JoinBattleGroupAsync(Guid battleId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinBattleGroup", battleId);
            _logger.LogInformation("Joined battle group: {BattleId}", battleId);
        }
    }

    public async Task LeaveBattleGroupAsync(Guid battleId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveBattleGroup", battleId);
            _logger.LogInformation("Left battle group: {BattleId}", battleId);
        }
    }

    public async Task JoinGroupAsync(string groupName)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("JoinGroup", groupName);
            _logger.LogInformation("Joined group: {GroupName}", groupName);
        }
    }

    public async Task LeaveGroupAsync(string groupName)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("LeaveGroup", groupName);
            _logger.LogInformation("Left group: {GroupName}", groupName);
        }
    }

    private void RegisterEventHandlers()
    {
        if (_connection == null) return;

        _connection.On<string>("CharacterUpdate", async (data) =>
        {
            try
            {
                if (OnCharacterUpdate != null)
                    await OnCharacterUpdate.Invoke(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling CharacterUpdate event");
            }
        });

        _connection.On<string>("ActivityUpdate", async (data) =>
        {
            try
            {
                if (OnActivityUpdate != null)
                    await OnActivityUpdate.Invoke(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ActivityUpdate event");
            }
        });

        _connection.On<string>("BattleUpdate", async (data) =>
        {
            try
            {
                if (OnBattleUpdate != null)
                    await OnBattleUpdate.Invoke(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling BattleUpdate event");
            }
        });

        _connection.On<string>("Notification", async (message) =>
        {
            try
            {
                if (OnNotification != null)
                    await OnNotification.Invoke(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Notification event");
            }
        });

        _connection.On<string, object>("RealtimeEvent", async (eventType, data) =>
        {
            try
            {
                if (OnRealtimeEvent != null)
                    await OnRealtimeEvent.Invoke(eventType, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling RealtimeEvent");
            }
        });
    }

    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR connection lost, attempting to reconnect");
        return Task.CompletedTask;
    }

    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR connection reconnected with ID: {ConnectionId}", connectionId);
        return Task.CompletedTask;
    }

    private Task OnClosed(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "SignalR connection closed due to error");
        }
        else
        {
            _logger.LogInformation("SignalR connection closed");
        }
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
            _isDisposed = true;
        }
    }
}