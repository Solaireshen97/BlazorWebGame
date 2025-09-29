# 用户账号注册与登录系统文档

## 概述

本文档描述了BlazorWebGame项目中实现的用户账号注册与登录系统，包括数据存储、安全机制、API接口和使用说明。

## 系统架构

### 核心组件

```
BlazorWebGame.Shared/
├── Models/
│   └── DataStorageModels.cs          # 用户实体模型 (UserEntity)
├── DTOs/
│   ├── DataStorageDTOs.cs           # 用户数据传输对象 (UserStorageDto)
│   └── AuthenticationDTOs.cs        # 认证请求和响应对象
└── Interfaces/
    └── IDataStorageService.cs       # 数据存储服务接口

BlazorWebGame.Server/
├── Services/
│   ├── UserService.cs               # 用户管理服务
│   ├── DataStorageService.cs        # 内存数据存储实现
│   └── SqliteDataStorageService.cs  # SQLite数据存储实现
├── Controllers/
│   └── AuthController.cs           # 认证API控制器
├── Security/
│   └── GameAuthenticationService.cs # JWT令牌管理
└── Tests/
    └── UserServiceTests.cs         # 用户服务测试
```

## 数据模型

### UserEntity (用户实体)
```csharp
public class UserEntity : BaseEntity
{
    public string Username { get; set; }      // 用户名 (唯一)
    public string Email { get; set; }         // 邮箱
    public string PasswordHash { get; set; }  // 密码哈希
    public string Salt { get; set; }          // 密码盐值
    public bool IsActive { get; set; }        // 账户状态
    public bool EmailVerified { get; set; }   // 邮箱验证状态
    public DateTime LastLoginAt { get; set; } // 最后登录时间
    public string LastLoginIp { get; set; }   // 最后登录IP
    public int LoginAttempts { get; set; }    // 登录尝试次数
    public DateTime? LockedUntil { get; set; } // 账户锁定截止时间
    public string RolesJson { get; set; }     // 用户角色 (JSON格式)
    public string ProfileJson { get; set; }   // 用户资料 (JSON格式)
}
```

### 数据库表结构
```sql
CREATE TABLE Users (
    Id NVARCHAR(100) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100),
    PasswordHash NVARCHAR(255) NOT NULL,
    Salt NVARCHAR(100) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    EmailVerified BOOLEAN NOT NULL DEFAULT 0,
    LastLoginAt DATETIME2 NOT NULL,
    LastLoginIp NVARCHAR(45),
    LoginAttempts INTEGER NOT NULL DEFAULT 0,
    LockedUntil DATETIME2,
    RolesJson TEXT NOT NULL DEFAULT '["Player"]',
    ProfileJson TEXT NOT NULL DEFAULT '{}',
    CreatedAt DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME2 NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 索引
CREATE UNIQUE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_IsActive ON Users(IsActive);
CREATE INDEX IX_Users_LastLoginAt ON Users(LastLoginAt);
```

## API 接口

### 用户注册
```http
POST /api/auth/register
Content-Type: application/json

{
    "username": "testuser",
    "password": "password123",
    "email": "test@example.com"
}
```

**响应:**
```json
{
    "success": true,
    "data": {
        "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "userId": "user-12345",
        "username": "testuser",
        "roles": ["Player"]
    },
    "message": "Registration successful",
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 用户登录
```http
POST /api/auth/login
Content-Type: application/json

{
    "username": "testuser",
    "password": "password123"
}
```

**响应:** (同注册响应格式)

### 刷新令牌
```http
POST /api/auth/refresh
Content-Type: application/json

{
    "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "userId": "user-12345"
}
```

### 获取当前用户信息
```http
GET /api/auth/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**响应:**
```json
{
    "success": true,
    "data": {
        "userId": "user-12345",
        "username": "testuser",
        "roles": ["Player"]
    },
    "message": "User information retrieved successfully",
    "timestamp": "2024-01-01T12:00:00Z"
}
```

### 用户登出
```http
POST /api/auth/logout
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 安全特性

### 密码安全
- 使用 **BCrypt** 进行密码哈希和盐值处理
- 密码最小长度：6个字符
- 支持密码强度验证（可扩展）

### 账户保护
- **登录失败保护**: 5次失败后锁定账户30分钟
- **账户状态管理**: 支持激活/停用账户
- **IP地址记录**: 记录最后登录IP用于安全审计

### JWT令牌安全
- 访问令牌有效期：60分钟（可配置）
- 刷新令牌机制支持长期会话
- 令牌包含用户ID、用户名和角色信息
- 支持时钟偏差容错（1分钟）

### 输入验证
- 用户名：3-20个字符，仅允许字母数字下划线
- 邮箱：标准邮箱格式验证
- 防止SQL注入和XSS攻击

## 数据存储实现

### 支持的存储类型
1. **内存存储** (`DataStorageService`): 开发和测试使用
2. **SQLite存储** (`SqliteDataStorageService`): 生产环境推荐

### 存储服务接口
```csharp
public interface IDataStorageService
{
    // 用户账号管理
    Task<UserStorageDto?> GetUserByUsernameAsync(string username);
    Task<UserStorageDto?> GetUserByIdAsync(string userId);
    Task<UserStorageDto?> GetUserByEmailAsync(string email);
    Task<ApiResponse<UserStorageDto>> CreateUserAsync(UserStorageDto user, string password);
    Task<ApiResponse<UserStorageDto>> UpdateUserAsync(UserStorageDto user);
    Task<bool> ValidateUserPasswordAsync(string userId, string password);
    Task<ApiResponse<bool>> UpdateUserPasswordAsync(string userId, string newPassword);
    Task<ApiResponse<bool>> UpdateUserLastLoginAsync(string userId, string ipAddress);
    Task<ApiResponse<bool>> LockUserAccountAsync(string userId, DateTime lockUntil);
    Task<ApiResponse<bool>> UnlockUserAccountAsync(string userId);
}
```

## 测试覆盖

### 自动化测试
系统包含完整的自动化测试套件 (`UserServiceTests.cs`)：

1. **用户注册测试**
   - 成功注册新用户
   - 重复用户名检测
   - 邮箱唯一性验证

2. **用户登录测试**
   - 正确凭据登录
   - 错误密码拒绝
   - 不存在用户拒绝

3. **密码验证测试**
   - 密码哈希验证
   - 无效密码检测

4. **用户角色测试**
   - 默认角色分配
   - 角色权限检查

### 运行测试
测试在开发环境下自动运行，可以在控制台输出中看到测试结果：
```
[07:44:31 INF] [Program] Starting UserService basic tests...
[07:44:31 INF] [Program] ✓ User registration test passed
[07:44:31 INF] [Program] ✓ User login test passed
[07:44:31 INF] [Program] ✓ Password validation test passed
[07:44:31 INF] [Program] ✓ User roles test passed
[07:44:31 INF] [Program] All UserService tests passed successfully!
```

## 配置选项

### JWT配置 (appsettings.json)
```json
{
  "Jwt": {
    "Key": "your-secret-key-here-minimum-256-bits",
    "Issuer": "BlazorWebGameServer",
    "Audience": "BlazorWebGameClient",
    "ExpireMinutes": 60
  }
}
```

### 数据库配置
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=gamedata_dev.db"
  },
  "GameServer": {
    "DataStorageType": "SQLite",
    "EnableDevelopmentTests": true
  }
}
```

## 部署指南

### 开发环境
1. 确保已安装 .NET 8.0 SDK
2. 运行 `dotnet build` 编译项目
3. 运行 `dotnet run` 启动服务器
4. 访问 `http://localhost:5239` 查看API文档

### 生产环境
1. 设置强密码的JWT密钥
2. 配置生产数据库连接字符串
3. 禁用开发测试 (`EnableDevelopmentTests: false`)
4. 启用HTTPS和其他安全措施

## 最佳实践

### 安全建议
1. **定期更换JWT密钥**
2. **启用HTTPS** 加密传输
3. **实施速率限制** 防止暴力攻击
4. **监控异常登录** 活动
5. **定期清理过期令牌**

### 性能优化
1. **缓存用户信息** 减少数据库查询
2. **异步操作** 提高响应性能
3. **连接池管理** 优化数据库连接
4. **索引优化** 加速用户查询

### 监控指标
- 注册用户数量
- 活跃用户统计
- 登录成功/失败率
- 账户锁定频率
- API响应时间

## 扩展功能

### 已实现
- ✅ 用户注册和登录
- ✅ 密码安全存储
- ✅ JWT令牌认证
- ✅ 账户锁定保护
- ✅ 双存储支持（内存+SQLite）
- ✅ 完整测试覆盖

### 待扩展
- 🔄 邮箱验证功能
- 🔄 密码重置功能
- 🔄 第三方登录集成
- 🔄 多因素认证
- 🔄 用户资料管理
- 🔄 角色权限系统扩展

## 故障排除

### 常见问题

**Q: 用户注册失败，提示"用户名已存在"**
A: 检查用户名是否已被使用，用户名不区分大小写。

**Q: 登录时提示"账户已锁定"**
A: 等待30分钟锁定期过期，或联系管理员解锁账户。

**Q: JWT令牌验证失败**
A: 检查令牌是否过期，确认JWT配置正确。

**Q: 数据库连接错误**
A: 检查SQLite数据库文件权限和路径配置。

### 日志分析
系统提供详细的日志记录，可以通过日志文件 `logs/blazorwebgame-*.log` 进行问题诊断。

---

*最后更新: 2024年12月*
*版本: 1.0.0*