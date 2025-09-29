using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 任务领域模型
/// </summary>
public class Quest
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public QuestType Type { get; private set; } = QuestType.Main;
    public QuestStatus Status { get; private set; } = QuestStatus.Available;
    public int RequiredLevel { get; private set; } = 1;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    
    // 任务目标
    private readonly List<QuestObjective> _objectives = new();
    public IReadOnlyList<QuestObjective> Objectives => _objectives.AsReadOnly();
    
    // 任务奖励
    private readonly List<QuestReward> _rewards = new();
    public IReadOnlyList<QuestReward> Rewards => _rewards.AsReadOnly();
    
    // 前置任务
    private readonly List<string> _prerequisites = new();
    public IReadOnlyList<string> Prerequisites => _prerequisites.AsReadOnly();

    // 私有构造函数，用于反序列化
    private Quest() { }

    /// <summary>
    /// 创建新任务
    /// </summary>
    public Quest(string name, string description, QuestType type = QuestType.Main, int requiredLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("任务名称不能为空", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        RequiredLevel = Math.Max(1, requiredLevel);
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 添加任务目标
    /// </summary>
    public void AddObjective(string description, QuestObjectiveType type, string targetId, int requiredCount = 1)
    {
        var objective = new QuestObjective(description, type, targetId, requiredCount);
        _objectives.Add(objective);
    }

    /// <summary>
    /// 添加奖励
    /// </summary>
    public void AddReward(QuestRewardType type, int amount, string? itemId = null, string? professionType = null)
    {
        var reward = new QuestReward(type, amount, itemId, professionType);
        _rewards.Add(reward);
    }

    /// <summary>
    /// 添加前置任务
    /// </summary>
    public void AddPrerequisite(string questId)
    {
        if (!string.IsNullOrWhiteSpace(questId) && !_prerequisites.Contains(questId))
        {
            _prerequisites.Add(questId);
        }
    }

    /// <summary>
    /// 设置过期时间
    /// </summary>
    public void SetExpirationTime(DateTime expiresAt)
    {
        if (expiresAt > DateTime.UtcNow)
        {
            ExpiresAt = expiresAt;
        }
    }

    /// <summary>
    /// 激活任务
    /// </summary>
    public bool Activate()
    {
        if (Status != QuestStatus.Available)
            return false;

        Status = QuestStatus.Active;
        return true;
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    public bool Complete()
    {
        if (Status != QuestStatus.Active || !IsAllObjectivesCompleted())
            return false;

        Status = QuestStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// 放弃任务
    /// </summary>
    public bool Abandon()
    {
        if (Status != QuestStatus.Active)
            return false;

        Status = QuestStatus.Available;
        // 重置所有目标进度
        foreach (var objective in _objectives)
        {
            objective.ResetProgress();
        }
        return true;
    }

    /// <summary>
    /// 更新目标进度
    /// </summary>
    public bool UpdateObjectiveProgress(string objectiveId, int progress)
    {
        var objective = _objectives.FirstOrDefault(o => o.Id == objectiveId);
        if (objective == null) return false;

        objective.UpdateProgress(progress);
        
        // 检查是否所有目标都完成
        if (IsAllObjectivesCompleted() && Status == QuestStatus.Active)
        {
            Status = QuestStatus.ReadyToComplete;
        }
        
        return true;
    }

    /// <summary>
    /// 检查是否所有目标都完成
    /// </summary>
    public bool IsAllObjectivesCompleted()
    {
        return _objectives.All(o => o.IsCompleted);
    }

    /// <summary>
    /// 检查任务是否已过期
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    /// <summary>
    /// 获取任务进度百分比
    /// </summary>
    public double GetProgressPercentage()
    {
        if (_objectives.Count == 0) return 0.0;
        
        var totalProgress = _objectives.Sum(o => o.GetProgressPercentage());
        return totalProgress / _objectives.Count;
    }
}

/// <summary>
/// 任务目标
/// </summary>
public class QuestObjective
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Description { get; private set; } = string.Empty;
    public QuestObjectiveType Type { get; private set; } = QuestObjectiveType.Kill;
    public string TargetId { get; private set; } = string.Empty;
    public int RequiredCount { get; private set; } = 1;
    public int CurrentCount { get; private set; } = 0;
    public bool IsCompleted => CurrentCount >= RequiredCount;

    // 私有构造函数，用于反序列化
    private QuestObjective() { }

    /// <summary>
    /// 创建任务目标
    /// </summary>
    public QuestObjective(string description, QuestObjectiveType type, string targetId, int requiredCount = 1)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("目标描述不能为空", nameof(description));

        Description = description.Trim();
        Type = type;
        TargetId = targetId?.Trim() ?? string.Empty;
        RequiredCount = Math.Max(1, requiredCount);
    }

    /// <summary>
    /// 更新进度
    /// </summary>
    public void UpdateProgress(int count)
    {
        CurrentCount = Math.Clamp(count, 0, RequiredCount);
    }

    /// <summary>
    /// 增加进度
    /// </summary>
    public void AddProgress(int increment = 1)
    {
        CurrentCount = Math.Min(RequiredCount, CurrentCount + Math.Max(0, increment));
    }

    /// <summary>
    /// 重置进度
    /// </summary>
    public void ResetProgress()
    {
        CurrentCount = 0;
    }

    /// <summary>
    /// 获取进度百分比
    /// </summary>
    public double GetProgressPercentage()
    {
        return RequiredCount > 0 ? (double)CurrentCount / RequiredCount : 0.0;
    }
}

/// <summary>
/// 任务奖励
/// </summary>
public class QuestReward
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public QuestRewardType Type { get; private set; } = QuestRewardType.Experience;
    public int Amount { get; private set; } = 0;
    public string? ItemId { get; private set; }
    public string? ProfessionType { get; private set; }

    // 私有构造函数，用于反序列化
    private QuestReward() { }

    /// <summary>
    /// 创建任务奖励
    /// </summary>
    public QuestReward(QuestRewardType type, int amount, string? itemId = null, string? professionType = null)
    {
        Type = type;
        Amount = Math.Max(0, amount);
        ItemId = itemId;
        ProfessionType = professionType;
    }

    /// <summary>
    /// 获取奖励描述
    /// </summary>
    public string GetDescription()
    {
        return Type switch
        {
            QuestRewardType.Experience => $"经验值 +{Amount}",
            QuestRewardType.Gold => $"金币 +{Amount}",
            QuestRewardType.Item => $"物品 x{Amount}",
            QuestRewardType.ProfessionExperience => $"{ProfessionType}经验 +{Amount}",
            _ => "未知奖励"
        };
    }
}

/// <summary>
/// 任务类型枚举
/// </summary>
public enum QuestType
{
    Main,           // 主线任务
    Side,           // 支线任务
    Daily,          // 日常任务
    Weekly,         // 周常任务
    Event,          // 活动任务
    Dungeon,        // 副本任务
    PvP,            // PvP任务
    Achievement     // 成就任务
}

/// <summary>
/// 任务状态枚举
/// </summary>
public enum QuestStatus
{
    Available,          // 可接受
    Active,             // 进行中
    ReadyToComplete,    // 可完成
    Completed,          // 已完成
    Failed,             // 失败
    Expired            // 已过期
}

/// <summary>
/// 任务目标类型枚举
/// </summary>
public enum QuestObjectiveType
{
    Kill,           // 击杀
    Collect,        // 收集
    Gather,         // 采集
    Craft,          // 制作
    Deliver,        // 配送
    Talk,           // 对话
    Explore,        // 探索
    Survive,        // 生存
    Escort,         // 护送
    Defend,         // 防御
    Custom          // 自定义
}

/// <summary>
/// 任务奖励类型枚举
/// </summary>
public enum QuestRewardType
{
    Experience,             // 经验值
    Gold,                   // 金币
    Item,                   // 物品
    ProfessionExperience,   // 职业经验
    Reputation,             // 声望
    Title                   // 称号
}

/// <summary>
/// 角色任务状态管理
/// </summary>
public class CharacterQuestManager
{
    private readonly Dictionary<string, Quest> _activeQuests = new();
    private readonly Dictionary<string, Quest> _completedQuests = new();
    private readonly Dictionary<string, Quest> _availableQuests = new();
    public DateTime LastDailyReset { get; private set; } = DateTime.UtcNow.Date;
    public DateTime LastWeeklyReset { get; private set; } = DateTime.UtcNow.Date;

    public IReadOnlyDictionary<string, Quest> ActiveQuests => _activeQuests;
    public IReadOnlyDictionary<string, Quest> CompletedQuests => _completedQuests;
    public IReadOnlyDictionary<string, Quest> AvailableQuests => _availableQuests;

    /// <summary>
    /// 添加可用任务
    /// </summary>
    public void AddAvailableQuest(Quest quest)
    {
        if (quest.Status == QuestStatus.Available && !_availableQuests.ContainsKey(quest.Id))
        {
            _availableQuests[quest.Id] = quest;
        }
    }

    /// <summary>
    /// 接受任务
    /// </summary>
    public bool AcceptQuest(string questId)
    {
        if (!_availableQuests.TryGetValue(questId, out var quest))
            return false;

        if (quest.Activate())
        {
            _availableQuests.Remove(questId);
            _activeQuests[questId] = quest;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 完成任务
    /// </summary>
    public bool CompleteQuest(string questId)
    {
        if (!_activeQuests.TryGetValue(questId, out var quest))
            return false;

        if (quest.Complete())
        {
            _activeQuests.Remove(questId);
            _completedQuests[questId] = quest;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 放弃任务
    /// </summary>
    public bool AbandonQuest(string questId)
    {
        if (!_activeQuests.TryGetValue(questId, out var quest))
            return false;

        if (quest.Abandon())
        {
            _activeQuests.Remove(questId);
            _availableQuests[questId] = quest;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public bool UpdateQuestProgress(string questId, string objectiveId, int progress)
    {
        if (_activeQuests.TryGetValue(questId, out var quest))
        {
            return quest.UpdateObjectiveProgress(objectiveId, progress);
        }

        return false;
    }

    /// <summary>
    /// 检查任务是否已完成
    /// </summary>
    public bool IsQuestCompleted(string questId)
    {
        return _completedQuests.ContainsKey(questId);
    }

    /// <summary>
    /// 检查任务是否激活
    /// </summary>
    public bool IsQuestActive(string questId)
    {
        return _activeQuests.ContainsKey(questId);
    }

    /// <summary>
    /// 获取总完成任务数
    /// </summary>
    public int GetCompletedQuestCount()
    {
        return _completedQuests.Count;
    }

    /// <summary>
    /// 按类型获取完成任务数
    /// </summary>
    public int GetCompletedQuestCount(QuestType questType)
    {
        return _completedQuests.Values.Count(q => q.Type == questType);
    }

    /// <summary>
    /// 处理日常重置
    /// </summary>
    public void ProcessDailyReset()
    {
        var today = DateTime.UtcNow.Date;
        if (LastDailyReset < today)
        {
            LastDailyReset = today;
            
            // 重置日常任务
            var dailyQuests = _completedQuests.Values.Where(q => q.Type == QuestType.Daily).ToList();
            foreach (var quest in dailyQuests)
            {
                _completedQuests.Remove(quest.Id);
                quest.Abandon(); // 重置状态
                _availableQuests[quest.Id] = quest;
            }
        }
    }

    /// <summary>
    /// 处理周常重置
    /// </summary>
    public void ProcessWeeklyReset()
    {
        var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        if (LastWeeklyReset < startOfWeek)
        {
            LastWeeklyReset = startOfWeek;
            
            // 重置周常任务
            var weeklyQuests = _completedQuests.Values.Where(q => q.Type == QuestType.Weekly).ToList();
            foreach (var quest in weeklyQuests)
            {
                _completedQuests.Remove(quest.Id);
                quest.Abandon(); // 重置状态
                _availableQuests[quest.Id] = quest;
            }
        }
    }
}