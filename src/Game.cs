using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OthelloSharp.src
{
    class Game
    {
        public static class Piece
        {
            public const int Empty = 0;
            public const int Black = 1;
            public const int White = 2;
        }

        public int turn;
        public int myPiece, opponentPiece;
        public double myTime, opponentTime;
        public int[,] board = new int[8, 8];

        public void InitBoard()
        {
            Array.Clear(board, Piece.Empty, board.Length);

            board[3, 3] = Piece.Black; board[3, 4] = Piece.White;
            board[4, 3] = Piece.White; board[4, 4] = Piece.Black;
        }

        public static int InvertPiece(int piece)
        {
            if (piece == Piece.Black)
            {
                return Piece.White;
            }
            else
            {
                return Piece.Black;
            }
        }
    }
}
