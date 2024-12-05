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

    private void SolvePuzzle()
    {
        Grid = sudokuSolver.Solve(Grid);

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
}
