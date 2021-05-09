using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

		public void SetPiece(int myPiece)
		{
			this.myPiece = myPiece;
			opponentPiece = Opponent(myPiece);
		}

		public void InitBoard()
        {
            Array.Clear(board, Piece.Empty, board.Length);

            board[3, 3] = Piece.Black; board[3, 4] = Piece.White;
            board[4, 3] = Piece.White; board[4, 4] = Piece.Black;
		}

        public bool IsAvailable(int piece, int row, int col)
		{
			int opponentPiece = Opponent(piece);

			if (board[row, col] != Piece.Empty)
			{
				return false;
			}

			for (int rowDirection = -1; rowDirection <= 1; rowDirection++)
			{
				for (int colDirection = -1; colDirection <= 1; colDirection++)
				{
					if (rowDirection == 0 && colDirection == 0)
					{
						continue;
					}

					try
					{
						if (board[row + rowDirection, col + colDirection] == opponentPiece)
						{
							int tempRow = row;
							int tempCol = col;

							while (true)
							{
								tempRow += rowDirection;
								tempCol += colDirection;

								if (board[tempRow, tempCol] == Piece.Empty)
								{
									break;
								}
								else if (board[tempRow, tempCol] == piece)
								{
									return true;
								}
							}
						}
					}
					catch (IndexOutOfRangeException)
                    {
						continue;
                    }
				}
			}
			return false;
		}

		public bool HasAvailablePlace(int piece)
		{
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 8; x++)
				{
					if (IsAvailable(piece, x, y))
					{
						return true;
					}
				} 
			}
			return false;
		}

		public void PlacePiece(int piece, int row, int col)
		{
			int opponentPiece = Opponent(piece);

			board[row, col] = piece;

			for (int rowDirection = -1; rowDirection <= 1; rowDirection++)
			{
				for (int colDirection = -1; colDirection <= 1; colDirection++)
				{
					if (rowDirection == 0 && colDirection == 0)
					{
						continue;
					}

					try
					{
						if (board[row + rowDirection, col + colDirection] == opponentPiece)
						{
							int tempRow = row;
							int tempCol = col;

							while (true)
							{
								tempRow += rowDirection;
								tempCol += colDirection;

								if (board[tempRow, tempCol] == Piece.Empty)
								{
									break;
								}
								else if (board[tempRow, tempCol] == piece)
								{
									while (row != tempRow || col != tempCol)
									{
										board[tempRow, tempCol] = piece;

										tempRow -= rowDirection;
										tempCol -= colDirection;
									}

									break;
								}
							}
						}
					}
					catch (IndexOutOfRangeException)
                    {
						continue;
                    }
				}
			}
		}

		public static int Opponent(int piece)
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
