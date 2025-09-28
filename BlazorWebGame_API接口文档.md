# BlazorWebGame 数据存储服务 API 接口文档

## 概述

本文档描述了 BlazorWebGame 项目中数据存储服务的完整 API 接口。该服务支持 SQLite 数据库持久化存储和内存存储两种模式，为游戏提供玩家数据、队伍信息、战斗记录、动作目标和离线数据的完整管理功能。

## 基础信息

- **服务基址**: `https://localhost:7051/api/DataStorage` (开发环境)
- **认证方式**: JWT Bearer Token
- **数据格式**: JSON
- **字符编码**: UTF-8

## 通用响应格式

### 成功响应格式
```json
{
  "success": true,
  "data": { /* 响应数据 */ },
  "message": "操作成功信息"
}
```

### 错误响应格式
```json
{
  "success": false,
  "data": null,
  "message": "错误信息描述"
}
```

### 批量操作响应格式
```json
{
  "successfulItems": [ /* 成功处理的项目列表 */ ],
  "errors": [ /* 错误信息列表 */ ],
  "totalProcessed": 100,
  "successCount": 95,
  "errorCount": 5
}
```

## 1. 玩家数据管理 API

### 1.1 获取玩家数据

**接口地址**: `GET /api/DataStorage/players/{playerId}`

**请求参数**:
- `playerId` (string, required): 玩家唯一标识符

**响应示例**:
```json
{
  "id": "player-12345",
  "name": "测试玩家",
  "level": 25,
  "experience": 12500,
  "health": 100,
  "maxHealth": 100,
  "gold": 1000,
  "selectedBattleProfession": "Warrior",
  "currentAction": "Idle",
  "currentActionTargetId": null,
  "partyId": "party-67890",
  "isOnline": true,
  "lastActiveAt": "2024-01-15T10:30:00Z",
  "createdAt": "2024-01-01T12:00:00Z",
  "updatedAt": "2024-01-15T10:30:00Z",
  "attributes": {
    "strength": 20,
    "agility": 15,
    "intelligence": 10
  },
  "inventory": [
    {
      "id": "item-001",
      "name": "基础剑",
      "quantity": 1
    }
  ],
  "skills": ["基础剑术", "防御"],
  "equipment": {
    "weapon": "基础剑",
    "armor": "皮甲"
  }
}
```

### 1.2 保存玩家数据

**接口地址**: `POST /api/DataStorage/players`

**请求体**: 完整的玩家数据对象 (参考上述响应格式)

**响应**: 包含更新后玩家数据的 ApiResponse

### 1.3 删除玩家数据

**接口地址**: `DELETE /api/DataStorage/players/{playerId}`

**响应**: 布尔值表示删除是否成功

### 1.4 获取在线玩家列表

**接口地址**: `GET /api/DataStorage/players/online`

**响应**: 在线玩家数组

### 1.5 批量保存玩家数据

**接口地址**: `POST /api/DataStorage/players/batch`

**请求体**: 玩家数据对象数组

**响应**: 批量操作结果

### 1.6 搜索玩家

**接口地址**: `GET /api/DataStorage/players/search`

**查询参数**:
- `searchTerm` (string, required): 搜索关键词
- `limit` (int, optional): 结果数量限制，默认20

**响应**: 匹配的玩家数组

## 2. 队伍数据管理 API

### 2.1 获取队伍数据

**接口地址**: `GET /api/DataStorage/teams/{teamId}`

**响应示例**:
```json
{
  "id": "team-67890",
  "name": "勇敢者队伍",
  "captainId": "player-12345",
  "memberIds": ["player-12345", "player-23456"],
  "maxMembers": 5,
  "status": "Active",
  "currentBattleId": null,
  "lastBattleAt": "2024-01-15T09:00:00Z",
  "createdAt": "2024-01-10T12:00:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

### 2.2 根据队长获取队伍

**接口地址**: `GET /api/DataStorage/teams/captain/{captainId}`

### 2.3 根据玩家获取队伍

**接口地址**: `GET /api/DataStorage/teams/player/{playerId}`

### 2.4 保存队伍数据

**接口地址**: `POST /api/DataStorage/teams`

### 2.5 删除队伍

**接口地址**: `DELETE /api/DataStorage/teams/{teamId}`

### 2.6 获取活跃队伍列表

**接口地址**: `GET /api/DataStorage/teams/active`

## 3. 动作目标管理 API

### 3.1 获取当前动作目标

**接口地址**: `GET /api/DataStorage/action-targets/current/{playerId}`

**响应示例**:
```json
{
  "id": "action-001",
  "playerId": "player-12345",
  "targetType": "Enemy",
  "targetId": "goblin-001",
  "targetName": "森林哥布林",
  "actionType": "Combat",
  "progress": 0.75,
  "duration": 30.0,
  "startedAt": "2024-01-15T10:00:00Z",
  "completedAt": null,
  "isCompleted": false,
  "progressData": {
    "damageDealt": 45,
    "experienceGained": 25
  },
  "createdAt": "2024-01-15T10:00:00Z",
  "updatedAt": "2024-01-15T10:22:30Z"
}
```

### 3.2 保存动作目标

**接口地址**: `POST /api/DataStorage/action-targets`

### 3.3 完成动作目标

**接口地址**: `POST /api/DataStorage/action-targets/{actionTargetId}/complete`

### 3.4 取消动作目标

**接口地址**: `DELETE /api/DataStorage/action-targets/current/{playerId}`

### 3.5 获取动作历史

**接口地址**: `GET /api/DataStorage/action-targets/history/{playerId}`

**查询参数**:
- `limit` (int, optional): 结果数量限制，默认50

## 4. 战斗记录管理 API

### 4.1 获取战斗记录

**接口地址**: `GET /api/DataStorage/battle-records/{battleId}`

**响应示例**:
```json
{
  "id": "battle-record-001",
  "battleId": "battle-12345",
  "battleType": "Normal",
  "startedAt": "2024-01-15T10:00:00Z",
  "endedAt": "2024-01-15T10:05:30Z",
  "status": "Victory",
  "participants": ["player-12345", "player-23456"],
  "enemies": [
    {
      "id": "goblin-001",
      "name": "森林哥布林",
      "level": 5
    }
  ],
  "actions": [
    {
      "timestamp": "2024-01-15T10:01:00Z",
      "playerId": "player-12345",
      "action": "Attack",
      "target": "goblin-001",
      "damage": 15
    }
  ],
  "results": {
    "experience": 100,
    "gold": 50,
    "items": ["皮革", "哥布林耳朵"]
  },
  "partyId": "party-67890",
  "dungeonId": null,
  "waveNumber": 0,
  "duration": 330
}
```

### 4.2 保存战斗记录

**接口地址**: `POST /api/DataStorage/battle-records`

### 4.3 结束战斗记录

**接口地址**: `POST /api/DataStorage/battle-records/{battleId}/end`

**请求体**:
```json
{
  "status": "Victory",
  "results": {
    "experience": 100,
    "gold": 50
  }
}
```

### 4.4 获取玩家战斗历史

**接口地址**: `GET /api/DataStorage/battle-records/player/{playerId}`

**查询参数**: 支持分页和过滤 (DataStorageQueryDto)
- `page` (int): 页码，默认1
- `pageSize` (int): 每页大小，默认20
- `startDate` (datetime): 开始时间
- `endDate` (datetime): 结束时间
- `status` (string): 战斗状态过滤

### 4.5 获取队伍战斗历史

**接口地址**: `GET /api/DataStorage/battle-records/team/{teamId}`

### 4.6 获取进行中的战斗

**接口地址**: `GET /api/DataStorage/battle-records/active`

## 5. 离线数据管理 API

### 5.1 保存离线数据

**接口地址**: `POST /api/DataStorage/offline-data`

**请求体示例**:
```json
{
  "id": "offline-001",
  "playerId": "player-12345",
  "dataType": "PlayerProgress",
  "data": {
    "level": 26,
    "experience": 13000,
    "completedQuests": ["quest-001", "quest-002"]
  },
  "isSynced": false,
  "version": 1
}
```

### 5.2 获取未同步的离线数据

**接口地址**: `GET /api/DataStorage/offline-data/unsynced/{playerId}`

### 5.3 标记离线数据为已同步

**接口地址**: `POST /api/DataStorage/offline-data/mark-synced`

**请求体**: 离线数据ID数组
```json
["offline-001", "offline-002", "offline-003"]
```

### 5.4 清理已同步的离线数据

**接口地址**: `DELETE /api/DataStorage/offline-data/cleanup`

**查询参数**:
- `olderThan` (datetime): 清理此时间之前的数据

## 6. 数据查询和统计 API

### 6.1 获取存储统计信息

**接口地址**: `GET /api/DataStorage/stats`

**响应示例**:
```json
{
  "success": true,
  "data": {
    "totalPlayers": 1500,
    "onlinePlayers": 120,
    "totalTeams": 300,
    "activeTeams": 80,
    "totalActionTargets": 450,
    "activeActionTargets": 95,
    "totalBattleRecords": 5000,
    "activeBattles": 15,
    "totalOfflineData": 200,
    "unsyncedOfflineData": 25,
    "lastUpdated": "2024-01-15T10:30:00Z"
  }
}
```

### 6.2 健康检查

**接口地址**: `GET /api/DataStorage/health`

**响应示例**:
```json
{
  "success": true,
  "data": {
    "status": "Healthy",
    "storageType": "SQLite",
    "timestamp": "2024-01-15T10:30:00Z",
    "databaseConnected": true,
    "storageStats": {
      "players": 1500,
      "teams": 300,
      "actionTargets": 450,
      "battleRecords": 5000,
      "offlineData": 200
    }
  }
}
```

## 7. 数据同步和备份 API

### 7.1 导出玩家数据

**接口地址**: `GET /api/DataStorage/export/player/{playerId}`

**响应**: 包含玩家完整数据的导出对象

### 7.2 导入玩家数据

**接口地址**: `POST /api/DataStorage/import/player/{playerId}`

**请求体**: 导出的玩家数据对象

### 7.3 数据备份

**接口地址**: `POST /api/DataStorage/backup`

**响应**: 备份任务ID

### 7.4 清理过期数据

**接口地址**: `DELETE /api/DataStorage/cleanup`

**查询参数**:
- `olderThanDays` (int): 清理多少天前的数据，默认30

## 配置说明

### 数据库配置

在 `appsettings.json` 中配置：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gamedata.db;Cache=Shared"
  },
  "GameServer": {
    "DataStorageType": "SQLite"
  }
}
```

### 存储类型选择

- `"SQLite"`: 使用 SQLite 数据库持久化存储
- `"Memory"` 或其他: 使用内存存储

## 错误码说明

| HTTP状态码 | 说明 |
|-----------|------|
| 200 | 成功 |
| 400 | 请求参数错误 |
| 401 | 未授权 |
| 404 | 资源不存在 |
| 500 | 服务器内部错误 |

## 使用示例

### C# 客户端示例

```csharp
using System.Text.Json;
using System.Text;

public class DataStorageClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public DataStorageClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    public async Task<PlayerStorageDto?> GetPlayerAsync(string playerId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/players/{playerId}");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PlayerStorageDto>(json);
        }
        return null;
    }

    public async Task<bool> SavePlayerAsync(PlayerStorageDto player)
    {
        var json = JsonSerializer.Serialize(player);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{_baseUrl}/players", content);
        return response.IsSuccessStatusCode;
    }
}
```

### JavaScript 客户端示例

```javascript
class DataStorageClient {
    constructor(baseUrl, token) {
        this.baseUrl = baseUrl;
        this.headers = {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        };
    }

    async getPlayer(playerId) {
        const response = await fetch(`${this.baseUrl}/players/${playerId}`, {
            headers: this.headers
        });
        
        if (response.ok) {
            return await response.json();
        }
        throw new Error(`Failed to get player: ${response.statusText}`);
    }

    async savePlayer(player) {
        const response = await fetch(`${this.baseUrl}/players`, {
            method: 'POST',
            headers: this.headers,
            body: JSON.stringify(player)
        });
        
        if (response.ok) {
            return await response.json();
        }
        throw new Error(`Failed to save player: ${response.statusText}`);
    }
}
```

## 性能考虑

1. **索引优化**: SQLite 数据库已配置适当的索引以优化查询性能
2. **连接池**: Entity Framework Core 自动管理数据库连接池
3. **批量操作**: 对于大量数据操作，优先使用批量 API
4. **分页查询**: 大量数据查询支持分页，避免内存溢出
5. **异步操作**: 所有 API 都是异步的，提高并发性能

## 监控和日志

系统提供详细的日志记录，包括：
- 数据库操作日志
- 性能监控日志
- 错误和异常日志
- 用户操作审计日志

通过健康检查端点可以监控服务状态和数据库连接状态。

## 版本兼容性

当前 API 版本: **v1.0**

API 设计遵循向后兼容原则，新增功能不会破坏现有客户端集成。