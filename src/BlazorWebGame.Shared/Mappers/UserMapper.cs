using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Mappers
{
    /// <summary>
    /// 用户模型映射工具类 - 在领域模型和DTO之间转换
    /// </summary>
    public static class UserMapper
    {
        /// <summary>
        /// 将用户DTO转换为领域模型
        /// </summary>
        public static User ToUser(this UserStorageDto dto)
        {
            // 使用工厂方法创建用户而不是直接使用构造函数
            var user = UserFactory.CreateFromStorage(
                dto.Id,
                dto.Username,
                dto.Email,
                dto.IsActive,
                dto.EmailVerified,
                dto.CreatedAt,
                dto.UpdatedAt);

            // 设置登录信息
            if (dto.LastLoginAt > DateTime.MinValue && !string.IsNullOrEmpty(dto.LastLoginIp))
            {
                UserFactory.SetLastLoginInfo(user, dto.LastLoginAt, dto.LastLoginIp);
            }

            // 设置安全信息
            if (dto.Roles != null && dto.Roles.Any())
            {
                user.Security.Roles.Clear();
                foreach (var role in dto.Roles)
                {
                    user.Security.AddRole(role);
                }
            }

            // 设置登录失败尝试次数和锁定状态
            var securityType = typeof(UserSecurity);
            var loginAttemptsField = securityType.GetField("LoginAttempts", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (loginAttemptsField != null) loginAttemptsField.SetValue(user.Security, dto.LoginAttempts);

            var lockedUntilField = securityType.GetField("LockedUntil", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (lockedUntilField != null) lockedUntilField.SetValue(user.Security, dto.LockedUntil);

            // 设置登录历史记录
            if (dto.LoginHistory != null && dto.LoginHistory.Any())
            {
                var loginHistoryField = securityType.GetField("LoginHistory", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (loginHistoryField != null) loginHistoryField.SetValue(user.Security, new List<string>(dto.LoginHistory));
            }

            // 设置最后密码更改时间
            if (dto.LastPasswordChange.HasValue)
            {
                var lastPasswordChangeField = securityType.GetField("LastPasswordChange", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (lastPasswordChangeField != null) lastPasswordChangeField.SetValue(user.Security, dto.LastPasswordChange);
            }

            // 设置用户个人资料
            user.UpdateProfile(dto.DisplayName, dto.Avatar);

            // 添加自定义属性
            if (dto.CustomProperties != null)
            {
                foreach (var prop in dto.CustomProperties)
                {
                    user.Profile.SetCustomProperty(prop.Key, prop.Value);
                }
            }

            // 添加角色ID
            if (dto.CharacterIds != null)
            {
                foreach (var charId in dto.CharacterIds)
                {
                    user.AddCharacter(charId);
                }
            }

            return user;
        }

        /// <summary>
        /// 将领域模型转换为用户DTO
        /// </summary>
        public static UserStorageDto ToDto(this User user)
        {
            var dto = new UserStorageDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                EmailVerified = user.EmailVerified,
                LastLoginAt = user.LastLoginAt,
                LastLoginIp = user.LastLoginIp,
                LoginAttempts = user.Security.LoginAttempts,
                LockedUntil = user.Security.LockedUntil,
                LastPasswordChange = user.Security.LastPasswordChange,
                LoginHistory = new List<string>(user.Security.LoginHistory),
                Roles = new List<string>(user.Security.Roles),
                DisplayName = user.Profile.DisplayName,
                Avatar = user.Profile.Avatar,
                CustomProperties = new Dictionary<string, object>(user.Profile.CustomProperties),
                CharacterIds = new List<string>(user.CharacterIds),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
                // PasswordHash 和 PasswordSalt 需要在数据层单独处理
            };

            return dto;
        }
    }

    /// <summary>
    /// 用户工厂类 - 用于创建用户实例的特殊情况
    /// </summary>
    internal static class UserFactory
    {
        /// <summary>
        /// 从存储数据创建用户
        /// </summary>
        internal static User CreateFromStorage(
            string id,
            string username,
            string email,
            bool isActive,
            bool emailVerified,
            DateTime createdAt,
            DateTime updatedAt)
        {
            // 创建用户实例
            var user = new User(username, email);

            // 反射设置私有字段
            var idField = typeof(User).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (idField != null) idField.SetValue(user, id);

            var createdAtField = typeof(User).GetField("_createdAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (createdAtField != null) createdAtField.SetValue(user, createdAt);

            var updatedAtField = typeof(User).GetField("_updatedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updatedAtField != null) updatedAtField.SetValue(user, updatedAt);

            // 设置状态
            if (isActive) user.Activate(); else user.Deactivate();
            if (emailVerified) user.VerifyEmail();

            return user;
        }

        /// <summary>
        /// 设置最后登录信息
        /// </summary>
        internal static void SetLastLoginInfo(User user, DateTime lastLoginAt, string lastLoginIp)
        {
            var lastLoginAtField = typeof(User).GetField("_lastLoginAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lastLoginAtField != null) lastLoginAtField.SetValue(user, lastLoginAt);

            var lastLoginIpField = typeof(User).GetField("_lastLoginIp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (lastLoginIpField != null) lastLoginIpField.SetValue(user, lastLoginIp);
        }
    }
}