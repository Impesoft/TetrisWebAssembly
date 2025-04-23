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
    private async Task SolveOneField()
    {
        if (Grid == null)
            return;
        if (currentRow is < 0 or >= 9 || currentCol is < 0 or >= 9)
            return;

        if (!string.IsNullOrWhiteSpace(Grid[currentRow][currentCol]))
            (currentRow, currentCol) = FindNextEmptyField(Grid) ?? (0, 0);

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
        (int nextRow, int nextCol) = FindNextEmptyField(Grid) ?? (0, 0);
        await JSRuntime.InvokeVoidAsync("eval", $"document.getElementById('cell-{nextRow}-{nextCol}').focus()");

    }
    protected override void OnInitialized()
    {
        // Initialize the grid with test data

        Grid = Enumerable.Range(0, 9)
                     .Select(_ => Enumerable.Repeat("", 9).ToList())
                     .ToList();

        InitializePrefilledGrid();
        IsValid = Grid.Select(row => Enumerable.Repeat(true, row.Count).ToArray()).ToArray();
    }
    private void InitializePrefilledGrid()
    {
        if (Grid == null)
            return;

        IsPrefilled = Grid
            .Select(row => row.Select(cell => !string.IsNullOrWhiteSpace(cell)).ToArray())
            .ToArray();
    }

    private async Task OnDifficultyChanged()
    {
        // Perform some action when the selection changes
        if (difficulty == Difficulty.UnSelected)
        {
            Grid = Enumerable.Range(0, 9)
                     .Select(_ => Enumerable.Repeat("", 9).ToList())
                     .ToList();
            InitializePrefilledGrid();
            IsValid = Grid.Select(row => Enumerable.Repeat(true, row.Count).ToArray()).ToArray();
            return;
        }

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

    private async Task SetGridValue(int row, int col, ChangeEventArgs args)
    {
        if (Grid == null || IsValid == null)
            return;
        var value = args.Value?.ToString()?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 1 || !char.IsDigit(value[0]) || value == "0")
        {
            await JSRuntime.InvokeVoidAsync("eval", $"document.getElementById('cell-{row}-{col}').value = ''");
            return; // Invalid input: ignore
        }

        Grid[row][col] = ""; // Clear the cell first to avoid immediate validation failure

        // Validate the placement
        var isValid = sudokuSolver.IsValidPlacement(Grid, row, col, value);
        var breaksSolution = sudokuSolver.Solve(Grid) == null;
        IsValid[row][col] = isValid && !breaksSolution;

        Grid[row][col] = value;

        // Optionally, trigger a re-render if needed
        StateHasChanged();

        // Focus next empty cell
        var next = FindNextEmptyField(Grid);
        if (next.HasValue)
        {
            var (nextRow, nextCol) = next.Value;
            await JSRuntime.InvokeVoidAsync("eval", $"document.getElementById('cell-{nextRow}-{nextCol}').click()");
        }
    }
    // similar to the previous method, but this one will be called when the user clicks a cell to set the current cell position
    private void SetCurrentCell(int row, int col, EventArgs args)
    {
        currentRow = row;
        currentCol = col;
    }

}
