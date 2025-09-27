using BlazorWebGame.Refactored.Domain.Entities;

namespace BlazorWebGame.Refactored.Application.Services;

/// <summary>
/// 游戏状态管理器接口
/// </summary>
public interface IGameStateManager
{
    Character? CurrentCharacter { get; }
    Task LoadCharacterAsync(string characterId, CancellationToken cancellationToken = default);
    Task SaveCharacterAsync(CancellationToken cancellationToken = default);
    T? GetState<T>(string key) where T : class;
    void SetState<T>(string key, T value) where T : class;
    Task CreateCharacterAsync(string name, string userId, CancellationToken cancellationToken = default);
    Task<List<Character>> GetUserCharactersAsync(string userId, CancellationToken cancellationToken = default);
}