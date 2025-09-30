using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 会话认证
/// </summary>
public class SessionAuth
{
    public string SessionId { get; private set; } = Guid.NewGuid().ToString();
    public string UserId { get; private set; } = string.Empty;
    public string CharacterId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;

    public SessionAuth(string userId, string characterId, string ipAddress, TimeSpan? duration = null)
    {
        UserId = userId;
        CharacterId = characterId;
        IpAddress = ipAddress;
        ExpiresAt = DateTime.UtcNow.Add(duration ?? TimeSpan.FromHours(24));
    }

    public bool IsValid()
    {
        return DateTime.UtcNow < ExpiresAt;
    }

    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
        // 可选：延长过期时间
        if ((ExpiresAt - DateTime.UtcNow) < TimeSpan.FromMinutes(30))
        {
            ExpiresAt = DateTime.UtcNow.AddHours(1);
        }
    }
}

/// <summary>
/// 速率限制器
/// </summary>
public class RateLimiter
{
    private readonly Dictionary<string, RateLimitState> _states = new();
    private readonly Dictionary<string, RateLimitConfig> _configs = new();

    public RateLimiter()
    {
        // 初始化默认配置
        _configs["activity_change"] = new RateLimitConfig { MaxRequests = 10, Window = TimeSpan.FromMinutes(1) };
        _configs["skill_change"] = new RateLimitConfig { MaxRequests = 20, Window = TimeSpan.FromMinutes(1) };
        _configs["equipment_change"] = new RateLimitConfig { MaxRequests = 30, Window = TimeSpan.FromMinutes(1) };
        _configs["reroll_affix"] = new RateLimitConfig { MaxRequests = 5, Window = TimeSpan.FromMinutes(1) };
    }

    /// <summary>
    /// 检查是否允许请求
    /// </summary>
    public RateLimitResult CheckLimit(string key, string identifier)
    {
        if (!_configs.ContainsKey(key))
        {
            return new RateLimitResult { Allowed = true };
        }

        var config = _configs[key];
        var stateKey = $"{key}:{identifier}";

        if (!_states.ContainsKey(stateKey))
        {
            _states[stateKey] = new RateLimitState();
        }

        var state = _states[stateKey];
        var now = DateTime.UtcNow;

        // 清理过期的请求记录
        state.RequestTimes.RemoveAll(t => now - t > config.Window);

        if (state.RequestTimes.Count >= config.MaxRequests)
        {
            var oldestRequest = state.RequestTimes[0];
            var resetTime = oldestRequest.Add(config.Window);

            return new RateLimitResult
            {
                Allowed = false,
                RetryAfter = resetTime - now,
                Remaining = 0
            };
        }

        state.RequestTimes.Add(now);

        return new RateLimitResult
        {
            Allowed = true,
            Remaining = config.MaxRequests - state.RequestTimes.Count
        };
    }
}

/// <summary>
/// 速率限制配置
/// </summary>
public class RateLimitConfig
{
    public int MaxRequests { get; set; }
    public TimeSpan Window { get; set; }
}

/// <summary>
/// 速率限制状态
/// </summary>
public class RateLimitState
{
    public List<DateTime> RequestTimes { get; set; } = new();
}

/// <summary>
/// 速率限制结果
/// </summary>
public class RateLimitResult
{
    public bool Allowed { get; set; }
    public int Remaining { get; set; }
    public TimeSpan? RetryAfter { get; set; }
}

/// <summary>
/// 行为异常检测
/// </summary>
public class AnomalyDetector
{
    private readonly Dictionary<string, PlayerBehaviorProfile> _profiles = new();

    /// <summary>
    /// 检查行为异常
    /// </summary>
    public AnomalyCheckResult CheckBehavior(string playerId, BehaviorEvent behaviorEvent)
    {
        if (!_profiles.ContainsKey(playerId))
        {
            _profiles[playerId] = new PlayerBehaviorProfile();
        }

        var profile = _profiles[playerId];
        profile.RecordEvent(behaviorEvent);

        var anomalies = new List<string>();

        // 检查离线收益异常
        if (behaviorEvent.Type == "offline_gain")
        {
            var gain = (double)behaviorEvent.Value;
            if (gain > profile.MaxOfflineGain * 1.5)
            {
                anomalies.Add($"离线收益异常: {gain} (预期最大: {profile.MaxOfflineGain})");
            }
        }

        // 检查DPS异常
        if (behaviorEvent.Type == "damage_dealt")
        {
            var dps = (double)behaviorEvent.Value;
            if (dps > profile.MaxDps * 2.0)
            {
                anomalies.Add($"DPS异常: {dps} (历史最高: {profile.MaxDps})");
            }
        }

        return new AnomalyCheckResult
        {
            HasAnomaly = anomalies.Count > 0,
            Anomalies = anomalies,
            Severity = anomalies.Count > 2 ? "High" : anomalies.Count > 0 ? "Medium" : "None"
        };
    }
}

/// <summary>
/// 玩家行为档案
/// </summary>
public class PlayerBehaviorProfile
{
    public double MaxOfflineGain { get; set; } = 1000;
    public double MaxDps { get; set; } = 100;
    public double AveragePlayTime { get; set; } = 2.0; // hours
    public List<DateTime> LoginTimes { get; set; } = new();

    public void RecordEvent(BehaviorEvent evt)
    {
        switch (evt.Type)
        {
            case "offline_gain":
                MaxOfflineGain = Math.Max(MaxOfflineGain, (double)evt.Value);
                break;
            case "damage_dealt":
                MaxDps = Math.Max(MaxDps, (double)evt.Value);
                break;
        }
    }
}

/// <summary>
/// 行为事件
/// </summary>
public class BehaviorEvent
{
    public string Type { get; set; } = string.Empty;
    public object Value { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 异常检查结果
/// </summary>
public class AnomalyCheckResult
{
    public bool HasAnomaly { get; set; }
    public List<string> Anomalies { get; set; } = new();
    public string Severity { get; set; } = "None";
}