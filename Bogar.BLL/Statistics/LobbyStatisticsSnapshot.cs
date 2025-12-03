using System.Collections.Generic;

namespace Bogar.BLL.Statistics;

public sealed class LobbyStatisticsSnapshot
{
    public string LobbyName { get; init; } = string.Empty;
    public int TotalMatches { get; init; }
    public int DrawMatches { get; init; }
    public double AverageDurationSeconds { get; init; }
    public IReadOnlyList<PlayerStanding> PlayerStandings { get; init; } = new List<PlayerStanding>();
    public PlayerStanding? TopPerformer { get; init; }
}

public sealed class PlayerStanding
{
    public string BotName { get; init; } = string.Empty;
    public int MatchesPlayed { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int Draws { get; init; }
    public double AverageScore { get; init; }
}
