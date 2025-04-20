using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TetrisWebAssembly.GameLogic;

namespace TetrisWebAssembly.Pages;
public partial class SudokuGrid
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    public List<List<string>> Grid = [];
    private SudokuSolver sudokuSolver = new SudokuSolver();
    private Difficulty difficulty = Difficulty.UnSelected;
    private bool[][]? IsValid; // Add this as a field in your class
    private bool[][]? IsPrefilled;
    private int currentRow = 0;
    private int currentCol = 0;

    private void SolvePuzzle()
    {
        Grid = sudokuSolver.Solve(Grid);

    }
    // similar to the previous method, but this one will be called when the user clicks a button to solve only one field on the puzzle
    private void SolveOneField()
    {
        // Check if currentRow and currentCol are not out of bounds
        if (currentRow < 0 || currentRow >= Grid.Count || currentCol < 0 || currentCol >= Grid[currentRow].Count)
        {
            return; // Invalid cell position
        }

        // Check if the cell is empty
        if (string.IsNullOrWhiteSpace(Grid[currentRow][currentCol]))
        {
            // Try to solve the cell
            for (int num = 1; num <= 9; num++)
            {
                string numStr = num.ToString();
                if (sudokuSolver.IsValidPlacement(Grid, currentRow, currentCol, numStr))
                {
                    Grid[currentRow][currentCol] = numStr;
                    var isSolvable = sudokuSolver.Solve(Grid);
                    if (isSolvable is null)
                    {
                        Grid[currentRow][currentCol] = ""; // Reset the cell if it leads to an unsolvable state
                        continue;
                    }
                    break; // Exit the loop after placing a valid number
                }
            }
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

    private void SetGridValue(int row, int col, ChangeEventArgs args)
    {
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
