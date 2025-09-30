# 用户账户接口关系图

## 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                          客户端层                                 │
│                     (Blazor WebAssembly)                         │
│                                                                   │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │ 登录页面      │    │ 注册页面      │    │ 用户信息页面  │      │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘      │
│         │                   │                   │                │
│         └───────────────────┴───────────────────┘                │
│                             │                                     │
└─────────────────────────────┼─────────────────────────────────────┘
                              │
                              │ HTTP/HTTPS
                              │
┌─────────────────────────────┼─────────────────────────────────────┐
│                          API层 (Controller)                        │
│                                                                    │
│  ┌──────────────────────────▼──────────────────────────┐         │
│  │           AuthController                             │         │
│  ├──────────────────────────────────────────────────────┤         │
│  │ + POST /api/auth/login                               │         │
│  │ + POST /api/auth/register                            │         │
│  │ + POST /api/auth/refresh                             │         │
│  │ + POST /api/auth/logout                              │         │
│  │ + GET  /api/auth/me                                  │         │
│  │ + POST /api/auth/demo-login                          │         │
│  └───────────────────┬──────────────────────────────────┘         │
│                      │                                             │
│  ┌───────────────────▼──────────────────────────────────┐         │
│  │           CharacterController                         │         │
│  ├──────────────────────────────────────────────────────┤         │
│  │ + GET /api/character/my                              │         │
│  └──────────────────────────────────────────────────────┘         │
│                                                                    │
└────────────────────────────┼──────────────────────────────────────┘
                             │
                             │ 调用
                             │
┌────────────────────────────┼──────────────────────────────────────┐
│                        服务层 (Services)                           │
│                                                                    │
│  ┌────────────────────────▼────────────────────────────┐         │
│  │              UserService                             │         │
│  ├─────────────────────────────────────────────────────┤         │
│  │ + ValidateUserAsync()                               │         │
│  │ + RegisterUserAsync()                               │         │
│  │ + GetUserByIdAsync()                                │         │
│  │ + UpdateLastLoginAsync()                            │         │
│  │ + UserHasRoleAsync()                                │         │
│  │ + UserHasCharacterAsync()                           │         │
│  └─────────────────┬───────────────────────────────────┘         │
│                    │                                              │
│  ┌─────────────────▼───────────────────────────────────┐         │
│  │        GameAuthenticationService                     │         │
│  ├─────────────────────────────────────────────────────┤         │
│  │ + GenerateAccessToken()                             │         │
│  │ + GenerateRefreshToken()                            │         │
│  │ + ValidateToken()                                   │         │
│  │ + GetUserId()                                       │         │
│  │ + GetUsername()                                     │         │
│  └─────────────────────────────────────────────────────┘         │
│                                                                    │
└────────────────────────────┼──────────────────────────────────────┘
                             │
                             │ 调用
                             │
┌────────────────────────────┼──────────────────────────────────────┐
│                    数据访问层 (Data Access)                        │
│                                                                    │
│  ┌────────────────────────▼────────────────────────────┐         │
│  │         IDataStorageService (接口)                   │         │
│  ├─────────────────────────────────────────────────────┤         │
│  │ 用户账号管理 (10个方法):                              │         │
│  │ + GetUserByUsernameAsync()                          │         │
│  │ + GetUserByIdAsync()                                │         │
│  │ + GetUserByEmailAsync()                             │         │
│  │ + CreateUserAsync()                                 │         │
│  │ + UpdateUserAsync()                                 │         │
│  │ + ValidateUserPasswordAsync()                       │         │
│  │ + UpdateUserPasswordAsync()                         │         │
│  │ + UpdateUserLastLoginAsync()                        │         │
│  │ + LockUserAccountAsync()                            │         │
│  │ + UnlockUserAccountAsync()                          │         │
│  │                                                      │         │
│  │ 用户角色关联 (6个方法):                               │         │
│  │ + CreateUserCharacterAsync()                        │         │
│  │ + GetUserCharactersAsync()                          │         │
│  │ + GetCharacterOwnerAsync()                          │         │
│  │ + UserOwnsCharacterAsync()                          │         │
│  │ + SetDefaultCharacterAsync()                        │         │
│  │ + DeleteUserCharacterAsync()                        │         │
│  └─────────────────┬───────────────────────────────────┘         │
│                    │                                              │
│         ┌──────────┴──────────┐                                  │
│         │                     │                                  │
│  ┌──────▼──────────┐  ┌──────▼─────────────────┐                │
│  │ DataStorage     │  │ SqliteDataStorage      │                │
│  │ Service         │  │ Service                │                │
│  │ (内存实现)       │  │ (SQLite实现)            │                │
│  └─────────────────┘  └────────────────────────┘                │
│                                                                    │
└────────────────────────────┼──────────────────────────────────────┘
                             │
                             │ 存储
                             │
┌────────────────────────────┼──────────────────────────────────────┐
│                        数据层 (Data)                               │
│                                                                    │
│  ┌─────────────────────────▼───────────────────────────┐         │
│  │              内存存储 / SQLite数据库                  │         │
│  ├─────────────────────────────────────────────────────┤         │
│  │ 用户表 (Users)                                       │         │
│  │ 用户角色关联表 (UserCharacters)                       │         │
│  │ 玩家表 (Players)                                     │         │
│  └─────────────────────────────────────────────────────┘         │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

## 数据模型关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                         用户领域模型                              │
└─────────────────────────────────────────────────────────────────┘

    ┌──────────────────────────────────────────┐
    │            User (领域模型)                 │
    ├──────────────────────────────────────────┤
    │ - Id: string                             │
    │ - Username: string                       │
    │ - Email: string                          │
    │ - IsActive: bool                         │
    │ - EmailVerified: bool                    │
    │ - CreatedAt: DateTime                    │
    │ - UpdatedAt: DateTime                    │
    │ - LastLoginAt: DateTime                  │
    │ - LastLoginIp: string                    │
    │ - Profile: UserProfile ───────────┐      │
    │ - Security: UserSecurity ─────┐   │      │
    │ - CharacterIds: List<string> ─┼───┼───┐  │
    └──────────────────────────────┼───┼───┼──┘
                                   │   │   │
                ┌──────────────────┘   │   │
                │                      │   │
    ┌───────────▼──────────┐           │   │
    │   UserSecurity       │           │   │
    ├──────────────────────┤           │   │
    │ - Roles: List        │           │   │
    │ - LoginAttempts: int │           │   │
    │ - LockedUntil: Date? │           │   │
    │ - LastPasswordChange │           │   │
    │ - LoginHistory: List │           │   │
    ├──────────────────────┤           │   │
    │ + RecordSuccess()    │           │   │
    │ + RecordFailure()    │           │   │
    │ + IsLocked()         │           │   │
    │ + Unlock()           │           │   │
    └──────────────────────┘           │   │
                                       │   │
                    ┌──────────────────┘   │
                    │                      │
        ┌───────────▼──────────┐           │
        │   UserProfile        │           │
        ├──────────────────────┤           │
        │ - DisplayName: str   │           │
        │ - Avatar: string     │           │
        │ - CustomProperties   │           │
        ├──────────────────────┤           │
        │ + UpdateProfile()    │           │
        │ + SetProperty()      │           │
        │ + GetProperty()      │           │
        └──────────────────────┘           │
                                           │
                        ┌──────────────────┘
                        │
            ┌───────────▼────────────────┐
            │  UserCharacterRelation     │
            ├────────────────────────────┤
            │ - Id: string               │
            │ - UserId: string           │
            │ - CharacterId: string      │
            │ - IsActive: bool           │
            │ - IsDefault: bool          │
            │ - CreatedAt: DateTime      │
            │ - LastPlayedAt: DateTime   │
            ├────────────────────────────┤
            │ + SetAsDefault()           │
            │ + UnsetAsDefault()         │
            │ + Activate()               │
            │ + Deactivate()             │
            │ + UpdateLastPlayed()       │
            └────────────────────────────┘
```

## DTO 转换关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                      DTO 转换流程                                 │
└─────────────────────────────────────────────────────────────────┘

    User (领域模型)
         │
         │ 转换
         ▼
    UserStorageDto ◄──────────────┐
    (存储层传输)                    │
         │                         │
         │ 存储/检索                 │ 映射
         ▼                         │
    数据库/内存                      │
         │                         │
         │ 读取                     │
         ▼                         │
    UserStorageDto ────────────────┘
         │
         │ 转换
         ▼
    UserInfoDto
    (API层返回)
         │
         │ HTTP Response
         ▼
    客户端
```

## 用户认证流程图

```
┌─────────────────────────────────────────────────────────────────┐
│                      用户认证流程                                  │
└─────────────────────────────────────────────────────────────────┘

客户端                 AuthController          UserService       DataStorage
  │                        │                       │                 │
  │  POST /api/auth/login  │                       │                 │
  ├───────────────────────►│                       │                 │
  │  {username, password}  │                       │                 │
  │                        │                       │                 │
  │                        │ ValidateUserAsync()   │                 │
  │                        ├──────────────────────►│                 │
  │                        │                       │                 │
  │                        │                       │ GetUserByUsername│
  │                        │                       ├─────────────────►│
  │                        │                       │                 │
  │                        │                       │ UserStorageDto  │
  │                        │                       │◄─────────────────┤
  │                        │                       │                 │
  │                        │                       │ ValidatePassword │
  │                        │                       ├─────────────────►│
  │                        │                       │                 │
  │                        │                       │ true/false      │
  │                        │                       │◄─────────────────┤
  │                        │                       │                 │
  │                        │ UserStorageDto        │                 │
  │                        │◄──────────────────────┤                 │
  │                        │                       │                 │
  │                        │ GenerateAccessToken() │                 │
  │                        ├──────────────┐        │                 │
  │                        │              │        │                 │
  │                        │◄─────────────┘        │                 │
  │                        │ JWT Token             │                 │
  │                        │                       │                 │
  │                        │ UpdateLastLogin()     │                 │
  │                        ├──────────────────────►│                 │
  │                        │                       │                 │
  │                        │                       │ UpdateUserLastLogin│
  │                        │                       ├─────────────────►│
  │                        │                       │                 │
  │  AuthenticationResponse│                       │                 │
  │  {token, userId, ...}  │                       │                 │
  │◄───────────────────────┤                       │                 │
  │                        │                       │                 │
```

## 用户注册流程图

```
┌─────────────────────────────────────────────────────────────────┐
│                      用户注册流程                                  │
└─────────────────────────────────────────────────────────────────┘

客户端                AuthController         UserService        DataStorage
  │                       │                      │                  │
  │ POST /api/auth/register                     │                  │
  ├──────────────────────►│                      │                  │
  │ {username, password,   │                      │                  │
  │  email}               │                      │                  │
  │                       │                      │                  │
  │                       │ ValidateInput()      │                  │
  │                       ├────────────┐         │                  │
  │                       │            │         │                  │
  │                       │◄───────────┘         │                  │
  │                       │                      │                  │
  │                       │ RegisterUserAsync()  │                  │
  │                       ├─────────────────────►│                  │
  │                       │                      │                  │
  │                       │                      │ GetUserByUsername│
  │                       │                      ├─────────────────►│
  │                       │                      │                  │
  │                       │                      │ null (不存在)     │
  │                       │                      │◄─────────────────┤
  │                       │                      │                  │
  │                       │                      │ GetUserByEmail   │
  │                       │                      ├─────────────────►│
  │                       │                      │                  │
  │                       │                      │ null (不存在)     │
  │                       │                      │◄─────────────────┤
  │                       │                      │                  │
  │                       │                      │ CreateUserAsync  │
  │                       │                      ├─────────────────►│
  │                       │                      │ (user, password) │
  │                       │                      │                  │
  │                       │                      │ UserStorageDto   │
  │                       │                      │◄─────────────────┤
  │                       │                      │                  │
  │                       │ UserStorageDto       │                  │
  │                       │◄─────────────────────┤                  │
  │                       │                      │                  │
  │                       │ GenerateTokens()     │                  │
  │                       ├───────────┐          │                  │
  │                       │           │          │                  │
  │                       │◄──────────┘          │                  │
  │                       │                      │                  │
  │ AuthenticationResponse│                      │                  │
  │◄──────────────────────┤                      │                  │
  │                       │                      │                  │
```

## 用户-角色关联关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                   用户-角色关联管理                                │
└─────────────────────────────────────────────────────────────────┘

        User                UserCharacterRelation           Character
    ┌──────────┐                ┌──────────┐              ┌──────────┐
    │ user-001 │───────────────►│relation-1│◄─────────────│ char-001 │
    │          │                 │          │              │ "战士"   │
    └──────────┘                 │UserId    │              └──────────┘
        │                        │CharacterId              
        │                        │IsDefault:true           
        │                        └──────────┘              
        │                                                   
        │                        ┌──────────┐              ┌──────────┐
        └───────────────────────►│relation-2│◄─────────────│ char-002 │
                                 │          │              │ "法师"   │
                                 │UserId    │              └──────────┘
                                 │CharacterId              
                                 │IsDefault:false          
                                 └──────────┘              

关联操作:
- CreateUserCharacterAsync(): 创建新关联
- GetUserCharactersAsync(userId): 获取用户所有角色
- GetCharacterOwnerAsync(characterId): 获取角色拥有者
- UserOwnsCharacterAsync(userId, characterId): 验证所有权
- SetDefaultCharacterAsync(userId, characterId): 设置默认
- DeleteUserCharacterAsync(userId, characterId): 删除关联
```

## 接口依赖关系图

```
┌─────────────────────────────────────────────────────────────────┐
│                     接口依赖关系                                  │
└─────────────────────────────────────────────────────────────────┘

    IAuthApi ────────────┐
    (客户端接口)          │
                         │
                         ▼
                 AuthController ───────► UserService
                 (HTTP端点)              (业务逻辑)
                         │                    │
                         │                    │
                         │                    ▼
                         │           IDataStorageService
                         │           (数据访问接口)
                         │                    │
                         │         ┌──────────┴──────────┐
                         │         │                     │
                         │         ▼                     ▼
                         │  DataStorageService   SqliteDataStorageService
                         │  (内存实现)            (SQLite实现)
                         │
                         ▼
             GameAuthenticationService
             (JWT令牌管理)
```

---

**文档说明**:
- 本文档提供了用户账户系统的可视化架构和关系图
- 所有图表使用ASCII字符绘制，便于在任何文本编辑器中查看
- 配合其他文档使用，可以全面了解用户账户系统的设计和实现

**相关文档**:
- `USER_ACCOUNT_INTERFACES_ANALYSIS.md` - 详细接口分析
- `USER_ACCOUNT_INTERFACES_QUICKREF.md` - 快速参考手册
