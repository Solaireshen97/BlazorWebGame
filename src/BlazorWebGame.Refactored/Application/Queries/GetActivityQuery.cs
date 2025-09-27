using MediatR;
using BlazorWebGame.Refactored.Domain.Entities;
using BlazorWebGame.Refactored.Application.Interfaces;

namespace BlazorWebGame.Refactored.Application.Queries;

public record GetCharacterActivitiesQuery(Guid CharacterId) : IRequest<List<Activity>>;

public class GetCharacterActivitiesQueryHandler : IRequestHandler<GetCharacterActivitiesQuery, List<Activity>>
{
    private readonly IActivityService _activityService;

    public GetCharacterActivitiesQueryHandler(IActivityService activityService)
    {
        _activityService = activityService;
    }

    public async Task<List<Activity>> Handle(GetCharacterActivitiesQuery request, CancellationToken cancellationToken)
    {
        return await _activityService.GetCharacterActivitiesListAsync(request.CharacterId);
    }
}

public record GetActivityQuery(Guid ActivityId) : IRequest<Activity?>;

public class GetActivityQueryHandler : IRequestHandler<GetActivityQuery, Activity?>
{
    private readonly IActivityService _activityService;

    public GetActivityQueryHandler(IActivityService activityService)
    {
        _activityService = activityService;
    }

    public async Task<Activity?> Handle(GetActivityQuery request, CancellationToken cancellationToken)
    {
        return await _activityService.GetActivityAsync(request.ActivityId);
    }
}