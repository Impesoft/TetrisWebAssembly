using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorArcade.GameLogic
{
    public class Connect4AI
    {
        private int _aiPlayer;
        private int _humanPlayer;

        public Connect4AI(int aiPlayer = 2)
        {
            _aiPlayer = aiPlayer;
            _humanPlayer = aiPlayer == 1 ? 2 : 1;
        }

        public int GetBestMove(int[,] board)
        {
            // 1. Can AI win?
            for (int col = 0; col < Connect4Logic.Columns; col++)
            {
                if (CanPlay(board, col) && IsWinningMove(board, col, _aiPlayer))
                {
                    return col;
                }
            }

            // 2. Can Human win? (Block them)
            for (int col = 0; col < Connect4Logic.Columns; col++)
            {
                if (CanPlay(board, col) && IsWinningMove(board, col, _humanPlayer))
                {
                    return col;
                }
            }

            // 3. Fallback: prefer the center columns
            int[] columnPreferences = { 3, 2, 4, 1, 5, 0, 6 };
            
            // Collect valid moves, but avoid moves that give the human an immediate win on the next turn
            List<int> safeMoves = new List<int>();
            List<int> validMoves = new List<int>();

            foreach (var col in columnPreferences)
            {
                if (CanPlay(board, col))
                {
                    validMoves.Add(col);
                    // Check if playing here gives the opponent a winning move on top of this one
                    if (!GivesOpponentWin(board, col))
                    {
                        safeMoves.Add(col);
                    }
                }
            }

            if (safeMoves.Count > 0)
            {
                return safeMoves.First();
            }
            
            if (validMoves.Count > 0)
            {
                return validMoves.First();
            }

            return -1; // No valid moves
        }

        private bool CanPlay(int[,] board, int col)
        {
            return board[0, col] == 0;
        }

        private int GetNextAvailableRow(int[,] board, int col)
        {
            for (int r = Connect4Logic.Rows - 1; r >= 0; r--)
            {
                if (board[r, col] == 0) return r;
            }
            return -1;
        }

        private bool IsWinningMove(int[,] board, int col, int player)
        {
            int row = GetNextAvailableRow(board, col);
            if (row == -1) return false;

            // Make a temporary move
            board[row, col] = player;
            bool win = CheckWin(board, row, col, player);
            // Undo the move
            board[row, col] = 0;

            return win;
        }

        private bool GivesOpponentWin(int[,] board, int col)
        {
            int row = GetNextAvailableRow(board, col);
            if (row <= 0) return false; // No space above

            // Simulate AI playing in 'row'
            board[row, col] = _aiPlayer;

            // Does this allow human to win in 'row - 1'?
            board[row - 1, col] = _humanPlayer;
            bool givesWin = CheckWin(board, row - 1, col, _humanPlayer);

            // Undo
            board[row - 1, col] = 0;
            board[row, col] = 0;

            return givesWin;
        }

        private bool CheckWin(int[,] board, int row, int col, int player)
        {
            return CountDirection(board, row, col, 0, 1, player) + CountDirection(board, row, col, 0, -1, player) - 1 >= 4 ||
                   CountDirection(board, row, col, 1, 0, player) + CountDirection(board, row, col, -1, 0, player) - 1 >= 4 ||
                   CountDirection(board, row, col, 1, 1, player) + CountDirection(board, row, col, -1, -1, player) - 1 >= 4 ||
                   CountDirection(board, row, col, 1, -1, player) + CountDirection(board, row, col, -1, 1, player) - 1 >= 4;
        }

        private int CountDirection(int[,] board, int row, int col, int dRow, int dCol, int player)
        {
            int count = 0;
            int r = row;
            int c = col;

            while (r >= 0 && r < Connect4Logic.Rows && c >= 0 && c < Connect4Logic.Columns && board[r, c] == player)
            {
                count++;
                r += dRow;
                c += dCol;
            }

            return count;
        }
    }
}
