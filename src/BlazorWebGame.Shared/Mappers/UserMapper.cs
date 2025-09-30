using BlazorWebGame.Shared.DTOs;
using BlazorWebGame.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Mappers
{
    /// <summary>
    /// �û�ģ��ӳ�乤���� - ������ģ�ͺ�DTO֮��ת��
    /// </summary>
    public static class UserMapper
    {
        /// <summary>
        /// ���û�DTOת��Ϊ����ģ��
        /// </summary>
        public static User ToUser(this UserStorageDto dto)
        {
            // ʹ�ù������������û�������ֱ��ʹ�ù��캯��
            var user = UserFactory.CreateFromStorage(
                dto.Id,
                dto.Username,
                dto.Email,
                dto.IsActive,
                dto.EmailVerified,
                dto.CreatedAt,
                dto.UpdatedAt);

            // ���õ�¼��Ϣ
            if (dto.LastLoginAt > DateTime.MinValue && !string.IsNullOrEmpty(dto.LastLoginIp))
            {
                UserFactory.SetLastLoginInfo(user, dto.LastLoginAt, dto.LastLoginIp);
            }

            // ���ð�ȫ��Ϣ
            if (dto.Roles != null && dto.Roles.Any())
            {
                user.Security.Roles.Clear();
                foreach (var role in dto.Roles)
                {
                    user.Security.AddRole(role);
                }
            }

            // ���õ�¼ʧ�ܳ��Դ���������״̬
            var securityType = typeof(UserSecurity);
            var loginAttemptsField = securityType.GetField("LoginAttempts", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (loginAttemptsField != null) loginAttemptsField.SetValue(user.Security, dto.LoginAttempts);

            var lockedUntilField = securityType.GetField("LockedUntil", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (lockedUntilField != null) lockedUntilField.SetValue(user.Security, dto.LockedUntil);

            // ���õ�¼��ʷ��¼
            if (dto.LoginHistory != null && dto.LoginHistory.Any())
            {
                var loginHistoryField = securityType.GetField("LoginHistory", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (loginHistoryField != null) loginHistoryField.SetValue(user.Security, new List<string>(dto.LoginHistory));
            }

            // ��������������ʱ��
            if (dto.LastPasswordChange.HasValue)
            {
                var lastPasswordChangeField = securityType.GetField("LastPasswordChange", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (lastPasswordChangeField != null) lastPasswordChangeField.SetValue(user.Security, dto.LastPasswordChange);
            }

            // �����û���������
            user.UpdateProfile(dto.DisplayName, dto.Avatar);

            // ����Զ�������
            if (dto.CustomProperties != null)
            {
                foreach (var prop in dto.CustomProperties)
                {
                    user.Profile.SetCustomProperty(prop.Key, prop.Value);
                }
            }

            // ��ӽ�ɫID
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
        /// ������ģ��ת��Ϊ�û�DTO
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
                // PasswordHash �� PasswordSalt ��Ҫ�����ݲ㵥������
            };

            return dto;
        }
    }

    /// <summary>
    /// �û������� - ���ڴ����û�ʵ�����������
    /// </summary>
    internal static class UserFactory
    {
        /// <summary>
        /// �Ӵ洢���ݴ����û�
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
            // �����û�ʵ��
            var user = new User(username, email);

            // ��������˽���ֶ�
            var idField = typeof(User).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (idField != null) idField.SetValue(user, id);

            var createdAtField = typeof(User).GetField("_createdAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (createdAtField != null) createdAtField.SetValue(user, createdAt);

            var updatedAtField = typeof(User).GetField("_updatedAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (updatedAtField != null) updatedAtField.SetValue(user, updatedAt);

            // ����״̬
            if (isActive) user.Activate(); else user.Deactivate();
            if (emailVerified) user.VerifyEmail();

            return user;
        }

        /// <summary>
        /// ��������¼��Ϣ
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