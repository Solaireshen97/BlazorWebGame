using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BlazorWebGame.Shared.Models;

/// <summary>
/// 条件表达式系统
/// </summary>
public class ConditionExpr
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Raw { get; private set; } = string.Empty;
    public ConditionNode? Ast { get; private set; }
    public HashSet<string> Dependencies { get; private set; } = new();

    public ConditionExpr(string raw)
    {
        Raw = raw;
        Parse();
    }

    private void Parse()
    {
        // 简化的解析示例
        // 实际应该实现完整的DSL解析器
        Ast = ParseExpression(Raw);
        Dependencies = ExtractDependencies(Ast);
    }

    private ConditionNode ParseExpression(string expr)
    {
        // TODO: 实现真正的解析逻辑
        // 这里只是示例结构
        return new ConditionNode
        {
            Type = ConditionNodeType.And,
            Children = new List<ConditionNode>()
        };
    }

    private HashSet<string> ExtractDependencies(ConditionNode? node)
    {
        var deps = new HashSet<string>();

        if (node == null) return deps;

        if (node.Type == ConditionNodeType.Dependency)
        {
            deps.Add(node.Value ?? "");
        }

        foreach (var child in node.Children)
        {
            foreach (var dep in ExtractDependencies(child))
            {
                deps.Add(dep);
            }
        }

        return deps;
    }

    /// <summary>
    /// 评估条件
    /// </summary>
    public bool Evaluate(IConditionContext context)
    {
        if (Ast == null) return false;
        return EvaluateNode(Ast, context);
    }

    private bool EvaluateNode(ConditionNode node, IConditionContext context)
    {
        switch (node.Type)
        {
            case ConditionNodeType.And:
                return node.Children.All(c => EvaluateNode(c, context));

            case ConditionNodeType.Or:
                return node.Children.Any(c => EvaluateNode(c, context));

            case ConditionNodeType.Not:
                return node.Children.Count > 0 && !EvaluateNode(node.Children[0], context);

            case ConditionNodeType.Comparison:
                return EvaluateComparison(node, context);

            case ConditionNodeType.Dependency:
                return context.GetValue(node.Value ?? "") != null;

            default:
                return false;
        }
    }

    private bool EvaluateComparison(ConditionNode node, IConditionContext context)
    {
        // 实现比较逻辑
        // 例如: level >= 20
        var parts = (node.Value ?? "").Split(' ');
        if (parts.Length != 3) return false;

        var leftValue = context.GetValue(parts[0]);
        var op = parts[1];
        var rightValue = parts[2];

        // 简化的比较实现
        if (leftValue is int intVal && int.TryParse(rightValue, out var compareVal))
        {
            return op switch
            {
                ">=" => intVal >= compareVal,
                ">" => intVal > compareVal,
                "<=" => intVal <= compareVal,
                "<" => intVal < compareVal,
                "==" => intVal == compareVal,
                "!=" => intVal != compareVal,
                _ => false
            };
        }

        return false;
    }
}

/// <summary>
/// 条件节点
/// </summary>
public class ConditionNode
{
    public ConditionNodeType Type { get; set; }
    public string? Value { get; set; }
    public List<ConditionNode> Children { get; set; } = new();
}

/// <summary>
/// 条件节点类型
/// </summary>
public enum ConditionNodeType
{
    And,
    Or,
    Not,
    Comparison,
    Dependency
}

/// <summary>
/// 条件上下文接口
/// </summary>
public interface IConditionContext
{
    object? GetValue(string key);
}

/// <summary>
/// 条件缓存系统
/// </summary>
public class ConditionCache
{
    // 解析缓存
    private readonly Dictionary<string, ConditionExpr> _parseCache = new();

    // 结果缓存
    private readonly Dictionary<string, CachedResult> _resultCache = new();

    // 依赖索引（反向）
    private readonly Dictionary<string, HashSet<string>> _dependencyIndex = new();

    /// <summary>
    /// 获取或解析条件
    /// </summary>
    public ConditionExpr GetOrParse(string raw)
    {
        if (_parseCache.TryGetValue(raw, out var cached))
            return cached;

        var expr = new ConditionExpr(raw);
        _parseCache[raw] = expr;

        // 更新依赖索引
        foreach (var dep in expr.Dependencies)
        {
            if (!_dependencyIndex.ContainsKey(dep))
                _dependencyIndex[dep] = new HashSet<string>();
            _dependencyIndex[dep].Add(expr.Id);
        }

        return expr;
    }

    /// <summary>
    /// 评估条件（带缓存）
    /// </summary>
    public bool Evaluate(string exprId, IConditionContext context, TimeSpan? cacheDuration = null)
    {
        if (_resultCache.TryGetValue(exprId, out var cached))
        {
            if (!cached.IsExpired())
                return cached.Result;
        }

        var expr = _parseCache.GetValueOrDefault(exprId);
        if (expr == null) return false;

        var result = expr.Evaluate(context);

        _resultCache[exprId] = new CachedResult
        {
            Result = result,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = cacheDuration.HasValue
                ? DateTime.UtcNow.Add(cacheDuration.Value)
                : null
        };

        return result;
    }

    /// <summary>
    /// 失效依赖于某个键的所有条件
    /// </summary>
    public void InvalidateDependents(string dependencyKey)
    {
        if (!_dependencyIndex.ContainsKey(dependencyKey))
            return;

        foreach (var exprId in _dependencyIndex[dependencyKey])
        {
            _resultCache.Remove(exprId);
        }
    }

    private class CachedResult
    {
        public bool Result { get; set; }
        public DateTime CachedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public bool IsExpired()
        {
            return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
        }
    }
}