using Microsoft.AspNetCore.Mvc;
using BlazorWebGame.Server.Security;
using BlazorWebGame.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace BlazorWebGame.Server.Controllers;

/// <summary>
/// 身份验证控制器，处理用户登录、注册和令牌刷新
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly GameAuthenticationService _authService;
    private readonly DemoUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        GameAuthenticationService authService, 
        DemoUserService userService,
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
    public ActionResult<ApiResponse<AuthenticationResponse>> Login(LoginRequest request)
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
            var user = _userService.ValidateUser(request.Username, request.Password);
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

            // 生成令牌
            var accessToken = _authService.GenerateAccessToken(user.Id, user.Username, user.Roles);
            var refreshToken = _authService.GenerateRefreshToken();

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
                    Roles = user.Roles
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
    /// 用户注册（演示功能，生产环境需要更严格的验证）
    /// </summary>
    [HttpPost("register")]
    public ActionResult<ApiResponse<AuthenticationResponse>> Register(RegisterRequest request)
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

            // 对于演示，直接返回成功，生产环境需要实际创建用户
            var userId = $"user-{Guid.NewGuid():N}";
            var accessToken = _authService.GenerateAccessToken(userId, request.Username, new List<string> { "Player" });
            var refreshToken = _authService.GenerateRefreshToken();

            _logger.LogInformation("User {Username} (ID: {UserId}) registered successfully from IP: {ClientIp}", 
                request.Username, userId, GetClientIpAddress());

            return Ok(new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                Data = new AuthenticationResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserId = userId,
                    Username = request.Username,
                    Roles = new List<string> { "Player" }
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
    public ActionResult<ApiResponse<AuthenticationResponse>> RefreshToken(RefreshTokenRequest request)
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

            var user = _userService.GetUserById(request.UserId);
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
            var accessToken = _authService.GenerateAccessToken(user.Id, user.Username, user.Roles);
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
                    Roles = user.Roles
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
    public ActionResult<ApiResponse<UserInfo>> GetCurrentUser()
    {
        try
        {
            var userId = _authService.GetUserId(User);
            var username = _authService.GetUsername(User);
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
            {
                return Unauthorized(new ApiResponse<UserInfo>
                {
                    Success = false,
                    Message = "Invalid token",
                    Timestamp = DateTime.UtcNow
                });
            }

            var roles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Ok(new ApiResponse<UserInfo>
            {
                Success = true,
                Data = new UserInfo
                {
                    UserId = userId,
                    Username = username,
                    Roles = roles
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
            // 为演示创建一个临时用户
            var demoUserId = $"demo-user-{Guid.NewGuid():N}";
            var demoUsername = "DemoUser";
            var roles = new List<string> { "Player", "Tester" };

            // 生成访问令牌
            var accessToken = _authService.GenerateAccessToken(demoUserId, demoUsername, roles);
            var refreshToken = _authService.GenerateRefreshToken();

            _logger.LogInformation("Demo login successful for temporary user {UserId} from IP: {ClientIp}", 
                demoUserId, GetClientIpAddress());

            return Ok(new
            {
                success = true,
                token = accessToken,
                refreshToken = refreshToken,
                userId = demoUserId,
                username = demoUsername,
                roles = roles,
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

// 请求和响应模型
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class AuthenticationResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}