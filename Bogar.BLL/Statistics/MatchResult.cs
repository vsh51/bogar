using Bogar.BLL.Core;
using Bogar.DAL;
using System;
using System.Collections.Generic;

namespace Bogar.BLL.Statistics;

public sealed class MatchResult
{
    public Guid WhiteClientId { get; init; }
    public string WhiteNickname { get; init; } = string.Empty;
    public Guid BlackClientId { get; init; }
    public string BlackNickname { get; init; } = string.Empty;
    public Color? Winner { get; init; }
    public MatchStatus Status { get; init; } = MatchStatus.Completed;
    public IReadOnlyList<string> Moves { get; init; } = Array.Empty<string>();
    public int WhiteScore { get; init; }
    public int BlackScore { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; init; }
    public bool IsAutoWin { get; init; }
}
