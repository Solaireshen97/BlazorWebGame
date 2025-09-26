# 角色状态轮询系统

## 概述

本系统实现了服务端角色状态的采集、缓存和客户端轮询功能，使玩家能够实时监控所有角色的动作状态和进度。

## 系统架构

### 服务端组件

#### 1. CharacterStateService
- **功能**: 统一管理所有角色的状态信息
- **特性**:
  - 高并发支持（使用ConcurrentDictionary）
  - 事件驱动的状态更新
  - 智能缓存机制
  - 异步状态更新队列
  - 性能统计

#### 2. PlayerController API端点
- `GET /api/player/{characterId}/state` - 获取单个角色状态
- `POST /api/player/states` - 批量获取角色状态（支持增量更新）
- `GET /api/player/states/active` - 获取所有活跃角色状态
- `GET /api/player/states/stats` - 获取服务统计信息
- `POST /api/player/{characterId}/online-status` - 更新角色在线状态

#### 3. 事件处理系统
- **PLAYER_ACTION_STARTED** - 角色开始新动作
- **PLAYER_ACTION_COMPLETED** - 角色完成动作
- **GATHERING_STARTED** - 开始采集
- **CRAFTING_STARTED** - 开始制作
- **BATTLE_STARTED** - 开始战斗

### 客户端组件

#### 1. CharacterStateApiService
- 专用的角色状态API客户端
- 基于BaseApiService，支持配置化HttpClient
- 完整的错误处理和日志记录

#### 2. 增强的ClientGameStateService
- **新增功能**:
  - 角色状态轮询（2秒间隔）
  - 智能状态变更检测
  - 事件通知机制
  - 可配置的轮询开关

#### 3. CharacterStates.razor 监控页面
- 实时显示所有角色状态
- 可视化动作进度条
- 自动刷新切换
- 服务统计信息展示

## 数据传输对象 (DTOs)

### CharacterStateDto
```csharp
public class CharacterStateDto
{
    public string CharacterId { get; set; }
    public string CharacterName { get; set; }
    public PlayerActionStateDto CurrentAction { get; set; }
    public int Level { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsOnline { get; set; }
    public LocationDto? CurrentLocation { get; set; }
    // ... 其他属性
}
```

### PlayerActionStateDto
```csharp
public class PlayerActionStateDto
{
    public string ActionType { get; set; } // "Idle", "Combat", "Gathering", "Crafting"
    public string ActionTarget { get; set; }
    public double Progress { get; set; } // 0.0 到 1.0
    public double Duration { get; set; } // 总持续时间（秒）
    public double TimeRemaining { get; set; } // 剩余时间（秒）
    public DateTime StartTime { get; set; }
    public Dictionary<string, object> ActionData { get; set; }
}
```

## 使用方法

### 1. 服务端集成

服务已自动注册到DI容器：
```csharp
builder.Services.AddSingleton<CharacterStateService>();
builder.Services.AddSingleton<CharacterStateApiService>();
```

### 2. 更新角色状态

在游戏逻辑中调用状态更新：
```csharp
_characterStateService.UpdateCharacterActionState(
    characterId: "player123",
    actionType: "Gathering",
    actionTarget: "iron_ore_node",
    duration: 10.0
);
```

### 3. 客户端监听

订阅状态更新事件：
```csharp
GameStateService.OnCharacterStatesUpdated += states => {
    // 处理批量状态更新
};

GameStateService.OnCharacterStateChanged += state => {
    // 处理单个角色状态变更
};
```

### 4. 手动查询

直接调用API服务：
```csharp
var response = await CharacterStateApi.GetCharacterStateAsync("player123");
if (response.Success && response.Data != null)
{
    var characterState = response.Data;
    // 处理角色状态
}
```

## 性能优化

### 服务端优化
1. **缓存策略**: 5分钟过期时间，减少重复查询
2. **批量更新**: 异步队列处理状态更新，避免阻塞
3. **事件驱动**: 只在状态真正变化时更新缓存
4. **并发支持**: ConcurrentDictionary确保线程安全

### 客户端优化
1. **智能轮询**: 只查询有更新的角色
2. **增量更新**: 支持LastUpdateAfter参数减少数据传输
3. **变更检测**: 只在显著变化时触发UI更新
4. **连接管理**: 自动重连和错误恢复

## 监控和调试

### 1. 访问监控页面
导航到 `/character-states` 查看实时角色状态

### 2. 服务统计
- 总查询次数
- 总更新次数
- 缓存角色数量
- 队列更新数量

### 3. 日志记录
- 状态更新日志
- API调用日志
- 错误和警告日志

## 扩展功能

### 1. 添加新的动作类型
1. 在GameEventTypes中添加新的事件类型
2. 创建对应的事件处理器
3. 在UI中添加相应的显示逻辑

### 2. 增加状态字段
1. 扩展CharacterStateDto
2. 更新服务端状态构建逻辑
3. 修改UI显示组件

### 3. 自定义轮询频率
```csharp
// 可配置的轮询间隔
GameStateService.CharacterStatePollingInterval = TimeSpan.FromSeconds(5);
```

## 注意事项

1. **性能影响**: 轮询会增加服务器负载，建议根据实际需求调整频率
2. **网络开销**: 批量查询比单个查询更高效
3. **缓存一致性**: 状态更新可能有1-2秒的延迟
4. **错误处理**: 客户端应优雅处理网络错误和服务不可用情况

## 故障排除

### 常见问题

1. **角色状态不更新**
   - 检查CharacterStatePollingEnabled是否为true
   - 确认角色ID在跟踪列表中
   - 查看网络连接状态

2. **性能问题**
   - 减少轮询频率
   - 限制跟踪的角色数量
   - 检查服务端日志

3. **数据不一致**
   - 清理缓存数据
   - 重启服务或刷新页面
   - 检查事件处理器注册

### 调试建议

1. 启用详细日志记录
2. 使用浏览器开发者工具监控网络请求
3. 检查服务统计信息
4. 验证事件触发和处理流程