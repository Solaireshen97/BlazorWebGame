# 用户账户接口快速参考手册

## 📋 接口分类总览

### 🔐 认证接口 (IAuthApi)
**位置**: `src/BlazorWebGame.Shared/Interfaces/IAuthApi.cs`

| 接口方法 | 功能 | 请求参数 | 返回类型 |
|---------|------|---------|---------|
| LoginAsync | 用户登录 | LoginRequest (用户名, 密码) | Token字符串 |
| RegisterAsync | 用户注册 | RegisterRequest (用户名, 密码, 邮箱) | Token字符串 |
| RefreshTokenAsync | 刷新令牌 | RefreshTokenRequest (刷新令牌) | 新Token字符串 |
| LogoutAsync | 用户登出 | 无 | bool |
| GetCurrentUserAsync | 获取当前用户 | 无 | UserInfoDto |
| DemoLoginAsync | 演示登录 | 无 | Token字符串 |

### 💾 数据存储接口 (IDataStorageService - 用户部分)
**位置**: `src/BlazorWebGame.Shared/Interfaces/IDataStorageService.cs`

#### 用户账号管理 (10个方法)

| 接口方法 | 功能 | 主要参数 |
|---------|------|---------|
| GetUserByUsernameAsync | 根据用户名查询 | username |
| GetUserByIdAsync | 根据ID查询 | userId |
| GetUserByEmailAsync | 根据邮箱查询 | email |
| CreateUserAsync | 创建用户 | UserStorageDto, password |
| UpdateUserAsync | 更新用户信息 | UserStorageDto |
| ValidateUserPasswordAsync | 验证密码 | userId, password |
| UpdateUserPasswordAsync | 更新密码 | userId, newPassword |
| UpdateUserLastLoginAsync | 更新登录信息 | userId, ipAddress |
| LockUserAccountAsync | 锁定账户 | userId, lockUntil |
| UnlockUserAccountAsync | 解锁账户 | userId |

#### 用户角色关联管理 (6个方法)

| 接口方法 | 功能 | 主要参数 |
|---------|------|---------|
| CreateUserCharacterAsync | 创建用户-角色关联 | userId, characterId, characterName |
| GetUserCharactersAsync | 获取用户的所有角色 | userId |
| GetCharacterOwnerAsync | 获取角色的拥有者 | characterId |
| UserOwnsCharacterAsync | 验证角色所有权 | userId, characterId |
| SetDefaultCharacterAsync | 设置默认角色 | userId, characterId |
| DeleteUserCharacterAsync | 删除角色关联 | userId, characterId |

---

## 🌐 HTTP API 端点

### AuthController
**位置**: `src/BlazorWebGame.Server/Controllers/AuthController.cs`

| 端点 | 方法 | 认证 | 功能 |
|-----|------|------|------|
| `/api/auth/login` | POST | 否 | 用户登录 |
| `/api/auth/register` | POST | 否 | 用户注册 |
| `/api/auth/refresh` | POST | 否 | 刷新令牌 |
| `/api/auth/logout` | POST | ✅ | 用户登出 |
| `/api/auth/me` | GET | ✅ | 获取当前用户信息 |
| `/api/auth/demo-login` | POST | 否 | 演示登录 |

### CharacterController (用户相关)
**位置**: `src/BlazorWebGame.Server/Controllers/CharacterController.cs`

| 端点 | 方法 | 认证 | 功能 |
|-----|------|------|------|
| `/api/character/my` | GET | ✅ | 获取当前用户的角色列表 |

---

## 📦 数据模型和DTO

### 核心模型

#### User (领域模型)
**位置**: `src/BlazorWebGame.Shared/Models/UserModels.cs`

```
User
├── 基本信息
│   ├── Id (用户ID)
│   ├── Username (用户名)
│   ├── Email (邮箱)
│   ├── IsActive (是否激活)
│   └── EmailVerified (邮箱已验证)
├── 时间信息
│   ├── CreatedAt (创建时间)
│   ├── UpdatedAt (更新时间)
│   ├── LastLoginAt (最后登录)
│   └── LastLoginIp (登录IP)
├── Profile (用户档案)
│   ├── DisplayName (显示名)
│   ├── Avatar (头像)
│   └── CustomProperties (自定义属性)
├── Security (安全信息)
│   ├── Roles (角色列表)
│   ├── LoginAttempts (登录尝试次数)
│   ├── LockedUntil (锁定截止时间)
│   ├── LastPasswordChange (密码修改时间)
│   └── LoginHistory (登录历史)
└── CharacterIds (拥有的游戏角色)
```

### 数据传输对象 (DTOs)

#### UserStorageDto
**位置**: `src/BlazorWebGame.Shared/DTOs/DataStorageDTOs.cs`  
**用途**: 存储层数据传输

```csharp
public class UserStorageDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime LastLoginAt { get; set; }
    public string LastLoginIp { get; set; }
    public int LoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public List<string> Roles { get; set; }
    public Dictionary<string, object> Profile { get; set; }
}
```

#### UserInfoDto
**位置**: `src/BlazorWebGame.Shared/Interfaces/IAuthApi.cs`  
**用途**: API返回用户信息

```csharp
public class UserInfoDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public List<string> Roles { get; set; }
}
```

#### UserCharacterStorageDto
**位置**: `src/BlazorWebGame.Shared/DTOs/DataStorageDTOs.cs`  
**用途**: 用户-角色关联数据

```csharp
public class UserCharacterStorageDto
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string CharacterId { get; set; }
    public string CharacterName { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime LastPlayedAt { get; set; }
}
```

---

## 🔧 服务层

### UserService
**位置**: `src/BlazorWebGame.Server/Services/UserService.cs`

**主要功能**:
```
UserService
├── ValidateUserAsync() - 验证用户凭据
├── RegisterUserAsync() - 注册新用户
├── GetUserByIdAsync() - 获取用户信息
├── UpdateLastLoginAsync() - 更新登录信息
├── UserHasRoleAsync() - 检查用户角色
└── UserHasCharacterAsync() - 检查角色所有权
```

**验证规则**:
- 用户名: 3-20字符，字母数字下划线
- 密码: 至少6个字符
- 邮箱: 标准邮箱格式
- 登录失败: 5次失败后锁定30分钟

---

## 🔄 数据流程

### 用户登录流程
```
1. 客户端发送 POST /api/auth/login
   ├── Body: { username, password }
   
2. AuthController.Login()
   ├── 调用 UserService.ValidateUserAsync()
   │   ├── 获取用户数据
   │   ├── 检查账户状态（锁定、激活）
   │   └── 验证密码
   ├── 生成JWT令牌
   └── 更新登录信息
   
3. 返回响应
   └── { accessToken, refreshToken, userId, username, roles }
```

### 用户注册流程
```
1. 客户端发送 POST /api/auth/register
   ├── Body: { username, password, email }
   
2. AuthController.Register()
   ├── 验证输入格式
   ├── 调用 UserService.RegisterUserAsync()
   │   ├── 验证用户名、邮箱唯一性
   │   ├── 创建用户记录
   │   └── 加密存储密码
   ├── 生成JWT令牌
   └── 更新登录信息
   
3. 返回响应
   └── { accessToken, refreshToken, userId, username, roles }
```

### 用户-角色关联流程
```
1. 用户创建游戏角色
   ├── 角色创建成功
   
2. 建立关联
   ├── 调用 CreateUserCharacterAsync()
   │   ├── userId: 当前用户ID
   │   ├── characterId: 新创建的角色ID
   │   ├── characterName: 角色名称
   │   └── isDefault: 是否设为默认
   
3. 后续访问
   ├── 调用 UserOwnsCharacterAsync() 验证所有权
   └── 调用 GetUserCharactersAsync() 获取用户所有角色
```

---

## 📝 修改指南

### 如果要修改用户相关功能，需要检查的文件：

#### 1️⃣ 模型层
- ✅ `src/BlazorWebGame.Shared/Models/UserModels.cs` - User, UserProfile, UserSecurity

#### 2️⃣ DTO层
- ✅ `src/BlazorWebGame.Shared/DTOs/DataStorageDTOs.cs` - UserStorageDto
- ✅ `src/BlazorWebGame.Shared/DTOs/AuthenticationDTOs.cs` - LoginRequest, RegisterRequest
- ✅ `src/BlazorWebGame.Shared/Interfaces/IAuthApi.cs` - UserInfoDto

#### 3️⃣ 接口层
- ✅ `src/BlazorWebGame.Shared/Interfaces/IAuthApi.cs` - 认证接口定义
- ✅ `src/BlazorWebGame.Shared/Interfaces/IDataStorageService.cs` - 数据存储接口定义

#### 4️⃣ 服务层
- ✅ `src/BlazorWebGame.Server/Services/UserService.cs` - 用户业务逻辑
- ✅ `src/BlazorWebGame.Server/Services/DataStorageService.cs` - 内存存储实现
- ✅ `src/BlazorWebGame.Server/Services/SqliteDataStorageService.cs` - SQLite存储实现

#### 5️⃣ 控制器层
- ✅ `src/BlazorWebGame.Server/Controllers/AuthController.cs` - 认证API
- ✅ `src/BlazorWebGame.Server/Controllers/CharacterController.cs` - 角色API

#### 6️⃣ 测试层
- ✅ `src/BlazorWebGame.Server/Tests/UserServiceTests.cs` - 用户服务测试
- ✅ `src/BlazorWebGame.Server/Tests/UserCharacterServiceTests.cs` - 用户角色关联测试

---

## ⚠️ 重要注意事项

### 安全性
- ❗ 密码必须加密存储（使用BCrypt）
- ❗ JWT令牌包含敏感信息，注意过期时间
- ❗ 登录失败次数限制防止暴力破解
- ❗ 账户锁定机制保护用户安全

### 数据一致性
- ❗ User模型和UserStorageDto需要保持同步
- ❗ 修改接口定义时要同步更新实现
- ❗ 用户-角色关联删除时需要级联处理

### 性能考虑
- ❗ 频繁调用的接口需要考虑缓存
- ❗ 用户查询建议使用索引
- ❗ 批量操作时注意事务处理

### 向后兼容
- ❗ 修改DTO时注意向后兼容
- ❗ 新增字段使用可选类型或默认值
- ❗ API版本控制（如有必要）

---

## 📚 相关文档

- **详细分析**: `USER_ACCOUNT_INTERFACES_ANALYSIS.md` - 完整接口分析文档
- **认证系统**: `USER_AUTHENTICATION_SYSTEM_README.md` - 认证系统详细说明
- **用户角色关系**: `USER_CHARACTER_RELATIONSHIP_SYSTEM_README.md` - 用户角色关系系统
- **数据存储**: `DATA_STORAGE_SERVICE_README.md` - 数据存储服务文档

---

## 🎯 快速查找

需要修改以下功能时，主要涉及的文件：

| 功能 | 主要文件 |
|-----|---------|
| 登录/注册逻辑 | AuthController.cs, UserService.cs |
| 用户数据结构 | UserModels.cs, UserStorageDto |
| 密码验证规则 | UserService.cs (ValidateRegistrationInput) |
| 用户-角色关联 | IDataStorageService.cs, DataStorageService.cs |
| JWT令牌生成 | GameAuthenticationService.cs |
| 账户锁定逻辑 | UserService.cs, UserSecurity类 |
| API端点 | AuthController.cs, CharacterController.cs |

---

**文档版本**: 1.0  
**最后更新**: 2024年  
**维护者**: BlazorWebGame开发团队
