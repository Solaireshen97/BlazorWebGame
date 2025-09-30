using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 事件链系统 - 用于复杂连锁反应
/// </summary>
public class EventChain
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Name { get; private set; } = string.Empty;
    public List<EventChainNode> Nodes { get; private set; } = new();
    public Dictionary<string, object> Context { get; private set; } = new();

    public EventChain(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    public void AddNode(EventChainNode node)
    {
        Nodes.Add(node);
    }

    /// <summary>
    /// 执行事件链
    /// </summary>
    public EventChainResult Execute(IGameContext gameContext)
    {
        var result = new EventChainResult();

        foreach (var node in Nodes)
        {
            if (!EvaluateCondition(node.Condition, Context))
                continue;

            var nodeResult = node.Execute(gameContext, Context);
            result.ExecutedNodes.Add(node.Id);

            if (nodeResult.StopChain)
            {
                result.Stopped = true;
                break;
            }

            // 合并上下文
            foreach (var kvp in nodeResult.ContextUpdates)
            {
                Context[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    private bool EvaluateCondition(string? condition, Dictionary<string, object> context)
    {
        if (string.IsNullOrEmpty(condition))
            return true;

        // TODO: 实现条件评估
        return true;
    }
}

/// <summary>
/// 事件链节点
/// </summary>
public class EventChainNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Func<IGameContext, Dictionary<string, object>, NodeExecutionResult>? CustomExecutor { get; set; }

    public NodeExecutionResult Execute(IGameContext context, Dictionary<string, object> chainContext)
    {
        if (CustomExecutor != null)
            return CustomExecutor(context, chainContext);

        // 默认执行逻辑
        return new NodeExecutionResult();
    }
}

/// <summary>
/// 节点执行结果
/// </summary>
public class NodeExecutionResult
{
    public bool Success { get; set; } = true;
    public bool StopChain { get; set; } = false;
    public Dictionary<string, object> ContextUpdates { get; set; } = new();
}

/// <summary>
/// 事件链结果
/// </summary>
public class EventChainResult
{
    public List<string> ExecutedNodes { get; set; } = new();
    public bool Stopped { get; set; }
}