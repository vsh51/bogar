using System;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;

namespace Bogar
{
    class Program
    {
        private const string DatabaseFileName = "bogar_game_database.db";
        private static readonly string ConnectionString = $"Data Source={DatabaseFileName}";

        static void Main(string[] args)
        {
            Console.WriteLine("--- Консольний додаток для роботи з БД SQLite за новою схемою ---");

            InitializeDatabase();
            PopulateDatabaseWithRandomData();

            Console.WriteLine("\n--- Дані з таблиці 'Users': ---");
            ReadDataFromTable("Users");

            Console.WriteLine("\n--- Дані з таблиці 'Lobbies': ---");
            ReadDataFromTable("Lobbies");
            
            Console.WriteLine("\n--- Дані з таблиці 'Matches': ---");
            ReadDataFromTable("Matches");

            Console.WriteLine("\n\nРоботу завершено. Натисніть будь-яку клавішу для виходу.");
            Console.ReadKey();
        }
        
        private static void InitializeDatabase()
        {
            Console.WriteLine("\nІніціалізація бази даних...");

            if (File.Exists(DatabaseFileName))
            {
                File.Delete(DatabaseFileName);
                Console.WriteLine("Старий файл бази даних видалено.");
            }

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                Console.WriteLine("З'єднання з базою даних встановлено.");
                
                var fkCommand = connection.CreateCommand();
                fkCommand.CommandText = "PRAGMA foreign_keys = ON;";
                fkCommand.ExecuteNonQuery();

                var createUserTableCmd = connection.CreateCommand();
                createUserTableCmd.CommandText = @"
                    CREATE TABLE Users (
                        user_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        user_name TEXT,
                        user_bot_name TEXT,
                        user_bot_file_hash TEXT,
                        user_type TEXT CHECK(user_type IN ('admin', 'user'))
                    );";
                createUserTableCmd.ExecuteNonQuery();
                Console.WriteLine("Таблицю 'Users' створено.");

                var createLobbiesTableCmd = connection.CreateCommand();
                createLobbiesTableCmd.CommandText = @"
                    CREATE TABLE Lobbies (
                        lobby_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        lobby_name TEXT
                    );";
                createLobbiesTableCmd.ExecuteNonQuery();
                Console.WriteLine("Таблицю 'Lobbies' створено.");

                var createMatchesTableCmd = connection.CreateCommand();
                createMatchesTableCmd.CommandText = @"
                    CREATE TABLE Matches (
                        match_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        match_player1_id INTEGER,
                        match_player2_id INTEGER,
                        match_winner_id INTEGER,
                        match_lobby_id INTEGER,
                        match_started_ti TEXT,
                        finished_at TEXT,
                        auto_win INTEGER,
                        score_p1 INTEGER,
                        score_p2 INTEGER,
                        moves TEXT,
                        status TEXT CHECK(status IN ('pending', 'in_progress', 'completed', 'error')),
                        FOREIGN KEY (match_player1_id) REFERENCES Users(user_id),
                        FOREIGN KEY (match_player2_id) REFERENCES Users(user_id),
                        FOREIGN KEY (match_winner_id) REFERENCES Users(user_id),
                        FOREIGN KEY (match_lobby_id) REFERENCES Lobbies(lobby_id)
                    );";
                createMatchesTableCmd.ExecuteNonQuery();
                Console.WriteLine("Таблицю 'Matches' створено.");
            }
             Console.WriteLine("Ініціалізацію завершено.");
        }

        public static void PopulateDatabaseWithRandomData()
        {
            Console.WriteLine("\nЗаповнення бази даних тестовими даними...");
            var random = new Random();
            var userIds = new List<long>();
            var lobbyIds = new List<long>();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                var userNames = new List<string> { "PlayerOne", "PlayerTwo", "Champion", "Rookie", "Master", "Noob" };
                for (int i = 0; i < 50; i++)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO Users (user_name, user_bot_name, user_bot_file_hash, user_type) VALUES (@user_name, @user_bot_name, @user_bot_file_hash, @user_type);";
                    
                    command.Parameters.AddWithValue("@user_name", $"{userNames[random.Next(userNames.Count)]}{random.Next(100, 999)}");
                    command.Parameters.AddWithValue("@user_bot_name", $"Bot_{Guid.NewGuid().ToString().Substring(0, 8)}");
                    command.Parameters.AddWithValue("@user_bot_file_hash", Guid.NewGuid().ToString("N"));
                    command.Parameters.AddWithValue("@user_type", random.Next(0, 10) == 0 ? "admin" : "user");

                    command.ExecuteNonQuery();
                }
                Console.WriteLine("Таблиця 'Users' заповнена 50 записами.");

                var lobbyNames = new List<string> { "Beginners", "Pro League", "Casual Fun", "High Stakes", "Tournament" };
                 for (int i = 0; i < 10; i++)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO Lobbies (lobby_name) VALUES (@lobby_name);";
                    command.Parameters.AddWithValue("@lobby_name", $"{lobbyNames[random.Next(lobbyNames.Count)]} #{i + 1}");
                    command.ExecuteNonQuery();
                }
                Console.WriteLine("Таблиця 'Lobbies' заповнена 10 записами.");
                
                var fetchIdsCommand = connection.CreateCommand();
                fetchIdsCommand.CommandText = "SELECT user_id FROM Users";
                using(var reader = fetchIdsCommand.ExecuteReader()) { while(reader.Read()) userIds.Add(reader.GetInt64(0)); }
                
                fetchIdsCommand.CommandText = "SELECT lobby_id FROM Lobbies";
                using(var reader = fetchIdsCommand.ExecuteReader()) { while(reader.Read()) lobbyIds.Add(reader.GetInt64(0)); }

                var statuses = new List<string> { "pending", "in_progress", "completed", "error" };
                for (int i = 0; i < 40; i++)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO Matches (match_player1_id, match_player2_id, match_winner_id, match_lobby_id, 
                                           match_started_ti, finished_at, auto_win, score_p1, score_p2, moves, status)
                        VALUES (@p1, @p2, @winner, @lobby, @start, @finish, @auto, @s1, @s2, @moves, @status);";

                    long player1Id = userIds[random.Next(userIds.Count)];
                    long player2Id;
                    do {
                        player2Id = userIds[random.Next(userIds.Count)];
                    } while (player1Id == player2Id);
                    
                    long lobbyId = lobbyIds[random.Next(lobbyIds.Count)];
                    string status = statuses[random.Next(statuses.Count)];
                    
                    long? winnerId = null;
                    string finishedAt = null;
                    if (status == "completed")
                    {
                        winnerId = random.Next(0, 2) == 0 ? player1Id : player2Id;
                        finishedAt = DateTime.Now.AddMinutes(-random.Next(5, 60)).ToString("o");
                    }
                    
                    command.Parameters.AddWithValue("@p1", player1Id);
                    command.Parameters.AddWithValue("@p2", player2Id);
                    command.Parameters.AddWithValue("@winner", winnerId.HasValue ? (object)winnerId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@lobby", lobbyId);
                    command.Parameters.AddWithValue("@start", DateTime.Now.AddMinutes(-random.Next(60, 120)).ToString("o"));
                    command.Parameters.AddWithValue("@finish", (object)finishedAt ?? DBNull.Value);
                    command.Parameters.AddWithValue("@auto", random.Next(0,10) > 8 ? 1 : 0);
                    command.Parameters.AddWithValue("@s1", random.Next(0, 11));
                    command.Parameters.AddWithValue("@s2", random.Next(0, 11));
                    command.Parameters.AddWithValue("@moves", "{ \"history\": [\"e4\", \"e5\", \"Nf3\"] }");
                    command.Parameters.AddWithValue("@status", status);
                    
                    command.ExecuteNonQuery();
                }
                Console.WriteLine("Таблиця 'Matches' заповнена 40 записами.");
            }
             Console.WriteLine("Заповнення даними завершено.");
        }

        private static void ReadDataFromTable(string tableName)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {tableName} LIMIT 20;";

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.WriteLine($"Таблиця '{tableName}' порожня.");
                        return;
                    }

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Console.Write($"{reader.GetName(i),-22} | ");
                    }
                    Console.WriteLine("\n" + new string('=', reader.FieldCount * 25));

                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                           Console.Write($"{reader.GetValue(i).ToString().Substring(0, Math.Min(reader.GetValue(i).ToString().Length, 20)),-22} | ");
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
