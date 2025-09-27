using BlazorWebGame.Refactored.Domain.Entities;

namespace BlazorWebGame.Refactored.Infrastructure.Persistence;

/// <summary>
/// 角色数据仓储接口
/// </summary>
public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Character?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Character character, CancellationToken cancellationToken = default);
    Task UpdateAsync(Character character, CancellationToken cancellationToken = default);
    Task<List<Character>> GetAllAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    
    // 新增方法用于领域服务
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Character>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}