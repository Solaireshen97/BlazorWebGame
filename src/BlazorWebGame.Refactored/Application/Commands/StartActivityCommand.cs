using MediatR;
using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Domain.ValueObjects;
using BlazorWebGame.Refactored.Application.Interfaces;

namespace BlazorWebGame.Refactored.Application.Commands;

public record StartActivityCommand(
    Guid CharacterId,
    ActivityType ActivityType,
    Dictionary<string, object> Parameters
) : IRequest<Activity>;

public class StartActivityCommandHandler : IRequestHandler<StartActivityCommand, Activity>
{
    private readonly IActivityService _activityService;
    private readonly ICharacterService _characterService;

    public StartActivityCommandHandler(
        IActivityService activityService,
        ICharacterService characterService)
    {
        _activityService = activityService;
        _characterService = characterService;
    }

    public async Task<Activity> Handle(StartActivityCommand request, CancellationToken cancellationToken)
    {
        // 验证角色存在
        var character = await _characterService.GetCharacterAsync(request.CharacterId);
        if (character == null)
        {
            throw new ArgumentException($"Character with ID '{request.CharacterId}' not found.");
        }

        // 检查角色是否有可用活动槽位
        if (!character.Activities.HasAvailableSlot())
        {
            throw new InvalidOperationException("No available activity slots.");
        }

        // 创建活动参数
        var activityParameters = new ActivityParameters(request.Parameters);

        // 启动活动
        var activity = await _activityService.StartActivityAsync(
            request.CharacterId,
            request.ActivityType,
            activityParameters
        );

        return activity;
    }
}