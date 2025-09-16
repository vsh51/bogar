# Bogar

## Project Description
Bogar is a program that combines a board game with a platform for bot competitions. Users can place chess-like pieces according to the rules and create bots that execute commands and participate in the game.

## Game Rules
Each player receives a standard set of pieces: king, queen, two rooks, two bishops, two knights, and eight pawns.

- White moves first, then players take turnssss placing pieces on empty squares.
- After all pieces are placed, points are calculated.

### Scoring System
- **Pawn** — 1 point  
- **Knight / Bishop** — 3 points  
- **Rook** — 5 points  
- **Queen** — 8 points  
- **King** — 9 points  

Points are awarded for opponent pieces under threat. The player with the higher score wins.

## Project Structure
- **User Interface** — displays the board, available pieces, and game state.  
- **Game Logic** — implements rules for piece placement and scoring.  
- **Database** — stores user profiles, bots, and game statistics.  
- **Bots** — execute commands and make autonomous decisions during the game.  

## Completion Criteria
- **Functionality:** users and bots can place pieces; scoring works correctly.  
- **Database Integration:** SQLite stores profiles, bots, and statistics; CRUD operations work via Entity Framework Core + LINQ.  
- **Bot Behavior:** bots autonomously execute valid moves according to game rules.  
- **User Interface:** WPF displays the board and pieces; users receive feedback on actions.  
- **Stability:** system handles invalid moves and commands; users are notified of errors.  
- **Usability:** intuitive interface with clear feedback on moves and scoring.  

## Technologies
- **C# WPF** — graphical user interface  
- **SQLite** — database  
- **Entity Framework Core + LINQ** — data handling and queries  

## Future Development
Future development of Bogar includes the implementation of a tournament mode, a ranking system, and support for networked gameplay. These features will allow users to compete in structured competitions, track their performance, and engage with other players online. Overall, the planned improvements aim to enhance the educational and strategic aspects of the application, making it not only a platform for testing bot strategies but also a tool for learning and experimentation in algorithm design and game theory.
