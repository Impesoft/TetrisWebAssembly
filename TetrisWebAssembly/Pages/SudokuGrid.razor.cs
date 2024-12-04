using Microsoft.AspNetCore.Components;

namespace TetrisWebAssembly.Pages;
public partial class SudokuGrid
{
    public List<List<string>> Grid = [];

    private void SolvePuzzle()
    {
        var solver = new SudokuSolver();
        Grid = solver.Solve(Grid);
    }
    protected override void OnInitialized()
    {
        // Initialize the grid with test data
        Grid = new List<List<string>>
                {
                    new List<string> { "5", "3", "", "", "7", "", "", "", "" },
                    new List<string> { "6", "", "", "1", "9", "5", "", "", "" },
                    new List<string> { "", "9", "8", "", "", "", "", "6", "" },
                    new List<string> { "8", "", "", "", "6", "", "", "", "3" },
                    new List<string> { "4", "", "", "8", "", "3", "", "", "1" },
                    new List<string> { "7", "", "", "", "2", "", "", "", "6" },
                    new List<string> { "", "6", "", "", "", "", "2", "8", "" },
                    new List<string> { "", "", "", "4", "1", "9", "", "", "5" },
                    new List<string> { "", "", "", "", "8", "", "", "7", "9" }
                };
    }
    private void SetGridValue(int row, int col, ChangeEventArgs args) 
    {
        // Set the value of the cell at position (x, y)
        Grid[row][col] = args.Value?.ToString() ?? "";
    }
}

public class SudokuSolver
{
    public List<List<string>> Solve(List<List<string>> puzzle)
    {
        var sudokuGrid = puzzle
            .Select(row => row
                .Select(cell => (cell.Trim() != "" && int.TryParse(cell, out int n) && n >= 1 && n <= 9) ? cell : " ")
                .ToList())
            .ToList();

        if (SolveSudoku(sudokuGrid))
        {
            return sudokuGrid;
        }

        throw new Exception("Sudoku puzzle cannot be solved.");
    }
    private bool SolveSudoku(List<List<string>> puzzle)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (puzzle[row][col] == " ")
                {
                    for (int num = 1; num <= 9; num++)
                    {
                        string numStr = num.ToString();
                        if (IsValidPlacement(puzzle, row, col, numStr))
                        {
                            puzzle[row][col] = numStr;

                            // Recurse to solve the next cell
                            if (SolveSudoku(puzzle))
                            {
                                return true;
                            }

                            // Backtrack
                            puzzle[row][col] = " ";
                        }
                    }

                    // If no valid number is found, return false
                    return false;
                }
            }
        }

        // If no empty cells are left, the puzzle is solved
        return true;
    }
    bool IsValidPlacement(List<List<string>> grid, int row, int col, string num)
    {
        // Check row and column
        if (grid[row].Contains(num) || grid.Select(r => r[col]).Contains(num)) return false;

        // Check 3x3 subgrid
        int startRow = (row / 3) * 3;
        int startCol = (col / 3) * 3;
        var subgrid = GetSubgrid(grid, startRow, startCol);
        return !subgrid.Contains(num);
    }
    bool IsValidSudoku(List<List<string>> grid)
    {
        // Validate rows
        foreach (var row in grid)
        {
            if (!HasUniqueValues(row)) return false;
        }

        // Validate columns
        for (int col = 0; col < 9; col++)
        {
            var column = grid.Select(row => row[col]).ToList();
            if (!HasUniqueValues(column)) return false;
        }

        // Validate 3x3 subgrids
        for (int row = 0; row < 9; row += 3)
        {
            for (int col = 0; col < 9; col += 3)
            {
                var subgrid = GetSubgrid(grid, row, col);
                if (!HasUniqueValues(subgrid)) return false;
            }
        }

        return true;
    }

    bool HasUniqueValues(List<string> cells)
    {
        var numbers = cells.Where(cell => cell != " ").ToList();
        return numbers.Count == numbers.Distinct().Count();
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
