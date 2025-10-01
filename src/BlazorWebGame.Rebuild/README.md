# BlazorWebGame.Rebuild

## 项目说明 (Project Description)

这是从 `BlazorWebGame.Server` 项目生成的重建版本项目。此项目**保留了完整的框架结构、事件系统、数据库配置和所有接口定义**，但**移除了业务逻辑实现部分**。

This is a rebuild version generated from the `BlazorWebGame.Server` project. This project **retains the complete framework structure, event system, database configuration, and all interface definitions**, but **removes business logic implementations**.

## 重要特性 (Key Features)

### ✅ 保留的内容 (What's Preserved)

1. **完整的项目架构 (Complete Project Architecture)**
   - 所有控制器类和路由配置 (All controller classes and routing)
   - 所有服务类和依赖注入配置 (All service classes and DI configuration)
   - SignalR Hub 定义 (SignalR Hub definitions)
   - 中间件和过滤器 (Middleware and filters)

2. **事件驱动系统 (Event-Driven System)**
   - 统一事件队列 (UnifiedEventQueue)
   - 事件管理器 (GameEventManager)
   - 事件持久化接口 (Event persistence interfaces)
   - 所有事件类型定义 (All event type definitions)

3. **数据库架构 (Database Architecture)**
   - Entity Framework Core DbContext
   - 所有实体模型配置 (All entity model configurations)
   - 数据库关系映射 (Database relationship mappings)
   - 索引和约束定义 (Index and constraint definitions)

4. **依赖注入 (Dependency Injection)**
   - Program.cs 中的完整服务注册 (Complete service registration in Program.cs)
   - 服务生命周期配置 (Service lifetime configurations)
   - 所有构造函数注入 (All constructor injections)

5. **API 接口定义 (API Interface Definitions)**
   - 所有 HTTP 端点签名 (All HTTP endpoint signatures)
   - 请求/响应 DTO (Request/Response DTOs)
   - 授权和验证特性 (Authorization and validation attributes)

6. **配置系统 (Configuration System)**
   - appsettings.json 配置文件
   - 配置选项类 (Configuration option classes)
   - JWT 认证配置 (JWT authentication configuration)

### 🔧 可以使用的场景 (Use Cases)

1. **学习系统架构** - 理解整个系统的设计和组件关系
2. **创建全新实现** - 基于相同接口契约实现不同的业务逻辑
3. **重构和优化** - 在保持接口不变的前提下重新实现功能
4. **教学和培训** - 作为学习材料展示良好的架构设计
5. **原型开发** - 快速构建新功能的原型

## 项目结构 (Project Structure)

```
BlazorWebGame.Rebuild/
├── Controllers/                    # API 控制器
│   ├── AuthController.cs          # 认证控制器
│   ├── BattleController.cs        # 战斗控制器
│   ├── CharacterController.cs     # 角色控制器
│   ├── PartyController.cs         # 组队控制器
│   └── ...                        # 其他控制器
├── Services/                       # 服务层
│   ├── Core/                      # 核心服务
│   │   ├── GameEngineService.cs   # 游戏引擎
│   │   ├── GameLoopService.cs     # 游戏循环
│   │   └── ...
│   ├── Battle/                    # 战斗服务
│   ├── Character/                 # 角色服务
│   ├── Data/                      # 数据服务
│   └── ...                        # 其他服务目录
├── Hubs/                          # SignalR Hubs
│   └── GameHub.cs                 # 游戏实时通信Hub
├── Data/                          # 数据访问层
│   └── GameDbContext.cs           # 数据库上下文
├── Security/                      # 安全认证
│   └── GameAuthenticationService.cs
├── Middleware/                    # 中间件
│   ├── RateLimitingMiddleware.cs
│   ├── ErrorHandlingMiddleware.cs
│   └── ...
├── Configuration/                 # 配置选项
│   ├── GameServerOptions.cs
│   ├── SecurityOptions.cs
│   └── ...
├── Validation/                    # 验证特性
└── Program.cs                     # 应用程序入口
```

## 核心组件说明 (Core Components)

### 1. 游戏引擎服务 (GameEngineService)

负责游戏核心逻辑的协调和处理：
- 角色管理
- 战斗系统
- 经验值和升级
- 装备系统

### 2. 事件系统 (Event System)

统一的事件驱动架构：
```csharp
// 事件定义示例
public class BattleStartEvent
{
    public string BattleId { get; set; }
    public List<string> PlayerIds { get; set; }
    public string EnemyId { get; set; }
}

// 事件处理器
public class EventDrivenBattleEngine
{
    public void HandleBattleStart(BattleStartEvent evt)
    {
        // 实现战斗开始逻辑
    }
}
```

### 3. 数据库上下文 (GameDbContext)

包含所有数据表定义：
- Users (用户)
- Players (角色)
- Teams (队伍)
- BattleRecords (战斗记录)
- OfflineData (离线数据)
- UserCharacters (用户角色关联)

### 4. SignalR 实时通信

GameHub 提供实时数据推送：
- 战斗状态更新
- 角色属性变化
- 组队状态同步
- 系统通知

## 如何开始开发 (Getting Started)

### 1. 安装依赖

```bash
cd src/BlazorWebGame.Rebuild
dotnet restore
```

### 2. 配置数据库

编辑 `appsettings.json`：
```json
{
  "ConnectionStrings": {
    "GameDatabase": "Data Source=gamedata.db"
  }
}
```

### 3. 运行项目

```bash
dotnet run
```

### 4. 开始实现

选择一个服务开始实现业务逻辑。例如，实现角色创建：

```csharp
public async Task<CharacterDto> CreateCharacterAsync(CreateCharacterRequest request, string? userId = null)
{
    // 验证输入
    if (string.IsNullOrEmpty(request.Name))
    {
        throw new ArgumentException("Character name is required");
    }
    
    // 创建角色
    var character = new CharacterDetailsDto
    {
        Id = Guid.NewGuid().ToString(),
        Name = request.Name,
        Health = 100,
        MaxHealth = 100,
        Gold = 0,
        LastUpdated = DateTime.UtcNow
    };
    
    // 初始化角色数据
    _playerUtilityService.InitializeCollections(character);
    _playerAttributeService.InitializePlayerAttributes(character);
    
    // 保存到数据库
    await _dataStorage.SaveCharacterAsync(character);
    
    // 发布事件
    var evt = new CharacterCreatedEvent
    {
        CharacterId = character.Id,
        Name = character.Name
    };
    _eventManager.PublishEvent(evt);
    
    return MapToDto(character);
}
```

## 关键接口和契约 (Key Interfaces and Contracts)

### IDataStorageService

数据存储服务接口，支持多种存储后端：
- Memory (内存存储)
- SQLite (SQLite 数据库)
- Redis (分布式缓存)

### IEventProcessor

事件处理器接口，所有事件处理器必须实现：
```csharp
public interface IEventProcessor
{
    void ProcessEvent(UnifiedEvent evt);
    string GetProcessorName();
}
```

## 依赖项 (Dependencies)

- .NET 8.0
- Entity Framework Core 8.0
- ASP.NET Core SignalR
- Serilog (日志)
- JWT Authentication

## 相关项目 (Related Projects)

- **BlazorWebGame.Server** - 原始完整实现
- **BlazorWebGame.Shared** - 共享模型和接口
- **BlazorWebGame.Client** - Blazor WebAssembly 客户端

## 开发建议 (Development Tips)

1. **渐进式实现** - 从核心功能开始，逐步添加特性
2. **测试驱动** - 为每个实现编写单元测试
3. **保持接口一致** - 不要修改公共接口签名
4. **使用事件** - 充分利用事件系统解耦组件
5. **日志记录** - 使用 ILogger 记录关键操作

## 注意事项 (Important Notes)

⚠️ **此项目包含完整的实现代码**  
如果您需要的是仅包含接口定义（无实现）的骨架项目，请参考文档创建接口版本。

⚠️ **命名空间已更新**  
所有代码使用 `BlazorWebGame.Rebuild` 命名空间，与原项目隔离。

⚠️ **独立运行**  
此项目可以独立编译和运行，不依赖原 Server 项目。

## 许可证 (License)

遵循主项目的许可证协议。

---

## 快速参考 (Quick Reference)

### 启动服务器
```bash
dotnet run --urls="https://localhost:7052;http://localhost:5191"
```

### 查看 API 文档
```
https://localhost:7052/swagger
```

### 健康检查
```
https://localhost:7052/health
```

### 数据库迁移
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
