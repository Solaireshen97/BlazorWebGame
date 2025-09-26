using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BlazorWebGame.Server.Security;

/// <summary>
/// 游戏身份验证服务，处理JWT令牌生成和验证
/// </summary>
public class GameAuthenticationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GameAuthenticationService> _logger;
    private readonly SecurityKey _signingKey;

    public GameAuthenticationService(IConfiguration configuration, ILogger<GameAuthenticationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    }

    /// <summary>
    /// 生成JWT访问令牌
    /// </summary>
    public string GenerateAccessToken(string userId, string username, List<string>? roles = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
            new("UserId", userId),
            new("Username", username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // 添加角色声明
        if (roles != null && roles.Any())
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _logger.LogInformation("Access token generated for user {UserId}", userId);
        return tokenString;
    }

    /// <summary>
    /// 验证并解析JWT令牌
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token validation failed: {Error}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }

    /// <summary>
    /// 验证用户是否拥有指定角色的权限
    /// </summary>
    public bool HasRole(ClaimsPrincipal user, string role)
    {
        return user.IsInRole(role);
    }

    /// <summary>
    /// 验证用户是否可以访问指定资源
    /// </summary>
    public bool CanAccessResource(ClaimsPrincipal user, string resourceOwnerId)
    {
        var userId = user.FindFirst("UserId")?.Value;
        
        // 用户只能访问自己的资源，除非是管理员
        return userId == resourceOwnerId || user.IsInRole("Admin");
    }

    /// <summary>
    /// 从声明中提取用户ID
    /// </summary>
    public string? GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst("UserId")?.Value;
    }

    /// <summary>
    /// 从声明中提取用户名
    /// </summary>
    public string? GetUsername(ClaimsPrincipal user)
    {
        return user.FindFirst("Username")?.Value;
    }

    /// <summary>
    /// 检查令牌是否即将过期（5分钟内）
    /// </summary>
    public bool IsTokenNearExpiry(ClaimsPrincipal user)
    {
        var expClaim = user.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (expClaim == null) return true;

        if (long.TryParse(expClaim, out var exp))
        {
            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp);
            return expirationTime.Subtract(DateTimeOffset.UtcNow).TotalMinutes <= 5;
        }

        return true;
    }

    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// 获取令牌验证参数
    /// </summary>
    private TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.FromMinutes(1) // 允许1分钟的时钟偏差
        };
    }

    /// <summary>
    /// 获取令牌过期时间（分钟）
    /// </summary>
    private int GetTokenExpirationMinutes()
    {
        return _configuration.GetValue<int>("Jwt:ExpireMinutes", 60);
    }
}

/// <summary>
/// 临时用户服务，用于演示认证功能
/// 生产环境中应该连接到真实的用户数据库
/// </summary>
public class DemoUserService
{
    private readonly ILogger<DemoUserService> _logger;
    private readonly Dictionary<string, DemoUser> _users;

    public DemoUserService(ILogger<DemoUserService> logger)
    {
        _logger = logger;
        
        // 创建一些演示用户
        _users = new Dictionary<string, DemoUser>
        {
            ["demo"] = new DemoUser 
            { 
                Id = "demo-user-001", 
                Username = "demo", 
                Password = "demo123", 
                Roles = new List<string> { "Player" } 
            },
            ["admin"] = new DemoUser 
            { 
                Id = "admin-user-001", 
                Username = "admin", 
                Password = "admin123", 
                Roles = new List<string> { "Admin", "Player" } 
            },
            ["player1"] = new DemoUser 
            { 
                Id = "player-001", 
                Username = "player1", 
                Password = "player123", 
                Roles = new List<string> { "Player" } 
            }
        };
    }

    /// <summary>
    /// 验证用户凭据
    /// </summary>
    public DemoUser? ValidateUser(string username, string password)
    {
        if (_users.TryGetValue(username.ToLower(), out var user) && user.Password == password)
        {
            _logger.LogInformation("User {Username} authenticated successfully", username);
            return user;
        }

        _logger.LogWarning("Authentication failed for user {Username}", username);
        return null;
    }

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    public DemoUser? GetUserById(string userId)
    {
        return _users.Values.FirstOrDefault(u => u.Id == userId);
    }

    /// <summary>
    /// 检查用户是否拥有角色
    /// </summary>
    public bool UserHasCharacter(string userId, string characterId)
    {
        var user = GetUserById(userId);
        if (user == null) return false;

        // Admin users can access any character
        if (user.Roles.Contains("Admin")) return true;

        // Map demo users to test characters for testing purposes
        var userCharacterMappings = new Dictionary<string, string[]>
        {
            ["demo-user-001"] = new[] { "test-character-1", "test-character-2" },
            ["player-001"] = new[] { "test-character-1" },
            ["admin-user-001"] = new[] { "test-character-1", "test-character-2" }
        };

        // Check if user has access to the specified character
        if (userCharacterMappings.TryGetValue(userId, out var allowedCharacters))
        {
            return allowedCharacters.Contains(characterId);
        }

        // Fallback: check if character ID starts with user ID (for dynamically created characters)
        return characterId.StartsWith(userId);
    }
}

/// <summary>
/// 演示用户模型
/// </summary>
public class DemoUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}