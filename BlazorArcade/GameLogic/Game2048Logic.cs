using System;
using System.Collections.Generic;

namespace BlazorArcade.GameLogic
{
    public class Game2048Logic
    {
        public int Size { get; private set; } = 4;
        public int[,] Grid { get; private set; }
        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsGameWon { get; private set; }
        public bool HasWonPreviously { get; private set; }
        
        // This keeps track of tile origins for animation (Optional but nice)
        // For simplicity in a Blazor re-render, we'll just keep the raw grid here
        
        private Random _rnd = new Random();

        public Game2048Logic()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            Grid = new int[Size, Size];
            Score = 0;
            IsGameOver = false;
            IsGameWon = false;
            HasWonPreviously = false;
            
            AddRandomTile();
            AddRandomTile();
        }

        private void AddRandomTile()
        {
            List<(int, int)> emptySpots = new List<(int, int)>();
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    if (Grid[r, c] == 0)
                        emptySpots.Add((r, c));
                }
            }

            if (emptySpots.Count > 0)
            {
                var spot = emptySpots[_rnd.Next(emptySpots.Count)];
                Grid[spot.Item1, spot.Item2] = _rnd.Next(10) < 9 ? 2 : 4; // 90% chance of 2, 10% chance of 4
            }
        }

        public bool Move(Direction direction)
        {
            if (IsGameOver) return false;

            bool moved = false;
            int[,] newGrid = new int[Size, Size];
            Array.Copy(Grid, newGrid, Grid.Length);

            switch (direction)
            {
                case Direction.Up:
                    for (int c = 0; c < Size; c++) moved |= SlideAndMergeColumn(newGrid, c, true);
                    break;
                case Direction.Down:
                    for (int c = 0; c < Size; c++) moved |= SlideAndMergeColumn(newGrid, c, false);
                    break;
                case Direction.Left:
                    for (int r = 0; r < Size; r++) moved |= SlideAndMergeRow(newGrid, r, true);
                    break;
                case Direction.Right:
                    for (int r = 0; r < Size; r++) moved |= SlideAndMergeRow(newGrid, r, false);
                    break;
                case Direction.None:
                default:
                    return false;
            }

            if (moved)
            {
                Grid = newGrid;
                AddRandomTile();
                CheckGameState();
            }

            return moved;
        }

        private bool SlideAndMergeRow(int[,] grid, int r, bool left)
        {
            bool moved = false;
            List<int> current = new List<int>();
            for (int c = 0; c < Size; c++)
            {
                int val = grid[r, left ? c : Size - 1 - c];
                if (val != 0) current.Add(val);
            }

            List<int> merged = MergeLine(current, ref moved);

            for (int c = 0; c < Size; c++)
            {
                int oldVal = grid[r, c];
                int newVal = 0;
                if (left && c < merged.Count) newVal = merged[c];
                else if (!left && (Size - 1 - c) < merged.Count) newVal = merged[Size - 1 - c];

                grid[r, c] = newVal;
                if (oldVal != newVal) moved = true;
            }

            return moved;
        }

        private bool SlideAndMergeColumn(int[,] grid, int c, bool up)
        {
            bool moved = false;
            List<int> current = new List<int>();
            for (int r = 0; r < Size; r++)
            {
                int val = grid[up ? r : Size - 1 - r, c];
                if (val != 0) current.Add(val);
            }

            List<int> merged = MergeLine(current, ref moved);

            for (int r = 0; r < Size; r++)
            {
                int oldVal = grid[r, c];
                int newVal = 0;
                if (up && r < merged.Count) newVal = merged[r];
                else if (!up && (Size - 1 - r) < merged.Count) newVal = merged[Size - 1 - r];

                grid[r, c] = newVal;
                if (oldVal != newVal) moved = true;
            }

            return moved;
        }

        private List<int> MergeLine(List<int> line, ref bool moved)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < line.Count; i++)
            {
                if (i < line.Count - 1 && line[i] == line[i + 1])
                {
                    int mergedVal = line[i] * 2;
                    result.Add(mergedVal);
                    Score += mergedVal;
                    if (mergedVal == 2048)
                    {
                        IsGameWon = true;
                    }
                    i++; // skip the merged element
                    moved = true;
                }
                else
                {
                    result.Add(line[i]);
                }
            }
            return result;
        }

        public void ContinuePlaying()
        {
            if (IsGameWon && !HasWonPreviously)
            {
                HasWonPreviously = true;
                IsGameWon = false;
            }
        }

        private void CheckGameState()
        {
            // Check for empty spots
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    if (Grid[r, c] == 0) return; // Not game over
                }
            }

            // Check for possible merges
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    int val = Grid[r, c];
                    if (c < Size - 1 && Grid[r, c + 1] == val) return;
                    if (r < Size - 1 && Grid[r + 1, c] == val) return;
                }
            }

            IsGameOver = true;
        }
    }
}
