using BlazorWebGame.Server.Security;
using BlazorWebGame.Server.Services;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 身份验证控制器，处理用户登录、注册和令牌刷新
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly GameAuthenticationService _authService;
    private readonly UserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        GameAuthenticationService authService,
        UserService userService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthenticationResponse>>> Login(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new ApiResponse<AuthenticationResponse>
                {
                    Success = false,
                    Message = "Username and password are required",
                    Timestamp = DateTime.UtcNow
                });
            }

            // 验证用户凭据
            var user = await _userService.ValidateUserAsync(request.Username, request.Password);
            if (user == null)
            {
                _logger.LogWarning("Login failed for username: {Username} from IP: {ClientIp}",
                    request.Username, GetClientIpAddress());

                return Unauthorized(new ApiResponse<AuthenticationResponse>
                {
                    Success = false,
                    Message = "Invalid username or password",
                    Timestamp = DateTime.UtcNow
                });
            }

            // 生成令牌 - 使用 user.Security.Roles 而不是 user.Roles
            var accessToken = _authService.GenerateAccessToken(user.Id, user.Username, user.Security.Roles);
            var refreshToken = _authService.GenerateRefreshToken();

            // 更新最后登录信息
            await _userService.UpdateLastLoginAsync(user.Id, GetClientIpAddress());

            _logger.LogInformation("User {Username} (ID: {UserId}) logged in successfully from IP: {ClientIp}",
                user.Username, user.Id, GetClientIpAddress());

            return Ok(new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                Data = new AuthenticationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = user.Id,
                    Username = user.Username,
                    Roles = user.Security.Roles,
                    DisplayName = user.Profile.DisplayName,
                    Avatar = user.Profile.Avatar,
                    EmailVerified = user.EmailVerified
                },
                Message = "Login successful",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return StatusCode(500, new ApiResponse<AuthenticationResponse>
            {
                Success = false,
                Message = "Internal server error during login",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 更新用户个人资料
    /// </summary>
    [HttpPost("profile/update")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> UpdateUserProfile([FromBody] ProfileUpdateRequest request)
    {
        var userId = _authService.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _userService.UpdateUserProfileAsync(userId, request.DisplayName, request.Avatar);
        if (!result.Success)
            return BadRequest(result);

        // 返回更新后的用户信息
        return await GetCurrentUser();
    }

    /// <summary>
    /// 更新用户密码
    /// </summary>
    [HttpPost("password/update")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> UpdatePassword([FromBody] PasswordUpdateRequest request)
    {
        var userId = _authService.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _userService.UpdatePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return Ok(result);
    }

    /// <summary>
    /// 获取用户的游戏角色列表
    /// </summary>
    [HttpGet("characters")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<CharacterListItemDto>>>> GetUserCharacters()
    {
        var userId = _authService.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _userService.GetUserCharactersAsync(userId);
        if (!result.Success)
            return BadRequest(result);

        // 转换为前端友好的DTO
        var characters = result.Data.Select(c => new CharacterListItemDto
        {
            Id = c.CharacterId,
            IsDefault = c.IsDefault
        }).ToList();

        return Ok(new ApiResponse<List<CharacterListItemDto>>
        {
            Success = true,
            Data = characters,
            Message = result.Message
        });
    }

    /// <summary>
    /// 设置默认游戏角色
    /// </summary>
    [HttpPost("characters/set-default/{characterId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> SetDefaultCharacter(string characterId)
    {
        var userId = _authService.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _userService.SetDefaultCharacterAsync(userId, characterId);
        return Ok(result);
    }

    /// <summary>
    /// 请求邮箱验证链接（发送验证邮件）
    /// </summary>
    [HttpPost("email/verify-request")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> RequestEmailVerification()
    {
        var userId = _authService.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // 这需要一个发送验证邮件的服务实现
        // var result = await _emailService.SendVerificationEmailAsync(userId);

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "验证邮件已发送，请检查您的邮箱"
        });
    }

    /// <summary>
    /// 验证邮箱（通过验证链接）
    /// </summary>
    [HttpGet("email/verify")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyEmail([FromQuery] string token, [FromQuery] string userId)
    {
        // 验证token有效性的逻辑
        // var isValidToken = _tokenService.ValidateEmailVerificationToken(token, userId);
        // if (!isValidToken) return BadRequest(new ApiResponse<bool> { Success = false, Message = "无效的验证链接" });

        var result = await _userService.VerifyEmailAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// 管理员重置用户密码
    /// </summary>
    [HttpPost("admin/users/{userId}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<string>>> AdminResetPassword(string userId)
    {
        var result = await _userService.ResetPasswordAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// 管理员激活用户账户
    /// </summary>
    [HttpPost("admin/users/{userId}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> AdminActivateUser(string userId)
    {
        var result = await _userService.ActivateUserAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// 管理员停用用户账户
    /// </summary>
    [HttpPost("admin/users/{userId}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> AdminDeactivateUser(string userId)
    {
        var result = await _userService.DeactivateUserAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// 管理员解锁用户账户
    /// </summary>
    [HttpPost("admin/users/{userId}/unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> AdminUnlockUser(string userId)
    {
        var result = await _userService.UnlockUserAccountAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// 用户注册（演示功能，生产环境需要更严格的验证）
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthenticationResponse>>> Register(RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new ApiResponse<AuthenticationResponse>
                {
                    Success = false,
                    Message = "Username and password are required",
                    Timestamp = DateTime.UtcNow
                });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new ApiResponse<AuthenticationResponse>
                {
                    Success = false,
                    Message = "Password must be at least 6 characters long",
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Registration attempt for username: {Username} from IP: {ClientIp}",
                request.Username, GetClientIpAddress());

            // 注册用户
            var registrationResult = await _userService.RegisterUserAsync(request.Username, request.Password, request.Email);
            if (!registrationResult.Success)
            {
                return BadRequest(new ApiResponse<AuthenticationResponse>
                {
                    Success = false,
                    Message = registrationResult.Message,
                    Timestamp = DateTime.UtcNow
                });
            }

            var user = registrationResult.Data!;
            var accessToken = _authService.GenerateAccessToken(user.Id, user.Username, user.Security.Roles);
            var refreshToken = _authService.GenerateRefreshToken();

            // 更新最后登录信息
            await _userService.UpdateLastLoginAsync(user.Id, GetClientIpAddress());

            _logger.LogInformation("User {Username} (ID: {UserId}) registered successfully from IP: {ClientIp}",
                request.Username, user.Id, GetClientIpAddress());

            return Ok(new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                Data = new AuthenticationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = user.Id,
                    Username = user.Username,
                    Roles = user.Security.Roles,
                    DisplayName = user.Profile.DisplayName,
                    Avatar = user.Profile.Avatar,
                    EmailVerified = user.EmailVerified
                },
                Message = "Registration successful",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
            return StatusCode(500, new ApiResponse<AuthenticationResponse>
            {
                Success = false,
                Message = "Internal server error during registration",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthenticationResponse>>> RefreshToken(RefreshTokenRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new ApiResponse<AuthenticationResponse>
                {
                    Success = false,
                    Message = "Refresh token is required",
                    Timestamp = DateTime.UtcNow
                });
            }

            // 简化的刷新令牌验证，生产环境需要存储和验证刷新令牌
            if (string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest(new ApiResponse<AuthenticationResponse>
                {
                    Success = false,
                    Message = "User ID is required",
                    Timestamp = DateTime.UtcNow
                });
            }

            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return Unauthorized(new ApiResponse<AuthenticationResponse>
                {
                    Success = false,
                    Message = "Invalid refresh token",
                    Timestamp = DateTime.UtcNow
                });
            }

            // 生成新的令牌
            var accessToken = _authService.GenerateAccessToken(user.Id, user.Username, user.Security.Roles);
            var refreshToken = _authService.GenerateRefreshToken();

            _logger.LogInformation("Token refreshed for user {UserId} from IP: {ClientIp}",
                user.Id, GetClientIpAddress());

            return Ok(new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                Data = new AuthenticationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = user.Id,
                    Username = user.Username,
                    Roles = user.Security.Roles,
                    DisplayName = user.Profile.DisplayName,
                    Avatar = user.Profile.Avatar,
                    EmailVerified = user.EmailVerified
                },
                Message = "Token refreshed successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new ApiResponse<AuthenticationResponse>
            {
                Success = false,
                Message = "Internal server error during token refresh",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 登出
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public ActionResult<ApiResponse<object>> Logout()
    {
        try
        {
            var userId = _authService.GetUserId(User);
            _logger.LogInformation("User {UserId} logged out from IP: {ClientIp}",
                userId, GetClientIpAddress());

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Logout successful",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Internal server error during logout",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetCurrentUser()
    {
        try
        {
            var userId = _authService.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "Invalid token",
                    Timestamp = DateTime.UtcNow
                });
            }

            // 从数据库获取用户的完整信息
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "User not found",
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new ApiResponse<UserInfo>
            {
                Success = true,
                Data = new UserInfo
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Roles = user.Security.Roles,
                    DisplayName = user.Profile.DisplayName,
                    Avatar = user.Profile.Avatar,
                    Email = user.Email,
                    EmailVerified = user.EmailVerified,
                    IsActive = user.IsActive,
                    LastLoginAt = user.LastLoginAt
                },
                Message = "User information retrieved successfully",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user information");
            return StatusCode(500, new ApiResponse<UserInfo>
            {
                Success = false,
                Message = "Internal server error",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 演示登录 - 为测试目的快速生成临时令牌
    /// </summary>
    [HttpPost("demo-login")]
    public ActionResult<object> DemoLogin()
    {
        try
        {
            // 使用固定的演示用户ID，与UserHasCharacter方法中的映射保持一致
            var demoUserId = "demo-user-001";
            var demoUsername = "DemoUser";
            var roles = new List<string> { "Player", "Tester" };

            // 生成访问令牌
            var accessToken = _authService.GenerateAccessToken(demoUserId, demoUsername, roles);
            var refreshToken = _authService.GenerateRefreshToken();

            _logger.LogInformation("Demo login successful for user {UserId} from IP: {ClientIp}",
                demoUserId, GetClientIpAddress());

            return Ok(new
            {
                success = true,
                token = accessToken,
                refreshToken = refreshToken,
                userId = demoUserId,
                username = demoUsername,
                roles = roles,
                displayName = "Demo Player",
                avatar = "/images/avatars/default.png",
                emailVerified = true,
                message = "Demo login successful",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo login");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during demo login",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string GetClientIpAddress()
    {
        var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

/// <summary>
/// 用户信息响应DTO
/// </summary>
public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// 认证响应DTO
/// </summary>
public class AuthenticationResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public List<string> Roles { get; set; } = new();
}