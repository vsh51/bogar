using System;
using System.Collections.Generic;

namespace Bogar.UI.Chess
{
    public class Board
    {
        private PieceBase?[,] board = new PieceBase?[8, 8];

        private Dictionary<PieceColor, Dictionary<PieceType, int>> remainingToPlace
            = new Dictionary<PieceColor, Dictionary<PieceType, int>>();

        public Board()
        {
            board = new PieceBase?[8, 8];
            InitRemaining();
        }

        private void InitRemaining()
        {
            remainingToPlace = new Dictionary<PieceColor, Dictionary<PieceType, int>>()
            {
                { PieceColor.White, new Dictionary<PieceType,int>() },
                { PieceColor.Black, new Dictionary<PieceType,int>() }
            };

            var standard = new Dictionary<PieceType, int>()
            {
                { PieceType.King, 1 },
                { PieceType.Queen, 1 },
                { PieceType.Rook, 2 },
                { PieceType.Bishop, 2 },
                { PieceType.Knight, 2 },
                { PieceType.Pawn, 8 }
            };

            foreach (var kv in standard)
            {
                remainingToPlace[PieceColor.White][kv.Key] = kv.Value;
                remainingToPlace[PieceColor.Black][kv.Key] = kv.Value;
            }
        }

        public PieceBase? GetPieceAt(int row, int col)
        {
            if (row < 0 || row >= 8 || col < 0 || col >= 8) return null;
            return board[row, col];
        }

        public void SetPieceAt(int row, int col, PieceBase? piece)
        {
            if (row < 0 || row >= 8 || col < 0 || col >= 8) return;
            board[row, col] = piece;
        }

        public static bool TryParseSquare(string sq, out (int row, int col) rc)
        {
            rc = (-1, -1);
            if (string.IsNullOrWhiteSpace(sq) || sq.Length < 2) return false;
            sq = sq.Trim().ToLower();
            char file = sq[0];
            char rank = sq[1];
            if (file < 'a' || file > 'h') return false;
            if (rank < '1' || rank > '8') return false;
            int col = file - 'a';
            int row = 8 - (rank - '0');
            rc = (row, col);
            return true;
        }

        public static string AlgebraicFromRC(int row, int col)
        {
            char file = (char)('a' + col);
            char rank = (char)('0' + (8 - row));
            return $"{file}{rank}";
        }

        public int RemainingOf(PieceColor color, PieceType type) => remainingToPlace[color][type];

        public bool PlaceBySan(PieceColor color, string san, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(san)) { error = "Порожня команда"; return false; }

            string s = san.Replace("+", "").Replace("#", "").Trim();
            s = s.Replace("x", "").Replace("X", "");

            if (s.Length < 2) { error = "Невірний формат команди"; return false; }

            PieceType type;
            char first = s[0];
            if ("NBRQK".IndexOf(first) >= 0)
            {
                type = first switch
                {
                    'N' => PieceType.Knight,
                    'B' => PieceType.Bishop,
                    'R' => PieceType.Rook,
                    'Q' => PieceType.Queen,
                    'K' => PieceType.King,
                    _ => PieceType.Pawn
                };
            }
            else
            {
                type = PieceType.Pawn;
            }

            string target = s.Substring(s.Length - 2, 2);
            if (!TryParseSquare(target, out var rc)) { error = "Невірна цільова клітина"; return false; }
            int tr = rc.row, tc = rc.col;

            if (GetPieceAt(tr, tc) != null) { error = "Клітинка вже зайнята"; return false; }
            if (remainingToPlace[color][type] <= 0) { error = $"У гравця {color} більше немає фігур типу {type} для розміщення"; return false; }

            PieceBase piece = type switch
            {
                PieceType.Pawn => new Pawn(color),
                PieceType.Knight => new Knight(color),
                PieceType.Bishop => new Bishop(color),
                PieceType.Rook => new Rook(color),
                PieceType.Queen => new Queen(color),
                PieceType.King => new King(color),
                _ => new Pawn(color)
            };

            SetPieceAt(tr, tc, piece);
            remainingToPlace[color][type]--;
            piece.HasMoved = true;
            return true;
        }
    }
}
