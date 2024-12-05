using System.Threading;
using TetrisWebAssembly.Helpers;
using TetrisWebAssembly.Models;

namespace TetrisWebAssembly.GameLogic;

public class TetrisGame
{
    public int BoardWidth; // Width of the playfield (in blocks)
    public int BoardHeight; // Height of the playfield (in blocks)
    private int BlockSize;

    public List<TetrisBlock> Blocks { get; private set; } = new(); // Locked blocks on the playfield
    public Tetromino CurrentTetromino { get; private set; } // Currently falling tetromino
    public Tetromino NextTetromino { get; private set; } // Preview of the next tetromino
    public bool IsGameOver { get; private set; } = false; // Flag to track if the game is over
    public int Score { get; private set; } = 0; // Current game score
    public bool IsRunning { get; private set; } = false; // Flag to track if the game is running
    private static readonly int MinDropInterval = 200; // Minimum drop interval (faster speed)
    private int dropInterval = 1000; // Current drop interval (in milliseconds)
    private CancellationTokenSource? Cts = new CancellationTokenSource();
    public TetrisGame(int blockSize, int boardWidth, int boardHeight, CancellationTokenSource? cts)
    {
        BlockSize = blockSize;
        BoardWidth = boardWidth;
        BoardHeight = boardHeight;
        CurrentTetromino = Tetromino.GenerateRandom(BlockSize);
        NextTetromino = Tetromino.GenerateRandom(BlockSize);
        Cts = cts;
        StartNewGame();
    }

    /// <summary>
    /// Starts a new game by resetting the state.
    /// </summary>
    public void StartNewGame()
    {
        Blocks.Clear();
        Score = 0;
        IsGameOver = false;
        dropInterval = 1000;
        IsRunning = true;
        Cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Moves the current tetromino down. If it cannot move, locks it in place and spawns a new one.
    /// </summary>
    public void MoveTetrominoDown()
    {
        if (CurrentTetromino == null || IsGameOver)
            return;

        if (CurrentTetromino.CanMove(Blocks, BoardWidth, BoardHeight, 0, 1, BlockSize))
        {
            CurrentTetromino.Move(0, 1, BlockSize);
        }
        else
        {
            LockTetromino();
            CheckForCompletedLines();

            // Spawn the next tetromino
            CurrentTetromino = NextTetromino;
            NextTetromino = Tetromino.GenerateRandom(BlockSize);

            // Check for Game Over
            if (!CurrentTetromino.CanMove(Blocks, BoardWidth, BoardHeight, 0, 0, BlockSize))
            {
                IsGameOver = true;
                IsRunning = false;
                Cts?.Cancel();
            }
        }
    }

    /// <summary>
    /// Moves the current tetromino left.
    /// </summary>
    public void MoveTetrominoLeft()
    {
        if (CurrentTetromino?.CanMove(Blocks, BoardWidth, BoardHeight, -1, 0, BlockSize) == true)
        {
            CurrentTetromino.Move(-1, 0, BlockSize);
        }
    }

    /// <summary>
    /// Moves the current tetromino right.
    /// </summary>
    public void MoveTetrominoRight()
    {
        if (CurrentTetromino?.CanMove(Blocks, BoardWidth, BoardHeight, 1, 0, BlockSize) == true)
        {
            CurrentTetromino.Move(1, 0, BlockSize);
        }
    }

    /// <summary>
    /// Rotates the current tetromino clockwise.
    /// </summary>
    public void RotateTetromino()
    {
        CurrentTetromino?.RotateClockwise(Blocks, BoardWidth, BoardHeight, BlockSize);
    }

    /// <summary>
    /// Locks the current tetromino into place.
    /// </summary>
    private void LockTetromino()
    {
        if (CurrentTetromino != null)
        {
            Blocks.AddRange(CurrentTetromino.Blocks);
        }
    }

    /// <summary>
    /// Checks for completed lines, clears them, and adjusts the game state.
    /// </summary>
    private void CheckForCompletedLines()
    {
        var completedRows = Blocks.GroupBy(b => b.Y)
            .Where(g => g.Count() == BoardWidth)
            .Select(g => g.Key)
            .OrderByDescending(y => y) // Process rows from bottom to top
            .ToList();

        // Remove all blocks in the completed rows
        Blocks.RemoveAll(b => completedRows.Contains(b.Y));

        // Drop blocks above cleared rows
        foreach (var block in Blocks)
        {
            // Count how many completed rows are **below** the current block
            int rowsClearedBelow = completedRows.Count(row => row > block.Y);

            // Move the block down by the number of cleared rows below it
            block.Y += rowsClearedBelow * BlockSize;
        }

        // Update score based on the number of cleared rows
        Score += completedRows.Count * 100;

        // Adjust drop speed
        AdjustSpeed(completedRows.Count);
    }

    /// <summary>
    /// Adjusts the drop speed based on the number of cleared lines.
    /// </summary>
    private void AdjustSpeed(int linesCleared)
    {
        if (linesCleared > 0 && dropInterval > MinDropInterval)
        {
            dropInterval = Math.Max(dropInterval - 50, MinDropInterval);
        }
    }

    /// <summary>
    /// Returns the full playfield representation, including locked blocks and the current tetromino.
    /// </summary>
    public IEnumerable<TetrisBlock> GetPlayfield()
    {
        // Combine locked blocks and current tetromino blocks for rendering
        if (CurrentTetromino != null)
        {
            return Blocks.Concat(CurrentTetromino.Blocks);
        }

        return Blocks;
    }

    /// <summary>
    /// Returns the current drop interval (for debugging or UI purposes).
    /// </summary>
    public int GetDropInterval()
    {
        return dropInterval;
    }
}
