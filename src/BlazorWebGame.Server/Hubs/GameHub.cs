using Microsoft.AspNetCore.SignalR;

namespace BlazorWebGame.Server.Hubs;

/// <summary>
/// SignalR Hub for real-time game updates
/// </summary>
public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;

    public GameHub(ILogger<GameHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a battle group to receive real-time updates
    /// </summary>
    public async Task JoinBattle(string battleId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"battle-{battleId}");
        _logger.LogInformation("Client {ConnectionId} joined battle {BattleId}", Context.ConnectionId, battleId);
    }

    /// <summary>
    /// Leave a battle group
    /// </summary>
    public async Task LeaveBattle(string battleId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"battle-{battleId}");
        _logger.LogInformation("Client {ConnectionId} left battle {BattleId}", Context.ConnectionId, battleId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}