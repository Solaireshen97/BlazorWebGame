namespace BlazorWebGame.Refactored.Infrastructure.Events.Core;

/// <summary>
/// 游戏事件基接口
/// </summary>
public interface IGameEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
    string Type { get; }
    Dictionary<string, object> Metadata { get; }
}