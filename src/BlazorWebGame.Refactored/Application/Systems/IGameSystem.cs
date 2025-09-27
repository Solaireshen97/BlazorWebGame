namespace BlazorWebGame.Refactored.Application.Systems;

/// <summary>
/// 游戏系统接口
/// </summary>
public interface IGameSystem
{
    string Name { get; }
    int Priority { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
    bool ShouldProcess(double deltaTime);
    Task ProcessAsync(double deltaTime, CancellationToken cancellationToken);
}