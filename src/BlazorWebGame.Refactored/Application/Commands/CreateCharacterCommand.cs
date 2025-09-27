using MediatR;
using FluentValidation;
using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.Services;
using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Application.DTOs;

namespace BlazorWebGame.Refactored.Application.Commands;

public record CreateCharacterCommand(
    string Name,
    CharacterClass CharacterClass,
    Guid UserId
) : IRequest<Result<CharacterDto>>;

public class CreateCharacterCommandValidator : AbstractValidator<CreateCharacterCommand>
{
    public CreateCharacterCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Character name is required")
            .Length(3, 20).WithMessage("Character name must be between 3 and 20 characters")
            .Matches(@"^[a-zA-Z0-9_\-\u4e00-\u9fa5]+$")
            .WithMessage("Character name contains invalid characters");
        
        RuleFor(x => x.CharacterClass)
            .IsInEnum().WithMessage("Invalid character class");
        
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}

public class CreateCharacterCommandHandler : IRequestHandler<CreateCharacterCommand, Result<CharacterDto>>
{
    private readonly CharacterDomainService _domainService;
    private readonly IValidator<CreateCharacterCommand> _validator;
    private readonly ILogger<CreateCharacterCommandHandler> _logger;
    
    public CreateCharacterCommandHandler(
        CharacterDomainService domainService,
        IValidator<CreateCharacterCommand> validator,
        ILogger<CreateCharacterCommandHandler> logger)
    {
        _domainService = domainService;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<Result<CharacterDto>> Handle(
        CreateCharacterCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // 验证命令
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<CharacterDto>.Failure(errors);
            }
            
            // 创建值对象
            var nameResult = CharacterName.Create(request.Name);
            if (!nameResult.IsSuccess)
                return Result<CharacterDto>.Failure(nameResult.Error);
            
            var userId = UserId.Create(request.UserId);
            
            // 调用领域服务
            var result = await _domainService.CreateCharacterAsync(
                nameResult.Value!,
                request.CharacterClass,
                userId,
                cancellationToken);
            
            if (!result.IsSuccess)
                return Result<CharacterDto>.Failure(result.Error);
            
            // 映射到DTO
            var dto = MapToDto(result.Value!);
            
            _logger.LogInformation("Character {CharacterName} created successfully for user {UserId}", 
                request.Name, request.UserId);
            
            return Result<CharacterDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating character");
            return Result<CharacterDto>.Failure("An error occurred while creating the character");
        }
    }
    
    private CharacterDto MapToDto(Character character)
    {
        return new CharacterDto
        {
            Id = character.Id,
            UserId = character.UserId,
            Name = character.Name,
            Level = character.Level,
            Experience = character.Experience.ToLong(),
            CharacterClass = character.CharacterClass,
            Stats = new CharacterStatsDto
            {
                Strength = character.Stats.Strength,
                Intelligence = character.Stats.Intelligence,
                Agility = character.Stats.Agility,
                Vitality = character.Stats.Vitality,
                AttackPower = character.Stats.AttackPower,
                MagicPower = character.Stats.MagicPower,
                MaxHealth = character.Stats.MaxHealth,
                MaxMana = character.Stats.MaxMana,
                CriticalChance = character.Stats.CriticalChance,
                AttackSpeed = character.Stats.AttackSpeed
            },
            Gold = character.Resources.Gold.ToLong(),
            IsActive = character.IsActive,
            CreatedAt = character.CreatedAt,
            LastLogin = character.LastLogin
        };
    }
}