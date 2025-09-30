using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 地图区域系统
/// </summary>
public class MapRegion
{
    public string Id { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int MinLevel { get; private set; } = 1;
    public int MaxLevel { get; private set; } = 10;

    // 怪物池
    public List<MonsterPoolEntry> MonsterPool { get; private set; } = new();

    // 采集节点
    public List<string> GatheringNodeIds { get; private set; } = new();

    // 副本入口
    public List<string> DungeonIds { get; private set; } = new();

    // NPC
    public List<string> NpcIds { get; private set; } = new();

    // 解锁条件
    public string UnlockConditionExpr { get; private set; } = string.Empty;

    // 相邻区域
    public List<string> AdjacentRegionIds { get; private set; } = new();

    // 区域修饰符
    public RegionModifiers Modifiers { get; private set; } = new();

    public MapRegion(string id, string name, int minLevel, int maxLevel)
    {
        Id = id;
        Name = name;
        MinLevel = minLevel;
        MaxLevel = maxLevel;
    }

    /// <summary>
    /// 添加怪物到池
    /// </summary>
    public void AddMonsterToPool(string monsterId, double weight, int minCount = 1, int maxCount = 1)
    {
        MonsterPool.Add(new MonsterPoolEntry
        {
            MonsterId = monsterId,
            Weight = weight,
            MinCount = minCount,
            MaxCount = maxCount
        });
    }

    /// <summary>
    /// 随机选择怪物组
    /// </summary>
    public List<string> SelectMonsterGroup(RNGContext rng)
    {
        var result = new List<string>();
        var totalWeight = MonsterPool.Sum(m => m.Weight);

        foreach (var entry in MonsterPool)
        {
            if (rng.NextDouble() < entry.Weight / totalWeight)
            {
                var count = rng.Next(entry.MinCount, entry.MaxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    result.Add(entry.MonsterId);
                }
            }
        }

        // 保证至少有一个怪物
        if (result.Count == 0 && MonsterPool.Count > 0)
        {
            var fallback = MonsterPool[rng.Next(0, MonsterPool.Count)];
            result.Add(fallback.MonsterId);
        }

        return result;
    }
}

/// <summary>
/// 怪物池条目
/// </summary>
public class MonsterPoolEntry
{
    public string MonsterId { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = 1;
}

/// <summary>
/// 区域修饰符
/// </summary>
public class RegionModifiers
{
    public double ExperienceMultiplier { get; set; } = 1.0;
    public double GoldMultiplier { get; set; } = 1.0;
    public double DropRateMultiplier { get; set; } = 1.0;
    public double GatheringSpeedMultiplier { get; set; } = 1.0;
    public Dictionary<string, double> ResourceBonuses { get; set; } = new();
}

/// <summary>
/// 区域图 - 管理区域关系
/// </summary>
public class RegionGraph
{
    private readonly Dictionary<string, MapRegion> _regions = new();
    private readonly Dictionary<string, HashSet<string>> _adjacency = new();

    public void AddRegion(MapRegion region)
    {
        _regions[region.Id] = region;

        if (!_adjacency.ContainsKey(region.Id))
        {
            _adjacency[region.Id] = new HashSet<string>();
        }

        // 建立双向邻接关系
        foreach (var adjacentId in region.AdjacentRegionIds)
        {
            _adjacency[region.Id].Add(adjacentId);

            if (!_adjacency.ContainsKey(adjacentId))
            {
                _adjacency[adjacentId] = new HashSet<string>();
            }
            _adjacency[adjacentId].Add(region.Id);
        }
    }

    public MapRegion? GetRegion(string regionId)
    {
        return _regions.GetValueOrDefault(regionId);
    }

    public List<MapRegion> GetAdjacentRegions(string regionId)
    {
        if (!_adjacency.ContainsKey(regionId))
            return new List<MapRegion>();

        return _adjacency[regionId]
            .Select(id => _regions.GetValueOrDefault(id))
            .Where(r => r != null)
            .ToList()!;
    }

    /// <summary>
    /// 检查是否可以从一个区域到达另一个区域
    /// </summary>
    public bool CanReach(string fromRegionId, string toRegionId, HashSet<string> unlockedRegions)
    {
        if (fromRegionId == toRegionId)
            return unlockedRegions.Contains(fromRegionId);

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(fromRegionId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!unlockedRegions.Contains(current))
                continue;

            if (current == toRegionId)
                return true;

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            foreach (var adjacent in _adjacency[current])
            {
                if (!visited.Contains(adjacent))
                {
                    queue.Enqueue(adjacent);
                }
            }
        }

        return false;
    }
}