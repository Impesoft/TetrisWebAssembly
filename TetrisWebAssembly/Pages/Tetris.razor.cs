using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using TetrisWebAssembly.GameLogic;

namespace TetrisWebAssembly.Pages;
public partial class Tetris
{
    private static int PreviewBoardWidth = 4;
    private static int BoardWidth = 10;
    private static int PreviewBoardHeight = 4;
    private static int BoardHeight = 20;
    private static int BlockSize = 50;

    [Inject] private IJSRuntime JSRuntime { get; set; }

    private ElementReference TetrisContainer; // Reference for key input
    private static CancellationTokenSource? Cts = new CancellationTokenSource();
    private TetrisGame GameInstance = new TetrisGame(BlockSize, BoardWidth, BoardHeight, Cts); // The game instance
    private PeriodicTimer? GameTimer;

    private int SvgWidth;
    private int PreviewSvgWidth;
    private int SvgHeight;
    private int PreviewSvgHeight;

    protected override void OnInitialized()
    {
        // Initialize the game instance
    }
    private async Task<int> CalculateBlockSize()
    {
        int viewportWidth = (int)(await JSRuntime.InvokeAsync<double>("eval", "window.innerWidth"));
        int viewportHeight = (int)(await JSRuntime.InvokeAsync<double>("eval", "window.innerHeight"));
        var maxBlockWidth = Math.Min(viewportWidth / (BoardWidth + 10), 50);
        var maxBlockHeight = (int)Math.Min((viewportHeight / BoardHeight)*.8, 50);
        var blockSize = Math.Min(Math.Min(maxBlockWidth, maxBlockHeight), 50);
        return blockSize;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            BlockSize = await CalculateBlockSize();
            SvgWidth = BoardWidth * BlockSize;
            SvgHeight = BoardHeight * BlockSize;
            PreviewSvgWidth = PreviewBoardWidth * BlockSize;
            PreviewSvgHeight = PreviewBoardHeight * BlockSize;
            GameInstance = new TetrisGame(BlockSize, BoardWidth, BoardHeight, Cts);
            StateHasChanged();
            await TetrisContainer.FocusAsync(); // Ensure focus on the container for key input
        }
    }

    private async Task StartGame()
    {
        Cts ??= new CancellationTokenSource();
        await TetrisContainer.FocusAsync(); // Ensure focus on the container for key input
        if (GameInstance.IsGameOver)
        {
            GameInstance.StartNewGame(); // Reset the game if it's over
        }
        // Start the background video
        // BackgroundVideo is an ElementReference to the video element
        await JSRuntime.InvokeVoidAsync("eval", "document.getElementById('bg').play()");


        // Start the game loop
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
            await JSRuntime.InvokeVoidAsync("eval", "document.getElementById('bg').pause()");
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
            Cts = null;
        }
        else
        {
            Cts ??= new CancellationTokenSource();
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
    private async Task HandlePointerDown(PointerEventArgs e)
    {
        if (GameInstance.IsGameOver)
            return;
        if (e.ClientX < SvgWidth / 2)
        {
            GameInstance.MoveTetrominoLeft();
        }
        else
        {
            GameInstance.MoveTetrominoRight();
        }
        if (e.ClientY > SvgHeight / 2)
        {
            GameInstance.MoveTetrominoDown();
        }
        else
        {
            GameInstance.RotateTetromino();
        }
        await TetrisContainer.FocusAsync(); // Refocus on the container for key input
        StateHasChanged(); // Update the UI
    }
}
