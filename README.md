# Bogar

Bogar is a program that combines a board game with a platform for bot
competitions. Users can place chess-like pieces according to the rules and
create bots that execute commands and participate in the game.
For detailed information, see the documents/ folder.

---

## Project Structure

```
bogar/
├── Bogar.sln                # main solution
├── Bogar.BLL/               # business logic layer
├── Bogar.DAL/               # data access layer (stub)
├── Bogar.Tests/             # xUnit tests
├── BogarConsoleApp.sln
├── BogarConsoleApp/         # console tool for DB inspection
├── bogar.db                 # SQLite database (generated via generate_db.py)
├── generate_db.py
└── documents/               # documentation and design materials
```

BogarConsoleApp is a stand-alone console tool used to populate and inspect the
database for testing and development purposes.

---

## Installing Dependencies

Install required .NET packages and tools with:
```bash
dotnet restore Bogar.sln
```

---

## Running Tests
```bash
dotnet test Bogar.sln
```

---

## Database Utilities

To (re)generate or inspect the database:
```bash
python3 generate_db.py
dotnet run --project BogarConsoleApp -- [options]
```

Example:
```bash
dotnet run --project BogarConsoleApp -- --help
```

## Notes

* Recommended to use `dotnet format` to keep code consistent.
