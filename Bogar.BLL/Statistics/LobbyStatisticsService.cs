using Bogar.BLL.Core;
using Bogar.DAL;
using Bogar.DAL.Infrastructure;
using Bogar.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Bogar.BLL.Statistics;

public sealed class LobbyStatisticsService : IDisposable
{
    private readonly string _lobbyName;
    private readonly LobbyDatabase _database;
    private readonly ILobbyMatchRepository _repository;
    private readonly SemaphoreSlim _lobbyInitSemaphore = new(1, 1);
    private int? _lobbyId;
    private static readonly Serilog.ILogger Logger = Log.ForContext<LobbyStatisticsService>();

    public LobbyStatisticsService(string lobbyName)
    {
        this._lobbyName = lobbyName;
        this._database = new LobbyDatabase(lobbyName);
        this._repository = new LobbyMatchRepository(this._database.CreateContext);
    }

    public async Task RecordMatchAsync(MatchResult result, CancellationToken cancellationToken = default)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        try
        {
            var lobbyId = await this.EnsureLobbyIdAsync(cancellationToken);
            var whiteUserId = await this._repository.EnsureUserAsync(lobbyId, result.WhiteNickname, cancellationToken);
            var blackUserId = await this._repository.EnsureUserAsync(lobbyId, result.BlackNickname, cancellationToken);

            int? winnerId = result.Winner switch
            {
                Color.White => whiteUserId,
                Color.Black => blackUserId,
                _ => null
            };

            var match = new Match
            {
                LobbyId = lobbyId,
                WhiteBotId = whiteUserId,
                BlackBotId = blackUserId,
                WinnerId = winnerId,
                StartTime = result.StartedAt.ToUnixTimeSeconds(),
                FinishTime = result.FinishedAt.ToUnixTimeSeconds(),
                IsAutoWin = result.IsAutoWin,
                ScoreWhite = result.WhiteScore,
                ScoreBlack = result.BlackScore,
                Moves = string.Join(" ", result.Moves ?? Array.Empty<string>()),
                Status = result.Status
            };

            await this._repository.AddMatchAsync(match, cancellationToken);
            Logger.Information(
                "Recorded match in lobby {Lobby}: {White} vs {Black} winner={Winner}",
                this._lobbyName,
                result.WhiteNickname,
                result.BlackNickname,
                result.Winner?.ToString() ?? "Draw");
        }
        catch (Exception ex)
        {
            Logger.Error(ex,
                "Failed to record match for lobby {Lobby}: {White} vs {Black}",
                this._lobbyName,
                result.WhiteNickname,
                result.BlackNickname);
            throw;
        }
    }

    public async Task DeleteMatchAsync(int matchId, CancellationToken cancellationToken = default)
    {
        try
        {
            var lobbyId = await this.EnsureLobbyIdAsync(cancellationToken);
            await this._repository.DeleteMatchAsync(lobbyId, matchId, cancellationToken);
            Logger.Information("Deleted match {MatchId} from lobby {Lobby}", matchId, this._lobbyName);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to delete match {MatchId} from lobby {Lobby}", matchId, this._lobbyName);
            throw;
        }
    }

    public async Task<IReadOnlyList<MatchHistoryEntry>> GetMatchHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var lobbyId = await this.EnsureLobbyIdAsync(cancellationToken);
            var matches = await this._repository.GetMatchesAsync(lobbyId, cancellationToken);

            var result = matches
                .Select(MapHistoryEntry)
                .ToList();

            Logger.Information("Loaded {Count} matches for lobby {Lobby}", result.Count, this._lobbyName);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load match history for lobby {Lobby}", this._lobbyName);
            throw;
        }
    }

    public async Task<LobbyStatisticsSnapshot> GetLobbyStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var lobbyId = await this.EnsureLobbyIdAsync(cancellationToken);
            var matches = await this._repository.GetMatchesAsync(lobbyId, cancellationToken);

            if (matches.Count == 0)
            {
                var emptySnapshot = new LobbyStatisticsSnapshot
                {
                    LobbyName = this._lobbyName,
                    PlayerStandings = Array.Empty<PlayerStanding>(),
                    TopPerformer = null
                };
                Logger.Information("No matches found for lobby {Lobby} while building snapshot", this._lobbyName);
                return emptySnapshot;
            }

            var standings = BuildStandings(matches);
            var durations = matches
                .Where(m => m.FinishTime.HasValue)
                .Select(m => Math.Max(0, m.FinishTime!.Value - m.StartTime!))
                .ToList();

            var snapshot = new LobbyStatisticsSnapshot
            {
                LobbyName = this._lobbyName,
                TotalMatches = matches.Count,
                DrawMatches = matches.Count(m => m.WinnerId == null),
                AverageDurationSeconds = durations.Count == 0 ? 0 : durations.Average(),
                PlayerStandings = standings,
                TopPerformer = standings.FirstOrDefault()
            };

            Logger.Information("Calculated lobby snapshot for {Lobby}", this._lobbyName);
            return snapshot;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to build lobby statistics for {Lobby}", this._lobbyName);
            throw;
        }
    }

    private async Task<int> EnsureLobbyIdAsync(CancellationToken cancellationToken)
    {
        if (this._lobbyId.HasValue)
        {
            return this._lobbyId.Value;
        }

        await this._lobbyInitSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!this._lobbyId.HasValue)
            {
                this._lobbyId = await this._repository.EnsureLobbyAsync(this._lobbyName, cancellationToken);
                Logger.Information("Initialized lobby storage for {Lobby}", this._lobbyName);
            }
        }
        finally
        {
            this._lobbyInitSemaphore.Release();
        }

        return this._lobbyId.Value;
    }

    private static MatchHistoryEntry MapHistoryEntry(Match match)
    {
        var finishedAt = match.FinishTime.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(match.FinishTime.Value)
            : (DateTimeOffset?)null;

        var durationSeconds = finishedAt.HasValue
            ? Math.Max(0, match.FinishTime!.Value - match.StartTime)
            : 0;

        return new MatchHistoryEntry
        {
            MatchId = match.Id,
            WhiteBotName = match.WhiteBot?.Username ?? "White",
            BlackBotName = match.BlackBot?.Username ?? "Black",
            WinnerName = match.Winner?.Username ?? "Draw",
            ScoreWhite = match.ScoreWhite,
            ScoreBlack = match.ScoreBlack,
            DurationSeconds = durationSeconds,
            StartedAt = DateTimeOffset.FromUnixTimeSeconds(match.StartTime),
            FinishedAt = finishedAt,
            Moves = SplitMoves(match.Moves!)
        };
    }

    private static IReadOnlyList<PlayerStanding> BuildStandings(IEnumerable<Match> matches)
    {
        var map = new Dictionary<string, MutableStanding>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches)
        {
            if (match.WhiteBot != null)
            {
                UpdateStanding(map, match.WhiteBot.Username!, match.ScoreWhite, match.WinnerId, match.WhiteBotId, match.BlackBotId);
            }

            if (match.BlackBot != null)
            {
                UpdateStanding(map, match.BlackBot.Username!, match.ScoreBlack, match.WinnerId, match.BlackBotId, match.WhiteBotId);
            }
        }

        return map.Values
            .Select(s => new PlayerStanding
            {
                BotName = s.BotName,
                MatchesPlayed = s.MatchesPlayed,
                Wins = s.Wins,
                Losses = s.Losses,
                Draws = s.Draws,
                AverageScore = s.MatchesPlayed == 0 ? 0 : s.TotalScore / (double)s.MatchesPlayed
            })
            .OrderByDescending(s => s.Wins)
            .ThenByDescending(s => s.AverageScore)
            .ThenBy(s => s.BotName)
            .ToList();
    }

    private static void UpdateStanding(
        IDictionary<string, MutableStanding> map,
        string botName,
        int score,
        int? winnerId,
        int playerId,
        int opponentId)
    {
        if (!map.TryGetValue(botName, out var standing))
        {
            standing = new MutableStanding(botName);
            map[botName] = standing;
        }

        standing.MatchesPlayed++;
        standing.TotalScore += score;

        if (winnerId == null)
        {
            standing.Draws++;
            return;
        }

        if (winnerId == playerId)
        {
            standing.Wins++;
        }
        else if (winnerId == opponentId)
        {
            standing.Losses++;
        }
    }

    private static IReadOnlyList<string> SplitMoves(string moves)
    {
        if (string.IsNullOrWhiteSpace(moves))
        {
            return Array.Empty<string>();
        }

        return moves
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    public void Dispose()
    {
        this._lobbyInitSemaphore.Dispose();
        this._database.Dispose();
    }

    private sealed class MutableStanding
    {
        public MutableStanding(string botName)
        {
            this.BotName = botName;
        }

        public string BotName { get; }
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public int TotalScore { get; set; }
    }
}
