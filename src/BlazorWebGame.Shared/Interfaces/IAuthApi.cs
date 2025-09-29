using BlazorWebGame.Shared.DTOs;

namespace BlazorWebGame.Shared.Interfaces;

/// <summary>
/// 认证系统API接口定义
/// </summary>
public interface IAuthApi
{
    /// <summary>
    /// 用户登录
    /// </summary>
    Task<ApiResponse<string>> LoginAsync(LoginRequest request);

    /// <summary>
    /// 用户注册
    /// </summary>
    Task<ApiResponse<string>> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// 刷新令牌
    /// </summary>
    Task<ApiResponse<string>> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// 用户登出
    /// </summary>
    Task<ApiResponse<bool>> LogoutAsync();

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    Task<ApiResponse<UserInfoDto>> GetCurrentUserAsync();

    /// <summary>
    /// 演示登录（开发用）
    /// </summary>
    Task<ApiResponse<string>> DemoLoginAsync();
}

/// <summary>
/// 登录请求DTO
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 注册请求DTO
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// 刷新令牌请求DTO
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 用户信息DTO
/// </summary>
public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}