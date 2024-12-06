using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using TetrisWebAssembly.GameLogic;

namespace TetrisWebAssembly.Pages;
public partial class Tetris
{
    private static double PreviewBoardWidth = 4;
    private static double BoardWidth = 10;
    private static double PreviewBoardHeight = 4;
    private static double BoardHeight = 20;
    private static double BlockSize = 50;
    private static double OffsetFieldLeft;
    private static double OffsetFieldBottom;

    public double OffsetFieldTop { get; private set; }
    [Inject] private IJSRuntime? JSRuntime { get; set; }

    private ElementReference TetrisContainer; // Reference for key input
    private static CancellationTokenSource? Cts = new CancellationTokenSource();
    private TetrisGame GameInstance = new TetrisGame(BlockSize, BoardWidth, BoardHeight, Cts); // The game instance
    private PeriodicTimer? GameTimer;

    private double SvgWidth;
    private double PreviewSvgWidth;
    private double SvgHeight;
    private double PreviewSvgHeight;

    protected override void OnInitialized()
    {
        // Initialize the game instance
    }
    private async Task CalculateBlockSize()
    {
        ArgumentNullException.ThrowIfNull(JSRuntime, nameof(JSRuntime));
        var rect = await JSRuntime.InvokeAsync<BoundingClientRect>("eval",
    new object[] { "document.querySelector('.tetris-board')?.getBoundingClientRect()" });

        double viewportWidth = (double)(await JSRuntime.InvokeAsync<double>("eval", "window.innerWidth"));
        double viewportHeight = (double)(await JSRuntime.InvokeAsync<double>("eval", "window.innerHeight"));
        //viewportHeight =(double)(viewportHeight - rect.Top);
        Console.WriteLine("top:" + rect.Top);
        Console.WriteLine("vh" + viewportHeight);
        Console.WriteLine("vw" + viewportWidth);
        double maxBlockWidth, maxBlockHeight;
        if (viewportWidth > 1218)
        {
            var possibleWidth = (viewportWidth - 300) / BoardWidth;
            var possibleHeight = viewportHeight / BoardHeight;
            maxBlockWidth = Math.Min(possibleWidth, 50);
            maxBlockHeight = (double)Math.Min(possibleHeight, 50);
            Console.WriteLine($"naast elkaar? w{possibleWidth}/{maxBlockWidth} h{possibleHeight}/{maxBlockHeight}");
        }
        else
        {
            var possibleWidth = viewportWidth / BoardWidth;
            var possibleHeight = (viewportHeight - rect.Top) / BoardHeight;
            maxBlockWidth = (double)Math.Min(viewportWidth / BoardWidth, 50);
            maxBlockHeight = (double)Math.Min(possibleHeight, 50);
            Console.WriteLine($"onder elkaar? w{possibleWidth}/{maxBlockWidth} h{possibleHeight}/{maxBlockHeight} t:{rect.Top};");
        }
        var blockSize = Math.Min(Math.Min(maxBlockWidth, maxBlockHeight), 50);
        BlockSize = blockSize;
        SvgWidth = BoardWidth * BlockSize;
        SvgHeight = BoardHeight * BlockSize;
        PreviewSvgWidth = PreviewBoardWidth * BlockSize;
        PreviewSvgHeight = PreviewBoardHeight * BlockSize;
        GameInstance.BoardWidth = BoardWidth;
        GameInstance.BoardHeight = BoardHeight;
        GameInstance.BlockSize = BlockSize;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ArgumentNullException.ThrowIfNull(JSRuntime, nameof(JSRuntime));
            await JSRuntime.InvokeVoidAsync("resizeHandler.addEventListener", DotNetObjectReference.Create(this));
            await CalculateBlockSize();
            GameInstance = new TetrisGame(BlockSize, BoardWidth, BoardHeight, Cts);
            StateHasChanged();
            await TetrisContainer.FocusAsync(); // Ensure focus on the container for key input
        }
        else
        {
            await CalculateBlockSize();
            ArgumentNullException.ThrowIfNull(JSRuntime, nameof(JSRuntime));
            var rect = await JSRuntime.InvokeAsync<BoundingClientRect>("eval",
                new object[] { "document.querySelector('.tetris-board')?.getBoundingClientRect()" });
            OffsetFieldLeft = (double)rect.Left;
            OffsetFieldBottom = (double)(rect.Top + rect.Height);
            OffsetFieldTop = (double)rect.Top;
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

        ArgumentNullException.ThrowIfNull(JSRuntime, nameof(JSRuntime));
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
        if (GameInstance.IsGameOver || !GameInstance.IsRunning)
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
        var offSetFieldTop = OffsetFieldBottom - SvgHeight;
        if (GameInstance.IsGameOver || !GameInstance.IsRunning)
            return;
        if (e.ClientX < OffsetFieldLeft)
        {
            GameInstance.MoveTetrominoLeft();
        }
        if (e.ClientX > OffsetFieldLeft + SvgWidth)
        {
            GameInstance.MoveTetrominoRight();
        }
        if (e.ClientY > OffsetFieldBottom && (e.ClientX > OffsetFieldLeft || e.ClientX < OffsetFieldLeft + SvgWidth))
        {
            GameInstance.MoveTetrominoDown();
        }
        if (e.ClientY < OffsetFieldBottom - SvgHeight && (e.ClientX > OffsetFieldLeft || e.ClientX < OffsetFieldLeft + SvgWidth))
        {
            GameInstance.RotateTetromino();
        }
        await TetrisContainer.FocusAsync(); // Refocus on the container for key input
        StateHasChanged(); // Update the UI
    }
    [JSInvokable]
    public async Task OnWindowResize()
    {
        await CalculateBlockSize();
        StateHasChanged(); // Trigger a UI update after recalculating sizes
    }
    public void Dispose()
    {
        // Cancel game loop and dispose resources
        Cts?.Cancel();
        Cts?.Dispose();
        GameTimer?.Dispose();

        // Remove the resize event listener
        if (JSRuntime is not null)
        {
            JSRuntime.InvokeVoidAsync("resizeHandler.removeEventListener", DotNetObjectReference.Create(this));
        }
    }

}
