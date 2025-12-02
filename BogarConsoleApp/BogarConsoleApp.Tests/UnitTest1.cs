using System;
using System.Data.SQLite;
using System.IO;

namespace BogarConsoleApp.Tests;

public class ProgramTests
{
    [Fact]
    public void Main_NoArguments_PrintsErrorAndReturnsOne()
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();

        Console.SetOut(outWriter);
        Console.SetError(errWriter);

        try
        {
            var exitCode = Program.Main(Array.Empty<string>());

            Assert.Equal(1, exitCode);
            Assert.Contains("no arguments provided", errWriter.ToString());
            Assert.Contains("Try '--help'", outWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void Main_TooManyArguments_PrintsUnknownOptions()
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();

        Console.SetOut(outWriter);
        Console.SetError(errWriter);

        try
        {
            var exitCode = Program.Main(new[] { "--fill", "--display" });

            Assert.Equal(1, exitCode);
            Assert.Contains("unknown options", errWriter.ToString());
            Assert.Contains("Try '--help'", outWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void Main_Help_PrintsUsageAndReturnsZero()
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();

        Console.SetOut(outWriter);
        Console.SetError(errWriter);

        try
        {
            var exitCode = Program.Main(new[] { "--help" });

            Assert.Equal(0, exitCode);
            Assert.Contains("Usage", outWriter.ToString());
            Assert.True(string.IsNullOrWhiteSpace(errWriter.ToString()));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void FillDatabaseWithRandomData_CreatesRecordsInAllTables()
    {
        WithTempDatabase(() =>
        {
            Program.FillDatabaseWithRandomData();

            using var conn = new SQLiteConnection(Program.ConnectionString);
            conn.Open();

            Assert.Equal(10, CountRows(conn, "Lobbies"));
            Assert.Equal(40, CountRows(conn, "Users"));
            Assert.Equal(40, CountRows(conn, "Matches"));
        });
    }

    [Fact]
    public void DisplayTable_WithSingleRow_PrintsColumns()
    {
        WithTempDatabase(() =>
        {
            SeedMinimalData();

            var originalOut = Console.Out;
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);

            try
            {
                Program.DisplayTable("Lobbies");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = outWriter.ToString();
            Assert.Contains("--- Table | Lobbies ---", output);
            Assert.Contains("id: 1", output);
            Assert.Contains("name: DisplayLobby", output);
        });
    }

    [Fact]
    public void Main_Display_PrintsAllTablesContent()
    {
        WithTempDatabase(() =>
        {
            SeedMinimalData();

            var originalOut = Console.Out;
            var originalErr = Console.Error;

            using var outWriter = new StringWriter();
            using var errWriter = new StringWriter();

            Console.SetOut(outWriter);
            Console.SetError(errWriter);

            try
            {
                var exitCode = Program.Main(new[] { "--display" });
                Assert.Equal(0, exitCode);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
            }

            var output = outWriter.ToString();
            Assert.Contains("--- Table | Lobbies ---", output);
            Assert.Contains("--- Table | Users ---", output);
            Assert.Contains("--- Table | Matches ---", output);
            Assert.Contains("name: DisplayLobby", output);
            Assert.Contains("username: DisplayUserA", output);
            Assert.Contains("status: pending", output);
            Assert.True(string.IsNullOrWhiteSpace(errWriter.ToString()));
        });
    }

    private static int CountRows(SQLiteConnection conn, string tableName)
    {
        using var cmd = new SQLiteCommand($"SELECT COUNT(*) FROM {tableName}", conn);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void EnsureSchemaExists()
    {
        using var conn = new SQLiteConnection(Program.ConnectionString);
        conn.Open();

        ExecuteNonQuery(conn, """
            CREATE TABLE IF NOT EXISTS Lobbies (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL UNIQUE
            );
            """);

        ExecuteNonQuery(conn, """
            CREATE TABLE IF NOT EXISTS Users (
                id INTEGER PRIMARY KEY,
                username TEXT NOT NULL UNIQUE,
                bot_name TEXT NOT NULL,
                bot_file_hash TEXT NOT NULL UNIQUE,
                lobby_ref INTEGER NOT NULL,
                FOREIGN KEY (lobby_ref) REFERENCES Lobbies (id)
            );
            """);

        ExecuteNonQuery(conn, """
            CREATE TABLE IF NOT EXISTS Matches (
                id INTEGER PRIMARY KEY,
                white_bot_ref INTEGER NOT NULL,
                black_bot_ref INTEGER NOT NULL,
                winner_ref INTEGER,
                start_time INTEGER,
                finish_time INTEGER,
                is_auto_win INTEGER,
                score_white INTEGER,
                score_black INTEGER,
                moves TEXT,
                status TEXT NOT NULL,
                FOREIGN KEY (white_bot_ref) REFERENCES Users (id),
                FOREIGN KEY (black_bot_ref) REFERENCES Users (id),
                FOREIGN KEY (winner_ref) REFERENCES Users (id)
            );
            """);
    }

    private static void SeedMinimalData()
    {
        using var conn = new SQLiteConnection(Program.ConnectionString);
        conn.Open();

        ExecuteNonQuery(conn, """
            INSERT INTO Lobbies (id, name)
            VALUES (1, 'DisplayLobby');
            """);

        ExecuteNonQuery(conn, """
            INSERT INTO Users (id, username, bot_name, bot_file_hash, lobby_ref)
            VALUES (1, 'DisplayUserA', 'DisplayBotA', 'hash-A', 1);
            """);

        ExecuteNonQuery(conn, """
            INSERT INTO Users (id, username, bot_name, bot_file_hash, lobby_ref)
            VALUES (2, 'DisplayUserB', 'DisplayBotB', 'hash-B', 1);
            """);

        ExecuteNonQuery(conn, """
            INSERT INTO Matches (
                id, white_bot_ref, black_bot_ref, winner_ref, start_time, finish_time,
                is_auto_win, score_white, score_black, moves, status)
            VALUES (
                1, 1, 2, 1, 1700000000, 1700000300,
                0, 5, 4, '{"moves":[]}', 'pending');
            """);
    }

    private static void ExecuteNonQuery(SQLiteConnection conn, string sql)
    {
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private static void WithTempDatabase(Action action)
    {
        var originalDirectory = Directory.GetCurrentDirectory();
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            Directory.SetCurrentDirectory(tempDirectory);
            EnsureSchemaExists();
            action();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }
    }
}
