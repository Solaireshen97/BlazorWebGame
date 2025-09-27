using MediatR;
using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Application.Interfaces;

namespace BlazorWebGame.Refactored.Application.Queries;

public record GetCharacterQuery(Guid CharacterId) : IRequest<Character?>;

public class GetCharacterQueryHandler : IRequestHandler<GetCharacterQuery, Character?>
{
    private readonly ICharacterService _characterService;

    public GetCharacterQueryHandler(ICharacterService characterService)
    {
        _characterService = characterService;
    }

    public async Task<Character?> Handle(GetCharacterQuery request, CancellationToken cancellationToken)
    {
        return await _characterService.GetCharacterAsync(request.CharacterId);
    }
}

public record GetUserCharactersQuery(string UserId) : IRequest<List<Character>>;

public class GetUserCharactersQueryHandler : IRequestHandler<GetUserCharactersQuery, List<Character>>
{
    private readonly ICharacterService _characterService;

    public GetUserCharactersQueryHandler(ICharacterService characterService)
    {
        _characterService = characterService;
    }

    public async Task<List<Character>> Handle(GetUserCharactersQuery request, CancellationToken cancellationToken)
    {
        return await _characterService.GetUserCharactersAsync(request.UserId);
    }
}