using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BlazorWebGame.Shared.DTOs;

/// <summary>
/// 认证响应DTO
/// </summary>
public class AuthenticationResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// 角色列表项DTO
/// </summary>
public class CharacterListItemDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    // 可以添加更多角色信息
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
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// 个人资料更新请求
/// </summary>
public class ProfileUpdateRequest
{
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
}

/// <summary>
/// 密码更新请求
/// </summary>
public class PasswordUpdateRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "新密码长度至少6个字符")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
    public string ConfirmPassword { get; set; } = string.Empty;
}