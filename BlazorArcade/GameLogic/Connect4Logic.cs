using System;

namespace BlazorArcade.GameLogic
{
    public class Connect4Logic
    {
        public const int Rows = 6;
        public const int Columns = 7;
        
        // 0 = empty, 1 = player 1, 2 = player 2
        // Row 0 is the top, Row 5 is the bottom
        public int[,] Board { get; private set; } = new int[Rows, Columns];
        public int CurrentPlayer { get; private set; } = 1;
        public int Winner { get; private set; } = 0;
        public bool IsGameOver { get; private set; } = false;
        public bool IsDraw { get; private set; } = false;

        public void InitializeGame()
        {
            Board = new int[Rows, Columns];
            CurrentPlayer = 1;
            Winner = 0;
            IsGameOver = false;
            IsDraw = false;
        }

        public bool DropPiece(int column)
        {
            if (IsGameOver || column < 0 || column >= Columns) return false;

            // Find the lowest empty row in this column
            int rowToDrop = -1;
            for (int r = Rows - 1; r >= 0; r--)
            {
                if (Board[r, column] == 0)
                {
                    rowToDrop = r;
                    break;
                }
            }

            // Column is full
            if (rowToDrop == -1) return false;

            Board[rowToDrop, column] = CurrentPlayer;

            if (CheckWin(rowToDrop, column))
            {
                Winner = CurrentPlayer;
                IsGameOver = true;
            }
            else if (CheckDraw())
            {
                IsDraw = true;
                IsGameOver = true;
            }
            else
            {
                CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
            }

            return true;
        }

        private bool CheckWin(int row, int col)
        {
            int player = Board[row, col];

            return CountDirection(row, col, 0, 1) + CountDirection(row, col, 0, -1) - 1 >= 4 || // Horizontal
                   CountDirection(row, col, 1, 0) + CountDirection(row, col, -1, 0) - 1 >= 4 || // Vertical
                   CountDirection(row, col, 1, 1) + CountDirection(row, col, -1, -1) - 1 >= 4 || // Diagonal /
                   CountDirection(row, col, 1, -1) + CountDirection(row, col, -1, 1) - 1 >= 4;  // Diagonal \
        }

        private int CountDirection(int row, int col, int dRow, int dCol)
        {
            int player = Board[row, col];
            int count = 0;
            int r = row;
            int c = col;

            while (r >= 0 && r < Rows && c >= 0 && c < Columns && Board[r, c] == player)
            {
                count++;
                r += dRow;
                c += dCol;
            }

            return count;
        }

        private bool CheckDraw()
        {
            for (int c = 0; c < Columns; c++)
            {
                if (Board[0, c] == 0) return false; // Found an empty spot in the top row
            }
            return true;
        }
    }
}
