using BlazorWebGame.Refactored.Application.Interfaces;
using BlazorWebGame.Refactored.Presentation.State;
using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.ValueObjects;

namespace BlazorWebGame.Refactored.Infrastructure.Services;

/// <summary>
/// 角色服务实现 - 临时存根实现
/// </summary>
public class CharacterService : ICharacterService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<CharacterService> _logger;

    public CharacterService(IHttpClientService httpClient, ILogger<CharacterService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<CharacterData>> GetCharactersAsync()
    {
        // TODO: 实现API调用
        await Task.Delay(100);
        return new List<CharacterData>();
    }

    public async Task<CharacterData?> GetCharacterDetailAsync(Guid characterId)
    {
        // TODO: 实现API调用
        await Task.Delay(100);
        return null;
    }

    public async Task<CharacterData> CreateCharacterAsync(string name, CharacterClass characterClass)
    {
        // TODO: 实现API调用
        await Task.Delay(100);
        return new CharacterData { Id = Guid.NewGuid(), Name = name, Class = characterClass };
    }

    public async Task UpdateCharacterAsync(Guid characterId, CharacterUpdateData updateData)
    {
        // TODO: 实现API调用
        await Task.Delay(100);
    }

    public async Task DeleteCharacterAsync(Guid characterId)
    {
        // TODO: 实现API调用
        await Task.Delay(100);
    }

    public async Task<ResourcePool> GetCharacterResourcesAsync(Guid characterId)
    {
        // TODO: 实现API调用
        await Task.Delay(100);
        return new ResourcePool();
    }

    public async Task<IEnumerable<ItemReward>> GetCharacterInventoryAsync(Guid characterId)
    {
        // TODO: 实现API调用
        await Task.Delay(100);
        return new List<ItemReward>();
    }
}