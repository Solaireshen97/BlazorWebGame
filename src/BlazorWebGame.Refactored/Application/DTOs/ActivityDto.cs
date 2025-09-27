using BlazorWebGame.Refactored.Domain.ValueObjects;

namespace BlazorWebGame.Refactored.Application.DTOs;

public class ActivityDto
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public double Progress { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
    public bool CanCancel { get; set; }
    public bool CanPause { get; set; }
    public bool IsPaused { get; set; }
}

public class BattleActivityDto : ActivityDto
{
    public string EnemyName { get; set; } = string.Empty;
    public int EnemyLevel { get; set; }
    public string EnemyHealth { get; set; } = "0"; // BigNumber as string
    public string EnemyMaxHealth { get; set; } = "0"; // BigNumber as string
    public List<BattleLogEntryDto> BattleLog { get; set; } = new();
    public bool IsPlayerTurn { get; set; }
    public List<string> AvailableActions { get; set; } = new();
}

public class BattleLogEntryDto
{
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Damage { get; set; } = "0"; // BigNumber as string
    public string Description { get; set; } = string.Empty;
}

public class GatheringActivityDto : ActivityDto
{
    public string ResourceType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ToolLevel { get; set; }
    public double SuccessRate { get; set; }
    public string ExpectedYield { get; set; } = "0"; // BigNumber as string
    public List<GatheringResultDto> Results { get; set; } = new();
}

public class GatheringResultDto
{
    public string ResourceName { get; set; } = string.Empty;
    public string Quantity { get; set; } = "0"; // BigNumber as string
    public string Quality { get; set; } = string.Empty;
    public bool IsRare { get; set; }
}

public class CraftingActivityDto : ActivityDto
{
    public string RecipeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public List<CraftingMaterialDto> Materials { get; set; } = new();
    public double SuccessRate { get; set; }
    public string QualityBonus { get; set; } = "0"; // BigNumber as string
}

public class CraftingMaterialDto
{
    public string ItemName { get; set; } = string.Empty;
    public string RequiredQuantity { get; set; } = "0"; // BigNumber as string
    public string AvailableQuantity { get; set; } = "0"; // BigNumber as string
    public bool HasSufficient { get; set; }
}