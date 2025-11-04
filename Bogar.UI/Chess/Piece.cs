using System;
using System.Windows.Media.Imaging;
using System.Windows;


namespace Bogar.UI.Chess
{
    public enum PieceColor { White, Black }
    public enum PieceType { Pawn, Knight, Bishop, Rook, Queen, King }

    public abstract class PieceBase
    {
        public PieceType Type { get; }
        public PieceColor Color { get; }
        public bool HasMoved { get; set; } = false;
        public abstract BitmapImage Image { get; }

        protected PieceBase(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }
    }

    public class Pawn : PieceBase
    {
        public Pawn(PieceColor color) : base(PieceType.Pawn, color) { }
        public override BitmapImage Image => (BitmapImage)Application.Current.Resources[Color == PieceColor.White ? "wP" : "bP"];
    }

    public class Knight : PieceBase
    {
        public Knight(PieceColor color) : base(PieceType.Knight, color) { }
        public override BitmapImage Image => (BitmapImage)Application.Current.Resources[Color == PieceColor.White ? "wN" : "bN"];
    }

    public class Bishop : PieceBase
    {
        public Bishop(PieceColor color) : base(PieceType.Bishop, color) { }
        public override BitmapImage Image => (BitmapImage)Application.Current.Resources[Color == PieceColor.White ? "wB" : "bB"];
    }

    public class Rook : PieceBase
    {
        public Rook(PieceColor color) : base(PieceType.Rook, color) { }
        public override BitmapImage Image => (BitmapImage)Application.Current.Resources[Color == PieceColor.White ? "wR" : "bR"];
    }

    public class Queen : PieceBase
    {
        public Queen(PieceColor color) : base(PieceType.Queen, color) { }
        public override BitmapImage Image => (BitmapImage)Application.Current.Resources[Color == PieceColor.White ? "wQ" : "bQ"];
    }

    public class King : PieceBase
    {
        public King(PieceColor color) : base(PieceType.King, color) { }
        public override BitmapImage Image => (BitmapImage)Application.Current.Resources[Color == PieceColor.White ? "wK" : "bK"];
    }
}
