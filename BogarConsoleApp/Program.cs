using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

class Program
{
    internal const string ConnectionString = "Data Source=bogar.db;Version=3;";

    internal static int Main(string[] args)
    {
        string exeName = System.IO.Path.GetFileName(Environment.GetCommandLineArgs()[0]);

        if (args.Length == 0)
        {
            Console.Error.WriteLine($"{exeName}: no arguments provided");
            Console.WriteLine("Try '--help' for more information.");
            return 1;
        }

        if (args.Length != 1)
        {
            Console.Error.WriteLine($"{exeName}: unknown options");
            Console.WriteLine("Try '--help' for more information.");
            return 1;
        }

        switch (args[0])
        {
            case "--help":
                ShowHelp();
                break;

            case "--fill":
                FillDatabaseWithRandomData();
                break;

            case "--display":
                DisplayTable("Lobbies");
                DisplayTable("Users");
                DisplayTable("Matches");
                break;

            default:
                Console.Error.WriteLine($"{exeName}: no arguments provided");
                Console.WriteLine("Try '--help' for more information.");
                return 1;
        }

        return 0;
    }

    internal static void ShowHelp()
    {
        Console.WriteLine(@"Usage:
  --fill      Fill the database with random test data
  --display   Display data from all tables to the console
  --help      Show this help
");
    }

    internal static void DisplayTable(string tableName)
    {
        Console.WriteLine($"--- Table | {tableName} ---");

        using (var conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();

            using (var cmd = new SQLiteCommand($"SELECT * FROM {tableName}", conn))
            using (var reader = cmd.ExecuteReader())
            {
                var table = new DataTable();
                table.Load(reader);

                foreach (DataRow row in table.Rows)
                {
                    foreach (DataColumn col in table.Columns)
                    {
                        Console.Write($"{col.ColumnName}: {row[col]} | ");
                    }
                    Console.WriteLine();
                }
            }
        }
    }

    internal static void FillDatabaseWithRandomData()
    {
        var rand = new Random();

        using (var conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();

            for (int i = 1; i <= 10; i++)
            {
                var cmd = new SQLiteCommand(
                    "INSERT OR IGNORE INTO Lobbies (name) VALUES (@name)", conn);
                cmd.Parameters.AddWithValue("@name", $"Lobby_{i}");
                cmd.ExecuteNonQuery();
            }

            var lobbyIds = new List<int>();
            using (var cmd = new SQLiteCommand("SELECT id FROM Lobbies", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    lobbyIds.Add(reader.GetInt32(0));
                }
            }

            for (int i = 1; i <= 40; i++)
            {
                var cmd = new SQLiteCommand(@"
INSERT OR IGNORE INTO Users 
(username, bot_name, bot_file_hash, lobby_ref) 
VALUES (@username, @bot_name, @bot_file_hash, @lobby_ref)", conn);

                cmd.Parameters.AddWithValue("@username", $"User_{i}");
                cmd.Parameters.AddWithValue("@bot_name", $"Bot_{i}");
                cmd.Parameters.AddWithValue("@bot_file_hash", Guid.NewGuid().ToString());
                cmd.Parameters.AddWithValue("@lobby_ref", lobbyIds[rand.Next(lobbyIds.Count)]);
                cmd.ExecuteNonQuery();
            }

            var userIds = new List<int>();
            using (var cmd = new SQLiteCommand("SELECT id FROM Users", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    userIds.Add(reader.GetInt32(0));
                }
            }

            string[] statuses = { "pending", "in_progress", "complited", "failure" };
            for (int i = 1; i <= 40; i++)
            {
                int whiteBot = userIds[rand.Next(userIds.Count)];
                int blackBot = userIds[rand.Next(userIds.Count)];
                int? winner = rand.Next(0, 2) == 0 ? (int?)whiteBot : (int?)blackBot;

                var cmd = new SQLiteCommand(@"
INSERT OR IGNORE INTO Matches
(white_bot_ref, black_bot_ref, winner_ref, start_time, finish_time, is_auto_win, score_white, score_black, moves, status)
VALUES (@white, @black, @winner, @start, @finish, @auto, @scoreW, @scoreB, @moves, @status)", conn);

                cmd.Parameters.AddWithValue("@white", whiteBot);
                cmd.Parameters.AddWithValue("@black", blackBot);
                cmd.Parameters.AddWithValue(
                    "@winner", winner.HasValue ? winner.Value : DBNull.Value);
                cmd.Parameters.AddWithValue(
                    "@start", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue(
                    "@finish", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + rand.Next(60, 3600));
                cmd.Parameters.AddWithValue("@auto", rand.Next(0, 2));
                cmd.Parameters.AddWithValue("@scoreW", rand.Next(0, 10));
                cmd.Parameters.AddWithValue("@scoreB", rand.Next(0, 10));
                cmd.Parameters.AddWithValue("@moves", "{}");
                cmd.Parameters.AddWithValue("@status", statuses[rand.Next(statuses.Length)]);
                cmd.ExecuteNonQuery();
            }
        }

        Console.WriteLine("The database is successfully filled with data.");
    }
}
