using MediatR;
using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Application.Interfaces;

namespace BlazorWebGame.Refactored.Application.Commands;

public record CreateCharacterCommand(
    string Name,
    string CharacterClass,
    string UserId
) : IRequest<Character>;

public class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, Character>
{
    private readonly ICharacterService _characterService;

    public CreateCharacterCommandHandler(ICharacterService characterService)
    {
        _characterService = characterService;
    }

    public async Task<Character> Handle(CreateCharacterCommand request, CancellationToken cancellationToken)
    {
        // 验证字符名是否可用
        var existingCharacter = await _characterService.GetCharacterByNameAsync(request.Name);
        if (existingCharacter != null)
        {
            throw new InvalidOperationException($"Character name '{request.Name}' is already taken.");
        }

        // 创建新角色
        var character = new Character(
            Guid.NewGuid(),
            request.Name,
            request.CharacterClass,
            request.UserId
        );

        return await _characterService.CreateCharacterAsync(character);
    }
}