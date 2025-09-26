namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 统一的游戏API客户端，提供对所有API服务的访问
/// </summary>
public class GameApiClient
{
    public BattleApiService Battle { get; }
    public CharacterApiService Character { get; }
    public PartyApiService Party { get; }
    public InventoryApiService Inventory { get; }
    public EquipmentApiService Equipment { get; }
    public ProductionApiServiceNew Production { get; }
    public QuestApiService Quest { get; }
    public AuthApiService Auth { get; }
    public OfflineSettlementApiService OfflineSettlement { get; }
    public MonitoringApiService Monitoring { get; }

    public GameApiClient(
        BattleApiService battle,
        CharacterApiService character,
        PartyApiService party,
        InventoryApiService inventory,
        EquipmentApiService equipment,
        ProductionApiServiceNew production,
        QuestApiService quest,
        AuthApiService auth,
        OfflineSettlementApiService offlineSettlement,
        MonitoringApiService monitoring)
    {
        Battle = battle;
        Character = character;
        Party = party;
        Inventory = inventory;
        Equipment = equipment;
        Production = production;
        Quest = quest;
        Auth = auth;
        OfflineSettlement = offlineSettlement;
        Monitoring = monitoring;
    }

    /// <summary>
    /// 快速设置认证令牌 - 调用AuthApiService的演示登录方法
    /// </summary>
    public async Task<string> SetupAuthenticationAsync()
    {
        var authService = Auth as AuthApiService;
        if (authService != null)
        {
            var result = await authService.DemoLoginAsync();
            return result.Success ? "✅ " + result.Message : "❌ " + result.Message;
        }
        return "❌ Auth service not available";
    }

    /// <summary>
    /// 检查服务器连接状态
    /// </summary>
    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var result = await Battle.GetActiveBattlesAsync();
            return true; // 如果能够调用API而没有异常，说明服务器可用
        }
        catch
        {
            return false;
        }
    }
}