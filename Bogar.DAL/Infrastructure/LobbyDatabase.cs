using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace Bogar.DAL.Infrastructure;

/// <summary>
/// Keeps a shared SQLite database alive for the lifetime of a lobby.
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
        _ = lobbyName;
        var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var databasePath = Path.Combine(projectRoot, "bogar.db");

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        };

        return builder.ToString();
    }

    public void Dispose()
    {
        _rootConnection.Dispose();
    }
}
