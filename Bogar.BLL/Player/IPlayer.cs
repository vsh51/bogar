using Bogar.BLL.Core;

namespace Bogar.BLL.Player;

/// <summary>
/// Abstraction for anything that can supply moves to the Game loop.
/// </summary>
public interface IPlayer
{
    /// <summary>
    /// Returns the next move string given the history so far.
    /// </summary>
    string GetMove(List<Move> moves);
}
