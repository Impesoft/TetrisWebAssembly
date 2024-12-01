using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TetrisWebAssembly.GameLogic;

namespace TetrisWebAssembly.Pages;

public partial class Home : ComponentBase
{
    private const int BoardWidth = 10;
    private const int BoardHeight = 20;
    private const int BlockSize = 30;

    private ElementReference TetrisContainer; // Reference for key input
    private Game GameInstance = new Game(BlockSize); // The game instance
    private PeriodicTimer? GameTimer;
    private CancellationTokenSource? Cts;

    private int SvgWidth => BoardWidth * BlockSize;
    private int SvgHeight => BoardHeight * BlockSize;

    protected override void OnInitialized()
    {
        // Initialize the game instance
        GameInstance = new Game(BlockSize);
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
            }
        }
        catch (OperationCanceledException)
        {
            // Timer canceled when the game is paused or stopped
        }
    }

    private void PauseGame()
    {
        Cts?.Cancel(); // Cancel the game loop
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
