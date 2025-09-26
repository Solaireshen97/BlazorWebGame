# BlazorWebGame API 服务组织结构

本文档说明了客户端API服务的组织结构和使用方法。

## 概述

API服务按功能模块进行了重组，提供了更清晰的结构和更容易维护的代码。

## 服务架构

### 基础架构

- **BaseApiService**: 所有API服务的基类，提供通用HTTP请求功能
- **ConfigurableHttpClientFactory**: 可配置的HTTP客户端工厂，支持动态服务器地址配置

### 功能模块API服务

| 服务类 | 接口 | 功能描述 |
|--------|------|----------|
| `BattleApiService` | `IBattleApi` | 战斗系统API - 战斗开始、状态查询、动作执行等 |
| `CharacterApiService` | `ICharacterApi` | 角色系统API - 角色管理、属性查询、经验值更新等 |
| `PartyApiService` | `IPartyApi` | 组队系统API - 创建组队、加入离开、状态查询等 |
| `InventoryApiService` | `IInventoryApi` | 库存系统API - 物品管理、装备、出售等 |
| `EquipmentApiService` | `IEquipmentApi` | 装备系统API - 装备生成、价值计算等 |
| `ProductionApiService` | `IProductionApi` | 生产系统API - 采集节点、生产状态等 |
| `QuestApiService` | `IQuestApi` | 任务系统API - 任务状态、进度更新、完成等 |
| `AuthApiService` | `IAuthApi` | 认证系统API - 登录、注册、令牌管理等 |
| `OfflineSettlementApiService` | `IOfflineSettlementApi` | 离线结算API - 离线进度计算和结算 |
| `MonitoringApiService` | `IMonitoringApi` | 监控系统API - 性能指标、系统状态等 |

### 统一访问

- **GameApiClient**: 统一的API客户端，提供对所有功能模块的访问
- **GameApiService**: 向后兼容的统一接口，保持现有代码的兼容性

## 使用方法

### 1. 使用统一API客户端 (推荐)

```csharp
@inject GameApiClient ApiClient

// 战斗相关操作
var battleResult = await ApiClient.Battle.StartBattleAsync(request);
var battleState = await ApiClient.Battle.GetBattleStateAsync(battleId);

// 角色相关操作
var characters = await ApiClient.Character.GetCharactersAsync();
var characterDetails = await ApiClient.Character.GetCharacterDetailsAsync(characterId);

// 组队相关操作
var party = await ApiClient.Party.CreatePartyAsync(characterId);
var joinResult = await ApiClient.Party.JoinPartyAsync(characterId, partyId);
```

### 2. 使用单独的API服务

```csharp
@inject IBattleApi BattleApi
@inject ICharacterApi CharacterApi

// 直接使用特定的API服务
var battleResult = await BattleApi.StartBattleAsync(request);
var characters = await CharacterApi.GetCharactersAsync();
```

### 3. 向后兼容方式

```csharp
@inject GameApiService GameApi

// 保持现有代码不变
var battleResult = await GameApi.StartBattleAsync(request);
var party = await GameApi.CreatePartyAsync(characterId);

// 或获取新的API客户端
var apiClient = GameApi.GetApiClient();
```

## 认证设置

所有API服务都支持自动认证。推荐在应用启动时设置认证：

```csharp
// 快速演示登录
var authResult = await ApiClient.SetupAuthenticationAsync();

// 或使用具体的认证方法
var loginResult = await ApiClient.Auth.DemoLoginAsync();
```

## 错误处理

所有API调用返回统一的 `ApiResponse<T>` 格式：

```csharp
var response = await ApiClient.Battle.StartBattleAsync(request);
if (response.Success)
{
    var battleState = response.Data;
    // 处理成功结果
}
else
{
    // 处理错误
    Console.WriteLine($"错误: {response.Message}");
}
```

## 服务器连接检查

```csharp
var isAvailable = await ApiClient.IsServerAvailableAsync();
if (!isAvailable)
{
    // 服务器不可用的处理
}
```

## 迁移指南

### 从旧的GameApiService迁移

旧代码：
```csharp
@inject GameApiService GameApi
var result = await GameApi.StartBattleAsync(request);
```

推荐的新代码：
```csharp
@inject GameApiClient ApiClient
var result = await ApiClient.Battle.StartBattleAsync(request);
```

### 依赖注入配置

在 `Program.cs` 中，所有服务都已自动注册：

```csharp
// 单独的API服务接口
builder.Services.AddSingleton<IBattleApi, BattleApiService>();
builder.Services.AddSingleton<ICharacterApi, CharacterApiService>();
// ... 其他服务

// 统一的API客户端
builder.Services.AddSingleton<GameApiClient>();

// 向后兼容的统一接口
builder.Services.AddSingleton<GameApiService>();
```

## 开发和测试

### 添加新的API端点

1. 在对应的接口中添加方法定义（如 `IBattleApi`）
2. 在对应的实现类中添加方法实现（如 `BattleApiService`）
3. 更新服务端控制器以支持新的端点

### API测试

可以使用 `ServerApiTestService` 进行API测试：

```csharp
@inject ServerApiTestService TestService
await TestService.TestAllApisAsync();
```

## 最佳实践

1. **优先使用接口**: 通过接口注入服务，便于测试和维护
2. **统一错误处理**: 使用 `ApiResponse<T>` 的统一错误处理模式
3. **合理的超时设置**: 长时间运行的操作应设置适当的超时
4. **日志记录**: 所有API服务都内置了日志记录功能
5. **异常处理**: 网络错误会被自动捕获并转换为友好的错误消息

## 故障排除

### 常见问题

1. **认证失败**: 确保在调用API前调用了认证设置方法
2. **服务器连接失败**: 检查服务器地址配置和网络连接
3. **API返回错误**: 检查请求参数和服务端日志

### 调试技巧

1. 启用详细日志记录
2. 使用浏览器开发者工具检查网络请求
3. 检查服务端API文档和状态