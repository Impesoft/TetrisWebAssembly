using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using TetrisWebAssembly.GameLogic;

namespace TetrisWebAssembly.Pages;
public partial class Tetris
{
    private const int BoardWidth = 10;
    private const int BoardHeight = 20;
    private const int BlockSize = 50;

    private ElementReference TetrisContainer; // Reference for key input
    private TetrisGame GameInstance = new TetrisGame(BlockSize, BoardWidth, BoardHeight); // The game instance
    private PeriodicTimer? GameTimer;
    private CancellationTokenSource? Cts;

    private int SvgWidth => BoardWidth * BlockSize;
    private int SvgHeight => BoardHeight * BlockSize;

    protected override void OnInitialized()
    {
        // Initialize the game instance
        GameInstance = new TetrisGame(BlockSize,BoardWidth,BoardHeight);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await TetrisContainer.FocusAsync(); // Ensure focus on the container for key input
        }
    }

    private async Task StartGame()
    {
        await TetrisContainer.FocusAsync(); // Ensure focus on the container for key input
        if (GameInstance.IsGameOver)
        {
            GameInstance.StartNewGame(); // Reset the game if it's over
        }

        // Start the game loop
        Cts = new CancellationTokenSource();
        GameTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(GameInstance.GetDropInterval()));

        try
        {
            while (await GameTimer.WaitForNextTickAsync(Cts.Token))
            {
                if (!GameInstance.IsGameOver)
                {
                    GameInstance.MoveTetrominoDown(); // Move the tetromino down on each tick
                    StateHasChanged(); // Trigger UI update
                }
                GameTimer?.Dispose();
                GameTimer = null;
                GameTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(GameInstance.GetDropInterval()));
            }
        }
        catch (OperationCanceledException)
        {
            // Timer canceled when the game is paused or stopped
            GameTimer?.Dispose();
            GameTimer = null;
        }
    }

    private async Task PauseGame()
    {
        if (GameInstance.IsRunning && Cts?.Token is not null)
        {
            Cts?.Cancel(); // Cancel the game loop
            Cts?.Dispose();
        }
        else
        {
            await StartGame();
        }
    }

    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (GameInstance.IsGameOver)
            return;

        switch (e.Key)
        {
            case "ArrowLeft":
                GameInstance.MoveTetrominoLeft();
                break;

            case "ArrowRight":
                GameInstance.MoveTetrominoRight();
                break;

            case "ArrowDown":
                GameInstance.MoveTetrominoDown(); // Accelerate the drop
                break;

            case "ArrowUp":
                GameInstance.RotateTetromino();
                break;
        }

        await TetrisContainer.FocusAsync(); // Refocus on the container for key input
        StateHasChanged(); // Update the UI
    }

}
