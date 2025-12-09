import sqlite3


DB_FILE = "bogar.db"


def create_database():
    try:
        conn = sqlite3.connect(DB_FILE)
        cursor = conn.cursor()
        print(f"Підключено до бази даних: {DB_FILE}")

        cursor.execute("""
        CREATE TABLE IF NOT EXISTS Lobbies (
            Id INTEGER PRIMARY KEY,
            Name TEXT
        );
        """)
        print("Таблиця 'Lobbies' створена або вже існує.")

        cursor.execute("""
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY,
            Username TEXT,
            BotName TEXT,
            BotFileHash TEXT,
            LobbyId INTEGER NOT NULL,
            FOREIGN KEY (LobbyId) REFERENCES Lobbies (Id) ON DELETE CASCADE
        );
        """)
        print("Таблиця 'Users' створена або вже існує.")

        cursor.execute("""
        CREATE TABLE IF NOT EXISTS Matches (
            Id INTEGER PRIMARY KEY,
            LobbyId INTEGER NOT NULL,
            WhiteBotId INTEGER NOT NULL,
            BlackBotId INTEGER NOT NULL,
            WinnerId INTEGER,
            StartTime INTEGER NOT NULL,
            FinishTime INTEGER,
            IsAutoWin INTEGER NOT NULL,
            ScoreWhite INTEGER NOT NULL,
            ScoreBlack INTEGER NOT NULL,
            Moves TEXT,
            Status TEXT NOT NULL CHECK (Status IN ('Pending', 'InProgress', 'Completed', 'Failure')),
            FOREIGN KEY (LobbyId) REFERENCES Lobbies (Id) ON DELETE CASCADE,
            FOREIGN KEY (WhiteBotId) REFERENCES Users (Id) ON DELETE RESTRICT,
            FOREIGN KEY (BlackBotId) REFERENCES Users (Id) ON DELETE RESTRICT,
            FOREIGN KEY (WinnerId) REFERENCES Users (Id) ON DELETE RESTRICT
        );""")
        print("Таблиця 'Matches' створена або вже існує.")

        conn.commit()
        print("Зміни збережено.")
    except sqlite3.Error as e:
        print(f"Помилка SQLite: {e}")
    finally:
        if conn:
            conn.close()
            print("Підключення до бази даних закрито.")


if __name__ == "__main__":
    create_database()
