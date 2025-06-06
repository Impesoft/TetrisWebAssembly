﻿using System;

namespace TetrisWebAssembly.GameLogic;

public class SudokuSolver
{
    private Random _random = new Random();
    public List<List<string>> GenerateSudokuGrid(Difficulty difficulty)
    {
        // Step 1: Generate a complete valid Sudoku grid
        List<List<string>> fullGrid = GenerateFullSudoku();

        // Step 2: Remove numbers based on difficulty
        List<List<string>> puzzleGrid = RemoveNumbers(fullGrid, difficulty);

        // Step 3: Convert to a list of lists of strings
        return puzzleGrid;
    }
    private List<List<string>> GenerateFullSudoku()
    {
        var grid = Enumerable.Range(0, 9)
                     .Select(_ => Enumerable.Repeat("", 9).ToList())
                     .ToList();

        if (SolveSudoku(grid))
        {
            return grid;
        }

        throw new Exception("Failed to generate a complete Sudoku grid.");
    }

    public List<List<string>>? Solve(List<List<string>> puzzle)
    {
        var sudokuGrid = puzzle
            .Select(row => row
                .Select(cell => cell.Trim() != "" && int.TryParse(cell, out int n) && n >= 1 && n <= 9 ? cell : "")
                .ToList())
            .ToList();

        if (SolveSudoku(sudokuGrid))
        {
            return sudokuGrid;
        }

        return null; // Return null if the puzzle cannot be solved
    }
    private bool SolveSudoku(List<List<string>> puzzle)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (puzzle[row][col] == "")
                {
                    List<int> numbers = Enumerable.Range(1, 9).OrderBy(_ => _random.Next()).ToList();

                    foreach (int num in numbers)
                    {
                        string numStr = num.ToString();
                        if (IsValidPlacement(puzzle, row, col, numStr))
                        {
                            puzzle[row][col] = numStr;

                            if (SolveSudoku(puzzle))
                                return true;

                            puzzle[row][col] = "";
                        }
                    }

                    return false; // No valid number found
                }
            }
        }

        return true; // Puzzle is completely filled
    }
    private List<List<string>> RemoveNumbers(List<List<string>> grid, Difficulty difficulty)
    {
        var puzzleGrid = grid.Select(row => row.ToList()).ToList(); // Deep copy
        int cellsToRemove = difficulty switch
        {
            Difficulty.Easy => 30,
            Difficulty.Medium => 40,
            Difficulty.Hard => 50,
            _ => 40
        };

        while (cellsToRemove > 0)
        {
            int row = _random.Next(0, 9);
            int col = _random.Next(0, 9);

            if (puzzleGrid[row][col] != "")
            {
                puzzleGrid[row][col] = "";
                cellsToRemove--;
            }
        }

        return puzzleGrid;
    }
    public bool IsValidPlacement(List<List<string>> grid, int row, int col, string num)
    {
        // Check row and column
        if (grid[row].Contains(num) || grid.Select(r => r[col]).Contains(num)) return false;

        // Check 3x3 subGrid
        int startRow = row / 3 * 3;
        int startCol = col / 3 * 3;
        var subgrid = GetSubgrid(grid, startRow, startCol);
        var result = subgrid.Contains(num);
        return !result;
    }

    List<string> GetSubgrid(List<List<string>> grid, int startRow, int startCol)
    {
        var subgrid = new List<string>();
        for (int row = startRow; row < startRow + 3; row++)
        {
            for (int col = startCol; col < startCol + 3; col++)
            {
                subgrid.Add(grid[row][col]);
            }
        }
        return subgrid;
    }
}
public enum Difficulty
{
    Easy,
    Medium,
    Hard,
    UnSelected
}