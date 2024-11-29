namespace TetrisWebAssembly.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading;
using System.Threading.Tasks;

public partial class Home : ComponentBase
{
    private int Score { get; set; }
    private int CurrentSpeed => dropInterval;

    private const int BoardWidth = 10; // Width of the playfield in blocks
    private const int BoardHeight = 20; // Height of the playfield in blocks
    private int BlockSize { get; set; } = 30; // Size of each block in pixels
    private int SvgWidth => BoardWidth * BlockSize; // Total width of the playfield in pixels
    private int SvgHeight => BoardHeight * BlockSize; // Total height of the playfield in pixels

    private List<Block> Blocks = new(); // List of locked blocks
    private Tetromino? CurrentTetromino; // Currently falling tetromino
    private ElementReference TetrisContainer;
    private PeriodicTimer? gameTimer; // Timer for the game loop
    private CancellationTokenSource? cts; // Cancellation token for the game loop
    private int dropInterval = 1000; // Initial interval for block drop (in ms)
    private const int MinInterval = 200; // Minimum interval for faster drops
    private Tetromino? NextTetromino { get; set; }
    private bool IsGameOver { get; set; } = false;

    protected override void OnInitialized()
    {
        CalculateBlockSize();
        CurrentTetromino = Tetromino.GenerateRandom(BlockSize);
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await TetrisContainer.FocusAsync();
        }
    }
    private void CalculateBlockSize()
    {
        // Adjust block size to fit the viewport
        var maxWidth = 300; // Maximum width in pixels
        var maxHeight = 600; // Maximum height in pixels
        BlockSize = Math.Min(maxWidth / BoardWidth, maxHeight / BoardHeight);
    }

    private async Task StartGame()
    {
        ResetGame();
        cts = new CancellationTokenSource();
        gameTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(dropInterval));

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                // Check and wait for the timer tick
                if (await gameTimer.WaitForNextTickAsync(cts.Token))
                {
                    MoveTetrominoDown();
                    StateHasChanged();
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
        cts?.Cancel();
    }

    private void ResetGame()
    {
        IsGameOver = false; // Reset the game over state
        Blocks.Clear();
        CurrentTetromino = Tetromino.GenerateRandom(BlockSize);
        NextTetromino = Tetromino.GenerateRandom(BlockSize); // Reset preview
        cts?.Cancel();
        cts = null;
    }

    private void MoveTetrominoDown()
    {
        if (CurrentTetromino != null && CurrentTetromino.CanMove(Blocks, BoardWidth, BoardHeight, 0, 1, BlockSize))
        {
            CurrentTetromino.Move(0, 1, BlockSize);
        }
        else
        {
            LockTetromino();
            CheckForCompletedLines();
            CurrentTetromino = NextTetromino; // Use the preview
            NextTetromino = Tetromino.GenerateRandom(BlockSize); // Generate a new preview
            if (!CurrentTetromino.CanMove(Blocks, BoardWidth, BoardHeight, 0, 0, BlockSize))
            {
                GameOver();
            }
        }
        Score +=5;
    }
    private void GameOver()
    {
        cts?.Cancel(); // Stop the game timer
        Console.WriteLine("Game Over!"); // Debug log (replace with UI feedback if needed)
        CurrentTetromino = null; // Stop rendering the current tetromino
        IsGameOver = true; // Flag to indicate game is over
    }

    private void LockTetromino()
    {
        if (CurrentTetromino != null)
        {
            // Add the current tetromino's blocks to the settled blocks
            Blocks.AddRange(CurrentTetromino.Blocks.Select(block =>
                new Block(block.X, block.Y, DarkenColor(CurrentTetromino.Color)))); // Slightly darken the color
        }
    }
    private string DarkenColor(string hexColor, double factor = 0.8)
    {
        // Ensure the hex color starts with '#'
        if (!hexColor.StartsWith("#"))
            return hexColor;

        // Parse the RGB components
        int r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
        int g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
        int b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

        // Darken the color by the given factor
        r = (int)(r * factor);
        g = (int)(g * factor);
        b = (int)(b * factor);

        // Return the new hex color
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private void CheckForCompletedLines()
    {
        // Get all completed rows (sorted from bottom to top)
        var completedRows = Blocks.GroupBy(b => b.Y)
            .Where(g => g.Count() == BoardWidth)
            .Select(g => g.Key)
            .OrderByDescending(y => y) // Start from the bottom row
            .ToList();

        if (!completedRows.Any())
            return;

        // Debugging: Log cleared rows
        Console.WriteLine($"Cleared rows: {string.Join(", ", completedRows)}");

        // Remove blocks in completed rows
        Blocks = Blocks.Where(b => !completedRows.Contains(b.Y)).ToList();

        // Create a map of how many rows have been cleared below each row
        var rowDrops = new Dictionary<int, int>();
        int clearedCount = 0;
        for (int y = BoardHeight * BlockSize; y >= 0; y -= BlockSize)
        {
            if (completedRows.Contains(y))
            {
                clearedCount++;
            }
            else
            {
                rowDrops[y] = clearedCount; // Rows below this row have been cleared
            }
        }

        // Move blocks above cleared rows down
        foreach (var block in Blocks)
        {
            if (rowDrops.TryGetValue(block.Y, out int dropAmount))
            {
                block.Y += dropAmount * BlockSize; // Drop block by the calculated amount
            }
        }

        // Debugging: Log block positions after processing
        foreach (var block in Blocks)
        {
            Console.WriteLine($"Block at X: {block.X}, Y: {block.Y}");
        }

        // Adjust score and speed
        Score += completedRows.Count * 100; // Increment score for each cleared row
        AdjustSpeed(completedRows.Count); // Adjust game speed
    }

    private void AdjustSpeed(int linesCleared)
    {
        if (linesCleared > 0 && dropInterval > MinInterval)
        {
            dropInterval = Math.Max(dropInterval - 50, MinInterval);

            // Safely restart the timer with the new interval
            var newTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(dropInterval));
            var oldTimer = gameTimer;
            gameTimer = newTimer;
            oldTimer?.Dispose();
        }
    }

    private void HandleKeyPress(KeyboardEventArgs e)
    {
        if (CurrentTetromino == null) return;

        switch (e.Key)
        {
            case "ArrowLeft":
                if (CurrentTetromino.CanMove(Blocks, BoardWidth, BoardHeight, -1, 0, BlockSize))
                    CurrentTetromino.Move(-1, 0, BlockSize);
                break;

            case "ArrowRight":
                if (CurrentTetromino.CanMove(Blocks, BoardWidth, BoardHeight, 1, 0, BlockSize))
                    CurrentTetromino.Move(1, 0, BlockSize);
                break;

            case "ArrowDown":
                MoveTetrominoDown(); // Accelerate block drop
                break;

            case "ArrowUp":
                CurrentTetromino.RotateClockwise(Blocks, BoardWidth, BoardHeight, BlockSize);
                break;
        }

        StateHasChanged();
    }

    private class Block
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Color { get; set; } // Store the color of the block

        public Block(int x, int y, string color)
        {
            X = x;
            Y = y;
            Color = color;
        }
    }

    private class Tetromino
    {
        public List<Block> Blocks { get; private set; } = new();
        public string Color { get; private set; } = "#FFFFFF"; // Default color
        private static readonly Dictionary<string, (List<(int x, int y)> shape, string color)> TetrominoDefinitions = new()
    {
        { "I", (new List<(int x, int y)> { (0, 0), (1, 0), (2, 0), (3, 0) }, "#00FFFF") }, // Cyan
        { "O", (new List<(int x, int y)> { (0, 0), (1, 0), (0, 1), (1, 1) }, "#FFFF00") }, // Yellow
        { "T", (new List<(int x, int y)> { (0, 0), (1, 0), (2, 0), (1, 1) }, "#800080") }, // Purple
        { "S", (new List<(int x, int y)> { (1, 0), (2, 0), (0, 1), (1, 1) }, "#00FF00") }, // Green
        { "Z", (new List<(int x, int y)> { (0, 0), (1, 0), (1, 1), (2, 1) }, "#FF0000") }, // Red
        { "J", (new List<(int x, int y)> { (0, 0), (0, 1), (1, 1), (2, 1) }, "#0000FF") }, // Blue
        { "L", (new List<(int x, int y)> { (2, 0), (0, 1), (1, 1), (2, 1) }, "#FFA500") }  // Orange
    };
        public static Tetromino GenerateRandom(int blockSize)
        {
            var random = new Random();
            var selected = TetrominoDefinitions.ElementAt(random.Next(TetrominoDefinitions.Count));
            var shapeOffsets = selected.Value.shape;
            var color = selected.Value.color;

            var blocks = shapeOffsets.Select(offset =>
                new Block(offset.x * blockSize, offset.y * blockSize, color)
            ).ToList();

            return new Tetromino { Blocks = blocks, Color = color };
        }
        public bool CanMove(List<Block> existingBlocks, int width, int height, int dx, int dy, int blockSize)
        {
            return Blocks.All(block =>
                block.X + dx * blockSize >= 0 &&
                block.X + dx * blockSize < width * blockSize &&
                block.Y + dy * blockSize < height * blockSize &&
                !existingBlocks.Any(b => b.X == block.X + dx * blockSize && b.Y == block.Y + dy * blockSize));
        }

        public void Move(int dx, int dy, int blockSize)
        {
            Blocks.ForEach(b =>
            {
                b.X += dx * blockSize;
                b.Y += dy * blockSize;
            });
        }
        public void RotateClockwise(List<Block> existingBlocks, int width, int height, int blockSize)
        {
            var pivot = Blocks[0];

            var rotatedBlocks = Blocks.Select(block =>
            {
                int relativeX = block.X - pivot.X;
                int relativeY = block.Y - pivot.Y;

                // Apply rotation formula
                int rotatedX = -relativeY + pivot.X;
                int rotatedY = relativeX + pivot.Y;

                return new Block(rotatedX, rotatedY, block.Color);
            }).ToList();

            if (rotatedBlocks.All(b =>
                b.X >= 0 && b.X < width * blockSize &&
                b.Y >= 0 && b.Y < height * blockSize &&
                !existingBlocks.Any(existing => existing.X == b.X && existing.Y == b.Y)))
            {
                Blocks = rotatedBlocks;
            }
        }
    }
}