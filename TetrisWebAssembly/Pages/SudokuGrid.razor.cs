using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using TetrisWebAssembly.GameLogic;

namespace TetrisWebAssembly.Pages;
public partial class SudokuGrid
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    public List<List<string>>? Grid = [];
    private SudokuSolver sudokuSolver = new SudokuSolver();
    private Difficulty difficulty = Difficulty.UnSelected;
    private bool[][]? IsValid; // Add this as a field in your class
    private bool[][]? IsPrefilled;
    private int currentRow = 0;
    private int currentCol = 0;
    private readonly Random _random = new();


    private void SolvePuzzle()
    {
        if (Grid == null)
            return;
        Grid = sudokuSolver.Solve(Grid);

    }
    // similar to the previous method, but this one will be called when the user clicks a button to solve only one field on the puzzle
    private void SolveOneField()
    {
        if (Grid == null)
            return;
        if (currentRow is < 0 or >= 9 || currentCol is < 0 or >= 9)
            return;

        if (!string.IsNullOrWhiteSpace(Grid[currentRow][currentCol]))
          (currentRow,currentCol) =  FindNextEmptyField(Grid) ?? (0,0);

        var candidates = Enumerable.Range(1, 9).OrderBy(_ => _random.Next());

        foreach (var num in candidates)
        {
            var numStr = num.ToString();
            if (!sudokuSolver.IsValidPlacement(Grid, currentRow, currentCol, numStr))
                continue;

            Grid[currentRow][currentCol] = numStr;

            var gridCopy = Grid.Select(row => row.ToList()).ToList(); // Deep clone
            if (sudokuSolver.Solve(gridCopy) is not null)
                break; // This number leads to a solvable grid

            Grid[currentRow][currentCol] = ""; // Backtrack and try next candidate
        }
    }
    protected override void OnInitialized()
    {
        // Initialize the grid with test data

        Grid = Enumerable.Range(0, 9)
                     .Select(_ => Enumerable.Repeat("", 9).ToList())
                     .ToList();
        InitializeValidationGrid();
        InitializePrefilledGrid();

        //sudokuSolver.GenerateSudokuGrid(difficulty);
    }
    private void InitializeValidationGrid()
    {
        if (Grid == null)
            return;
        IsValid = new bool[Grid.Count][];
        for (int row = 0; row < Grid.Count; row++)
        {
            IsValid[row] = new bool[Grid[row].Count];
            for (int col = 0; col < Grid[row].Count; col++)
            {
                IsValid[row][col] = true; // Initially, all cells are valid
            }
        }
    }
    private void InitializePrefilledGrid()
    {
        if (Grid == null)
            return;
        IsPrefilled = new bool[9][];
        for (int row = 0; row < 9; row++)
        {
            IsPrefilled[row] = new bool[9];
            for (int col = 0; col < Grid[row].Count; col++)
            {
                IsPrefilled[row][col] = !string.IsNullOrWhiteSpace(Grid[row][col]); // Mark prefilled cells
            }
        }
    }

    private async Task OnDifficultyChanged()
    {
        // Perform some action when the selection changes
        Grid = sudokuSolver.GenerateSudokuGrid(difficulty);
        InitializePrefilledGrid();
        await JSRuntime.InvokeVoidAsync("eval", "document.getElementById('bg').play()");
    }

    public (int row, int col)? FindNextEmptyField(List<List<string>> grid)
    {

        for (int row = currentRow; row < grid.Count; row++)
        {
            for (int col = currentCol; col < grid[row].Count; col++)
            {
                if (string.IsNullOrWhiteSpace(grid[row][col]))
                {
                    return (row, col); // Return the first empty cell
                }
            }
            currentCol = 0; // Reset column for the next row
        }
        if (currentRow > 0)
        {
            currentRow = 0; // Reset column for the next row to make sure we start from the beginning of the grid again
            return FindNextEmptyField(grid); // Recursively check the next row
        }
        return null; // Return null if no empty cell is found
    }

    private void SetGridValue(int row, int col, ChangeEventArgs args)
    {
        if (Grid == null)
            return;
        var value = args.Value?.ToString() ?? "";
        Grid[row][col] = value;

        // Validate the placement
        if (IsValid != null)
        {
            var isValid = sudokuSolver.IsValidPlacement(Grid, row, col, value);
            IsValid[row][col] = isValid;
        }

        // Optionally, trigger a re-render if needed
        StateHasChanged();
    }
    // similar to the previous method, but this one will be called when the user clicks a cell to set the current cell position
    private void SetCurrentCell(int row, int col, EventArgs args)
    {
        currentRow = row;
        currentCol = col;
    }

}
