using System;
using System.Collections.Generic;

namespace BlazorWebGame.Shared.Models.Future;

/// <summary>
/// 赛季系统占位
/// </summary>
public class SeasonSystem
{
    public string SeasonId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SeasonTheme Theme { get; set; }
    public Dictionary<string, object> SeasonModifiers { get; set; } = new();
    public List<SeasonReward> Rewards { get; set; } = new();
}

public enum SeasonTheme
{
    Conquest,       // 征服
    Exploration,    // 探索
    Crafting,       // 工艺
    Combat          // 战斗
}

public class SeasonReward
{
    public int Tier { get; set; }
    public string RequirementExpr { get; set; } = string.Empty;
    public List<string> RewardItems { get; set; } = new();
}

/// <summary>
/// 公会系统占位
/// </summary>
public class GuildSystem
{
    public string GuildId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public List<GuildMember> Members { get; set; } = new();
    public GuildTech TechTree { get; set; } = new();
    public List<GuildActivity> Activities { get; set; } = new();
}

public class GuildMember
{
    public string CharacterId { get; set; } = string.Empty;
    public GuildRank Rank { get; set; }
    public int ContributionPoints { get; set; }
}

public enum GuildRank
{
    Member,
    Officer,
    Leader
}

public class GuildTech
{
    public Dictionary<string, int> UnlockedTechs { get; set; } = new();
    public int TechPoints { get; set; }
}

public class GuildActivity
{
    public string ActivityId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public List<string> Participants { get; set; } = new();
}

/// <summary>
/// 交易系统占位
/// </summary>
public class TradingSystem
{
    public string MarketId { get; set; } = string.Empty;
    public List<MarketListing> Listings { get; set; } = new();
    public List<TradeHistory> History { get; set; } = new();
    public MarketPriceIndex PriceIndex { get; set; } = new();
}

public class MarketListing
{
    public string ListingId { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int PricePerUnit { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class TradeHistory
{
    public string TradeId { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int TotalPrice { get; set; }
    public DateTime TradeTime { get; set; }
}

public class MarketPriceIndex
{
    public Dictionary<string, PriceStats> ItemPrices { get; set; } = new();
}

public class PriceStats
{
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int Volume { get; set; }
}

/// <summary>
/// PvP系统占位
/// </summary>
public class PvPSystem
{
    public string ArenaId { get; set; } = string.Empty;
    public List<PvPMatch> ActiveMatches { get; set; } = new();
    public Dictionary<string, PvPRating> Ratings { get; set; } = new();
    public List<PvPSeason> Seasons { get; set; } = new();
}

public class PvPMatch
{
    public string MatchId { get; set; } = string.Empty;
    public List<string> TeamA { get; set; } = new();
    public List<string> TeamB { get; set; } = new();
    public DateTime StartTime { get; set; }
    public PvPMatchResult? Result { get; set; }
}

public class PvPRating
{
    public string CharacterId { get; set; } = string.Empty;
    public int Rating { get; set; } = 1500;
    public int Wins { get; set; }
    public int Losses { get; set; }
    public string Tier { get; set; } = "Bronze";
}

public class PvPSeason
{
    public int SeasonNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<PvPSeasonReward> Rewards { get; set; } = new();
}

public class PvPMatchResult
{
    public string WinningTeam { get; set; } = string.Empty;
    public int Duration { get; set; }
    public Dictionary<string, PvPPlayerStats> PlayerStats { get; set; } = new();
}

public class PvPPlayerStats
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int DamageDealt { get; set; }
    public int HealingDone { get; set; }
}

public class PvPSeasonReward
{
    public string Tier { get; set; } = string.Empty;
    public int MinRating { get; set; }
    public List<string> Rewards { get; set; } = new();
}