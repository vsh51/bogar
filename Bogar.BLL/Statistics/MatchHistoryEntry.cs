using System;
using System.Collections.Generic;

namespace Bogar.BLL.Statistics;

public sealed class MatchHistoryEntry
{
    public int MatchId { get; init; }
    public string WhiteBotName { get; init; } = string.Empty;
    public string BlackBotName { get; init; } = string.Empty;
    public string WinnerName { get; init; } = string.Empty;
    public int ScoreWhite { get; init; }
    public int ScoreBlack { get; init; }
    public double DurationSeconds { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public IReadOnlyList<string> Moves { get; init; } = Array.Empty<string>();
}
