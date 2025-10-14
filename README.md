# Bogar

Bogar is a program that combines a board game with a platform for bot
competitions. Users can place chess-like pieces according to the rules and
create bots that execute commands and participate in the game.
For detailed information, see the documents/ folder.

---

## Installing Dependencies

Install required .NET packages and tools with:

```bash
dotnet restore BogarConsoleApp/BogarConsoleApp.csproj
```

---

## Project Structure

```
bogar/
├── bogar.db            (generated via generate_db.py)
├── generate_db.py
└── BogarConsoleApp/
    ├── Program.cs
    └── BogarConsoleApp.csproj
```

---

## Running the App

### Build and Run

From the repository root:

```bash
python3 generate_db.py
dotnet run --project BogarConsoleApp -- [options]
```

### Example

```bash
dotnet run --project BogarConsoleApp -- --help
```

### Notes

* Recommended to use `dotnet format` to keep code consistent.
