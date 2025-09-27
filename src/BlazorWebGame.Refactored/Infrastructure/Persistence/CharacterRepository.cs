using BlazorWebGame.Refactored.Application.Interfaces;
using BlazorWebGame.Refactored.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BlazorWebGame.Refactored.Infrastructure.Persistence;

/// <summary>
/// 角色数据仓储实现
/// </summary>
public class CharacterRepository : ICharacterRepository
{
    private readonly IDataPersistenceService _persistenceService;
    private readonly ILogger<CharacterRepository> _logger;
    private const string COLLECTION_NAME = "characters";

    public CharacterRepository(IDataPersistenceService persistenceService, ILogger<CharacterRepository> logger)
    {
        _persistenceService = persistenceService;
        _logger = logger;
    }

    public async Task<Character?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _persistenceService.GetAsync<Character>(COLLECTION_NAME, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting character {Id}", id);
            return null;
        }
    }

    public async Task AddAsync(Character character, CancellationToken cancellationToken = default)
    {
        try
        {
            await _persistenceService.SaveAsync(COLLECTION_NAME, character.Id.ToString(), character);
            _logger.LogDebug("Character {Id} added successfully", character.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding character {Id}", character.Id);
            throw;
        }
    }

    public async Task UpdateAsync(Character character, CancellationToken cancellationToken = default)
    {
        try
        {
            await _persistenceService.SaveAsync(COLLECTION_NAME, character.Id.ToString(), character);
            _logger.LogDebug("Character {Id} updated successfully", character.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating character {Id}", character.Id);
            throw;
        }
    }

    public async Task<List<Character>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _persistenceService.GetAllAsync<Character>(COLLECTION_NAME);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all characters");
            return new List<Character>();
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _persistenceService.DeleteAsync(COLLECTION_NAME, id);
            _logger.LogDebug("Character {Id} deleted successfully", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting character {Id}", id);
            throw;
        }
    }
}