using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bogar.DAL.Repositories;

public interface ILobbyMatchRepository
{
    Task<int> EnsureLobbyAsync(string lobbyName, CancellationToken cancellationToken = default);
    Task<int> EnsureUserAsync(int lobbyId, string nickname, CancellationToken cancellationToken = default);
    Task AddMatchAsync(Match match, CancellationToken cancellationToken = default);
    Task<List<Match>> GetMatchesAsync(int lobbyId, CancellationToken cancellationToken = default);
    Task DeleteMatchAsync(int lobbyId, int matchId, CancellationToken cancellationToken = default);
}

public sealed class LobbyMatchRepository : ILobbyMatchRepository
{
    private readonly Func<GameDbContext> _contextFactory;

    public LobbyMatchRepository(Func<GameDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<int> EnsureLobbyAsync(string lobbyName, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory();
        var lobby = await context.Lobbies
            .FirstOrDefaultAsync(l => l.Name == lobbyName, cancellationToken);

        if (lobby != null)
        {
            return lobby.Id;
        }

        lobby = new Lobby { Name = lobbyName };
        context.Lobbies.Add(lobby);
        await context.SaveChangesAsync(cancellationToken);
        return lobby.Id;
    }

    public async Task<int> EnsureUserAsync(int lobbyId, string nickname, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory();
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.LobbyId == lobbyId && u.Username == nickname, cancellationToken);

        if (user != null)
        {
            return user.Id;
        }

        user = new User
        {
            LobbyId = lobbyId,
            Username = nickname,
            BotName = nickname,
            BotFileHash = string.Empty
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user.Id;
    }

    public async Task AddMatchAsync(Match match, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory();
        context.Matches.Add(match);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Match>> GetMatchesAsync(int lobbyId, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory();
        return await context.Matches
            .AsNoTracking()
            .Include(m => m.WhiteBot)
            .Include(m => m.BlackBot)
            .Include(m => m.Winner)
            .Where(m => m.LobbyId == lobbyId)
            .OrderByDescending(m => m.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteMatchAsync(int lobbyId, int matchId, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory();
        var match = await context.Matches
            .FirstOrDefaultAsync(m => m.Id == matchId && m.LobbyId == lobbyId, cancellationToken);

        if (match == null)
        {
            return;
        }

        context.Matches.Remove(match);
        await context.SaveChangesAsync(cancellationToken);
    }
}
