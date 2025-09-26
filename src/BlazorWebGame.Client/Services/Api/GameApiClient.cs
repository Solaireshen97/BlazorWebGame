using BlazorWebGame.Shared.Interfaces;

namespace BlazorWebGame.Client.Services.Api;

/// <summary>
/// 统一的游戏API客户端，提供对所有API服务的访问
/// </summary>
public class GameApiClient
{
    public IBattleApi Battle { get; }
    public ICharacterApi Character { get; }
    public IPartyApi Party { get; }
    public IInventoryApi Inventory { get; }
    public IEquipmentApi Equipment { get; }
    public IProductionApi Production { get; }
    public IQuestApi Quest { get; }
    public IAuthApi Auth { get; }
    public IOfflineSettlementApi OfflineSettlement { get; }
    public IMonitoringApi Monitoring { get; }

    public GameApiClient(
        IBattleApi battle,
        ICharacterApi character,
        IPartyApi party,
        IInventoryApi inventory,
        IEquipmentApi equipment,
        IProductionApi production,
        IQuestApi quest,
        IAuthApi auth,
        IOfflineSettlementApi offlineSettlement,
        IMonitoringApi monitoring)
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