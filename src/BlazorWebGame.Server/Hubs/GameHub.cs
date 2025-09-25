using Microsoft.AspNetCore.SignalR;
using BlazorWebGame.Server.Services;

namespace BlazorWebGame.Server.Hubs;

/// <summary>
/// SignalR Hub for real-time game updates
/// </summary>
public class GameHub : Hub
{
    private readonly ILogger<GameHub> _logger;
    private readonly ServerEventService _eventService;

    public GameHub(ILogger<GameHub> logger, ServerEventService eventService)
    {
        _logger = logger;
        _eventService = eventService;
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

    /// <summary>
    /// Join a party group to receive party-related updates
    /// </summary>
    public async Task JoinParty(string partyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"party_{partyId}");
        _logger.LogInformation("Client {ConnectionId} joined party {PartyId}", Context.ConnectionId, partyId);
    }

    /// <summary>
    /// Leave a party group
    /// </summary>
    public async Task LeaveParty(string partyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"party_{partyId}");
        _logger.LogInformation("Client {ConnectionId} left party {PartyId}", Context.ConnectionId, partyId);
    }

    /// <summary>
    /// Join character updates for specific character
    /// </summary>
    public async Task JoinCharacterUpdates(string characterId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"character_{characterId}");
        _logger.LogInformation("Client {ConnectionId} joined character updates for {CharacterId}", Context.ConnectionId, characterId);
    }

    /// <summary>
    /// Leave character updates
    /// </summary>
    public async Task LeaveCharacterUpdates(string characterId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"character_{characterId}");
        _logger.LogInformation("Client {ConnectionId} left character updates for {CharacterId}", Context.ConnectionId, characterId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}