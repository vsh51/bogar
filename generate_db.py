import sqlite3
import os

# 1. Визначення імені файлу бази даних
DB_FILE = 'bogar.db'

def create_database():
    """
    Створює підключення до бази даних та створює необхідні таблиці.
    """
    try:
        # Створення підключення (файл буде створено, якщо він не існує)
        conn = sqlite3.connect(DB_FILE)
        cursor = conn.cursor()
        print(f"Підключено до бази даних: {DB_FILE}")

        # 2. Створення таблиці Lobbies
        # Створення першою, оскільки на неї посилаються інші таблиці
        cursor.execute("""
        CREATE TABLE IF NOT EXISTS Lobbies (
            id INTEGER PRIMARY KEY,
            name TEXT NOT NULL UNIQUE
        );
        """)
        print("Таблиця 'Lobbies' створена або вже існує.")

        # 3. Створення таблиці Users
        cursor.execute("""
        CREATE TABLE IF NOT EXISTS Users (
            id INTEGER PRIMARY KEY,
            username TEXT NOT NULL UNIQUE,
            bot_name TEXT NOT NULL,
            bot_file_hash TEXT NOT NULL UNIQUE,
            lobby_ref INTEGER NOT NULL,
            FOREIGN KEY (lobby_ref) REFERENCES Lobbies (id)
        );
        """)
        print("Таблиця 'Users' створена або вже існує.")

        # 4. Створення таблиці Matches
        # Використовуємо REFERENCES Users(id) для зовнішніх ключів white_bot_ref, black_bot_ref, winner_ref
        cursor.execute("""
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
            status TEXT NOT NULL CHECK (status IN ('pending', 'in_progress', 'complited', 'failure')),
            FOREIGN KEY (white_bot_ref) REFERENCES Users (id),
            FOREIGN KEY (black_bot_ref) REFERENCES Users (id),
            FOREIGN KEY (winner_ref) REFERENCES Users (id)
        );""")
        print("Таблиця 'Matches' створена або вже існує.")

        # 5. Фіксація змін та закриття підключення
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
    print("\nСкрипт завершено.")
