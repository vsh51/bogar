using Bogar.BLL.Core;

namespace Bogar.BLL.Player;

/// <summary>
/// Abstraction for anything that can supply moves to the Game loop.
/// </summary>
public interface IPlayer
{
    string GetMove(List<Move> moves);
}
