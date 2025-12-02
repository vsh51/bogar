using Bogar.DAL;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class GameDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly GameDbContext _context;

    public GameDbContextTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new GameDbContext(options);

        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task CanAddAndReadLobby_ShouldSucceed()
    {
        var lobby = new Lobby { Name = "Test Lobby" };

        _context.Lobbies.Add(lobby);
        await _context.SaveChangesAsync();

        var lobbyFromDb = await _context.Lobbies.FindAsync(lobby.Id);
        Assert.NotNull(lobbyFromDb);
        Assert.Equal("Test Lobby", lobbyFromDb.Name);
    }

    [Fact]
    public async Task Relationship_AddingUserToLobby_ShouldSetForeignKey()
    {
        var lobby = new Lobby { Name = "Lobby with User" };
        var user = new User { Username = "TestUser", BotName = "Bot", BotFileHash="b12976edv" };

        lobby.Users.Add(user);

        _context.Lobbies.Add(lobby);
        await _context.SaveChangesAsync();

        var userFromDb = await _context.Users.SingleAsync();
        Assert.Equal(lobby.Id, userFromDb.LobbyId);
        Assert.Equal(user.Id, userFromDb.Id);
    }

    [Fact]
    public async Task Constraint_DeletingUserInMatch()
    {
        var lobby = new Lobby { Name = "Constraint Lobby" };
        var whiteBot = new User { Username = "Player1", BotName = "Bot1", BotFileHash="d871tf32d", Lobby = lobby };
        var blackBot = new User { Username = "Player2", BotName = "Bot2", BotFileHash = "12397d86gv", Lobby = lobby };
        _context.Users.AddRange(whiteBot, blackBot);

        var match = new Match
        {
            Lobby = lobby,
            WhiteBot = whiteBot,
            BlackBot = blackBot,
            Status = MatchStatus.InProgress,
            StartTime = 12345,
            Moves = "",
            ScoreBlack = 0,
            ScoreWhite = 0
        };
        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        // Очищує кеш (потрібно оскільки бд не зберігається в persistent memory)
        _context.ChangeTracker.Clear();

        var userToDelete = await _context.Users.FindAsync(whiteBot.Id);
        _context.Users.Remove(userToDelete);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await _context.SaveChangesAsync();
        });

        Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Contains("FOREIGN KEY constraint failed", exception.InnerException.Message);
    }

    [Fact]
    public async Task TypeConversion_MatchStatusEnum_ShouldSaveAsString()
    {
        var lobby = new Lobby { Name = "Lobby" };
        var u1 = new User { Username = "P1", BotName = "B1", BotFileHash = "d871tf32d", Lobby = lobby };
        var u2 = new User { Username = "P2", BotName = "B2", BotFileHash = "12397d86gv", Lobby = lobby };
        var match = new Match
        {
            Lobby = lobby,
            WhiteBot = u1,
            BlackBot = u2,
            Status = MatchStatus.InProgress,
            StartTime = 12345,
            Moves = "",
            ScoreBlack = 0,
            ScoreWhite = 0
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT status FROM Matches WHERE Id = {match.Id}";

        var rawStatusValue = (string)await command.ExecuteScalarAsync();

        Assert.Equal("InProgress", rawStatusValue);

        var matchFromDb = await _context.Matches.FindAsync(match.Id);
        Assert.Equal(MatchStatus.InProgress, matchFromDb.Status);
    }
}