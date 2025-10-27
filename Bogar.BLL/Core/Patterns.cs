using System;

namespace Bogar.BLL.Core;

public static class Patterns
{
    public static ulong WhitePawnAttackPattern(Square target)
        => throw new NotImplementedException();

    public static ulong BlackPawnAttackPattern(Square target)
        => throw new NotImplementedException();

    public static readonly Func<Square, ulong>[] PawnAttackPattern = new Func<Square, ulong>[]
    {
        WhitePawnAttackPattern,
        BlackPawnAttackPattern
    };

    public static ulong KnightAttackPattern(Square target)
        => throw new NotImplementedException();

    public static ulong KingAttackPattern(Square target)
        => throw new NotImplementedException();

    public static ulong RookAttackPattern(Square target)
        => throw new NotImplementedException();

    public static ulong BishopAttackPattern(Square target)
        => throw new NotImplementedException();

    public static ulong QueenAttackPattern(Square target)
        => throw new NotImplementedException();
}
