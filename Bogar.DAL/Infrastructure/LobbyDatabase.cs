using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;

namespace Bogar.DAL.Infrastructure;

/// <summary>
/// Keeps a single shared in-memory SQLite database alive for the lifetime of a lobby.
/// </summary>
public sealed class LobbyDatabase : IDisposable
{
    private readonly SqliteConnection _rootConnection;
    private readonly DbContextOptions<GameDbContext> _options;

    public LobbyDatabase(string lobbyName)
    {
        var connectionString = BuildConnectionString(lobbyName);

        _rootConnection = new SqliteConnection(connectionString);
        _rootConnection.Open();

        _options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite(connectionString)
            .Options;

        using var context = new GameDbContext(_options);
        context.Database.EnsureCreated();
    }

    public GameDbContext CreateContext() => new(_options);

    private static string BuildConnectionString(string lobbyName)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = $"mem_{NormalizeLobbyName(lobbyName)}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        };

        return builder.ToString();
    }

    private static string NormalizeLobbyName(string lobbyName)
    {
        if (string.IsNullOrWhiteSpace(lobbyName))
        {
            return "lobby";
        }

        var normalized = new StringBuilder();
        foreach (var ch in lobbyName)
        {
            if (char.IsLetterOrDigit(ch))
            {
                normalized.Append(char.ToLowerInvariant(ch));
            }
            else if (char.IsWhiteSpace(ch) && (normalized.Length == 0 || normalized[^1] != '_'))
            {
                normalized.Append('_');
            }
        }

        return normalized.Length == 0 ? "lobby" : normalized.ToString();
    }

    public void Dispose()
    {
        _rootConnection.Dispose();
    }
}
