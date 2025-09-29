# BlazorWebGame SQLite 数据库使用说明文档

## 概述

BlazorWebGame 项目使用 SQLite 作为主要的数据存储解决方案，提供了高性能、轻量级且功能完整的数据库支持。本文档详细说明了 SQLite 数据库的配置、使用和优化。

## 项目架构

### 数据库上下文

项目使用 `ConsolidatedGameDbContext` 作为主要的数据库上下文，集成了所有最佳实践和性能优化。

#### 主要实体表

- **Players** - 玩家数据表
  - 基本信息：ID、姓名、等级、经验值、生命值、金币
  - 游戏状态：当前职业、当前动作、队伍ID、在线状态
  - JSON 数据：属性、背包、技能、装备

- **Teams** - 队伍数据表
  - 队伍信息：ID、名称、队长ID、最大成员数
  - 状态管理：当前状态、当前战斗ID、最后战斗时间
  - 成员管理：成员ID列表（JSON格式）

- **ActionTargets** - 动作目标表
  - 目标信息：玩家ID、目标类型、目标ID、目标名称
  - 进度管理：动作类型、进度、持续时间、完成状态
  - 进度数据：详细进度信息（JSON格式）

- **BattleRecords** - 战斗记录表
  - 战斗信息：战斗ID、战斗类型、开始时间、结束时间
  - 参与者：参与者列表、敌人列表（JSON格式）
  - 战斗数据：动作记录、结果数据（JSON格式）

- **OfflineData** - 离线数据表
  - 玩家离线数据同步和管理
  - 数据类型分类和版本控制
  - 同步状态跟踪

## 配置说明

### appsettings.json 配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gamedata.db;Cache=Shared"
  },
  "ConsolidatedDataStorage": {
    "StorageType": "SQLite",
    "ConnectionString": "Data Source=gamedata.db;Cache=Shared",
    "EnableCaching": true,
    "CacheExpirationMinutes": 30,
    "HighPriorityCacheExpirationHours": 2,
    "EnableBatchOperations": true,
    "BatchSize": 100,
    "BatchIntervalSeconds": 5,
    "EnablePerformanceMonitoring": true,
    "EnableTransactionSupport": true,
    "ConnectionTimeoutSeconds": 30,
    "CommandTimeoutSeconds": 30,
    "EnableAutoMigration": true,
    "EnableHealthChecks": true,
    "EnableAutoBackup": false,
    "AutoBackupIntervalHours": 24,
    "BackupRetentionDays": 7,
    "SqliteOptimization": {
      "EnableWALMode": true,
      "CacheSize": 10000,
      "EnableMemoryMapping": true,
      "MemoryMapSize": 268435456,
      "SynchronousMode": "NORMAL",
      "TempStore": "MEMORY",
      "EnableOptimizer": true,
      "AnalysisLimit": 1000,
      "IdleConnectionTimeout": 300,
      "ConnectionPoolSize": 10
    }
  }
}
```

### SQLite 优化选项说明

#### 核心配置

- **EnableWALMode**: 启用 Write-Ahead Logging 模式
  - 默认值: `true`
  - 作用: 提高并发读写性能，允许同时读写操作

- **CacheSize**: 缓存页数
  - 默认值: `10000` (约40MB)
  - 作用: 设置 SQLite 内存缓存大小，提高查询性能

- **EnableMemoryMapping**: 启用内存映射
  - 默认值: `true`
  - 配合 `MemoryMapSize` 使用，将数据库文件映射到内存

- **MemoryMapSize**: 内存映射大小
  - 默认值: `268435456` (256MB)
  - 作用: 设置内存映射区域大小

#### 同步和存储配置

- **SynchronousMode**: 同步模式
  - 默认值: `"NORMAL"`
  - 可选值: `"OFF"`, `"NORMAL"`, `"FULL"`, `"EXTRA"`
  - 作用: 平衡性能和数据安全性

- **TempStore**: 临时存储模式
  - 默认值: `"MEMORY"`
  - 可选值: `"DEFAULT"`, `"FILE"`, `"MEMORY"`
  - 作用: 临时表和索引存储位置

#### 优化配置

- **EnableOptimizer**: 启用查询优化器
  - 默认值: `true`
  - 作用: 自动优化查询计划和索引使用

- **AnalysisLimit**: 分析限制
  - 默认值: `1000`
  - 作用: 限制 ANALYZE 命令分析的行数

## 数据存储服务

### ConsolidatedDataStorageService

主要的数据存储服务，提供完整的 CRUD 操作和高级功能。

#### 主要功能

1. **玩家数据管理**
   - 创建、读取、更新、删除玩家数据
   - 在线玩家状态管理
   - 玩家搜索和过滤

2. **队伍管理**
   - 队伍创建和解散
   - 成员添加和移除
   - 队伍状态跟踪

3. **动作目标管理**
   - 采集、制作等动作进度跟踪
   - 目标完成状态管理

4. **战斗记录**
   - 战斗历史记录
   - 战斗统计分析

5. **离线数据同步**
   - 离线期间数据缓存
   - 数据同步和冲突解决

### SqliteDataStorageService

专门针对 SQLite 优化的数据存储服务。

#### 修复的问题

1. **MapToDto 方法优化**
   - 将实例方法改为静态方法，避免 Entity Framework 内存泄漏警告
   - 提高查询性能和内存使用效率

2. **查询优化**
   - 使用高效的数据库查询
   - 避免 N+1 查询问题
   - 合理使用索引

## 性能优化

### 数据库级别优化

1. **WAL 模式**
   - 启用 Write-Ahead Logging
   - 允许并发读写操作
   - 提高整体性能

2. **内存映射**
   - 将数据库文件映射到内存
   - 减少磁盘I/O操作
   - 提高读取速度

3. **缓存优化**
   - 设置合适的缓存大小
   - 使用内存存储临时数据
   - 减少磁盘访问

### 索引优化

数据库包含了经过优化的索引设计：

#### 玩家表索引
- 主键索引：`Id`
- 唯一索引：`Name`
- 复合索引：`IsOnline, LastActiveAt`
- 复合索引：`PartyId, IsOnline`
- 复合索引：`Level, IsOnline`

#### 战斗记录表索引
- 主键索引：`Id`
- 唯一索引：`BattleId`
- 复合索引：`StartedAt, Status, BattleType`
- 复合索引：`PartyId, Status`

### 应用级别优化

1. **连接池管理**
   - 配置合适的连接池大小
   - 避免连接泄漏

2. **批量操作**
   - 启用批量插入和更新
   - 减少数据库往返次数

3. **缓存策略**
   - 内存缓存热点数据
   - 合理设置缓存过期时间

## 健康检查和监控

### 健康检查

系统提供了完善的健康检查机制：

1. **数据库连接检查**
   - 验证数据库连接状态
   - 检查基本查询功能

2. **统计信息检查**
   - 获取各表记录数量
   - 监控数据库大小和性能

### 监控指标

- 在线玩家数量
- 活跃队伍数量
- 进行中的战斗数量
- 未同步的离线数据数量
- 数据库文件大小

## 维护和备份

### 自动维护

系统提供自动维护服务：

1. **定期优化**
   - 每6小时执行数据库优化
   - 重建索引（每周日执行）
   - 数据库压缩（每月1日执行）

2. **自动备份**
   - 可配置的备份间隔
   - 自动清理过期备份
   - 备份文件管理

### 手动维护

可以通过 API 或服务接口执行：

1. **数据库优化**
   ```sql
   PRAGMA optimize;
   ANALYZE;
   ```

2. **数据库压缩**
   ```sql
   VACUUM;
   ```

3. **索引重建**
   ```sql
   REINDEX;
   ```

## 故障排除

### 常见问题

1. **数据库锁定**
   - 检查 WAL 模式是否启用
   - 确保连接正确关闭
   - 检查长时间运行的事务

2. **性能问题**
   - 检查索引使用情况
   - 分析慢查询
   - 调整缓存设置

3. **内存使用过高**
   - 调整缓存大小设置
   - 检查内存映射配置
   - 监控连接池使用

### 调试和诊断

1. **启用详细日志**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "BlazorWebGame.Server.Data": "Debug",
         "Microsoft.EntityFrameworkCore": "Information"
       }
     }
   }
   ```

2. **查看数据库统计**
   - 使用健康检查端点：`/health`
   - 查看数据库统计信息
   - 监控性能指标

## 最佳实践

### 开发建议

1. **事务管理**
   - 合理使用事务范围
   - 避免长时间事务
   - 正确处理事务回滚

2. **查询优化**
   - 使用适当的查询方法
   - 避免不必要的数据加载
   - 合理使用投影

3. **数据模型设计**
   - JSON 字段用于灵活数据
   - 关键查询字段单独存储
   - 合理设计索引

### 生产环境配置

1. **安全配置**
   - 设置数据库文件权限
   - 定期备份数据
   - 监控磁盘使用

2. **性能配置**
   - 根据服务器资源调整缓存大小
   - 监控和调优索引使用
   - 定期进行性能测试

3. **监控和告警**
   - 设置健康检查告警
   - 监控数据库大小增长
   - 跟踪查询性能指标

## 版本历史

### v1.0.0 (当前版本)
- 实现完整的 SQLite 数据存储方案
- 修复 MapToDto 方法内存泄漏问题
- 添加完善的性能优化配置
- 实现自动维护和备份功能
- 提供健康检查和监控功能

## 支持和贡献

如有问题或建议，请：
1. 查看本文档的故障排除部分
2. 检查系统日志和健康检查状态
3. 提交问题报告或改进建议

---

*本文档最后更新：2025年9月29日*