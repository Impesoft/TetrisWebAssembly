using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TetrisWebAssembly.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using System.Timers;
using TetrisWebAssembly.Models;

public partial class Breakout : ComponentBase
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;
    private const int FieldWidth = 600;
    private const int FieldHeight = 900;
    private ElementReference BreakoutContainer;
    public bool IsDemo { get; private set; }

    private double PaddleX = 250;
    private double BallX = 300, BallY = 760;
    private double BallSpeedX = 3, BallSpeedY = -3;
    //private double MouseLastX = 0, MouseSpeed = 0;
    private int Score = 0;
    private int Lives = 5;
    private int PaddleWidth = 100;
    private bool IsPaused = true;
    private double relativeX = 100;
    private Timer GameLoopTimer;
    private const int BallRadius = 10;
    private double PlayfieldOffsetLeft = 0;
    private const double MaxBallSpeed = 10; // Slightly higher top speed
    private const double MinHorizontalSpeed = 1; // Prevent ball from getting stuck
    private const int PaddleYPosition = 800; // Lowered paddle position
    public List<BreakoutBlock> Blocks { get; set; } = new();

    protected override void OnInitialized()
    {
        InitializeBlocks();
        GameLoopTimer = new Timer(16); // ~60 FPS
        GameLoopTimer.Elapsed += (sender, args) =>
        {
            if (!IsPaused)
            {
                InvokeAsync(() =>
                {
                    UpdateBall();
                    StateHasChanged();
                });
            }
        };
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Call JavaScript to get bounding rectangle
            var rect = await JSRuntime.InvokeAsync<BoundingClientRect>(
                "eval",
                new object[] { "document.querySelector('.game-field')?.getBoundingClientRect()" }
            );

            if (rect != null)
            {
                PlayfieldOffsetLeft = rect.Left;
            }
        }
    }
    private void InitializeBlocks()
    {
        string[] colors = { "purple", "red", "orange", "yellow", "blue", "green", "white", "black" };
        int[] hits = { 7, 6, 5, 4, 3, 2, 1, int.MaxValue };

        Blocks.Clear();

        // Main grid of blocks
        for (int row = 0; row < 7; row++) // 7 rows
        {
            for (int col = 0; col < 10; col++) // 10 columns
            {
                Blocks.Add(new BreakoutBlock
                {
                    X = col * 60 + 5,
                    Y = row * 30,
                    Color = colors[row % colors.Length],
                    HitsRemaining = hits[row % hits.Length]
                });
            }
        }
    }
    private void MovePaddle(MouseEventArgs e)
    {
        if (IsPaused) return;

        // Calculate the relative mouse position within the playfield
        relativeX = e.ClientX - PlayfieldOffsetLeft;

        // Check if the pointer is outside the playfield
        if (relativeX < PaddleWidth / 2 || relativeX > FieldWidth)
        {
            IsDemo = true;
            return;
        }

        IsDemo = false;

        // Center paddle around the mouse position and constrain it within bounds
        PaddleX = relativeX - PaddleWidth / 2 - BallRadius / 2; // Center the paddle 
        PaddleX = Math.Clamp(PaddleX, 0, FieldWidth - PaddleWidth); // Prevent paddle from leaving the playfield
    }
    private void UpdateBall()
    {
        BallX += BallSpeedX;
        BallY += BallSpeedY;
        if (IsDemo)
        {
            PaddleX = Math.Clamp(BallX - PaddleWidth / 2, 0, FieldWidth - PaddleWidth);
        }

        // Bounce off walls
        if (BallX - BallRadius <= 0)
        {
            BallSpeedX = Math.Abs(BallSpeedX);
            Score += 10;
            if (Math.Abs(BallSpeedY) > 1)
            {
                BallSpeedY *= 0.99; // Slow down ball
            }
        }
        if (BallX + BallRadius >= FieldWidth)
        {
            BallSpeedX = Math.Abs(BallSpeedX) * -1;
            Score += 10;
            if (Math.Abs(BallSpeedY) > 1)
            {
                BallSpeedY *= 0.99; // Slow down ball
            }
        }
        if (BallY - BallRadius <= 0)
        {
            BallSpeedY = Math.Abs(BallSpeedY);
            Score += 10;
            if (Math.Abs(BallSpeedY) > 1)
            {
                BallSpeedY *= 0.99; // Slow down ball
            }
        }
        // Ensure minimum horizontal speed
        if (Math.Abs(BallSpeedX) < MinHorizontalSpeed)
        {
            BallSpeedX = BallSpeedX < 0 ? -MinHorizontalSpeed : MinHorizontalSpeed;
            if (Math.Abs(BallSpeedY) < MaxBallSpeed)
            {
                BallSpeedY *= 1.2; // Slow down ball
            }

        }
        // Bounce off paddle
        if (BallY + BallRadius >= PaddleYPosition && BallX >= PaddleX && BallX <= PaddleX + 100)
        {
            BallSpeedY = -Math.Abs(BallSpeedY); // Always bounce up
            BallSpeedX += (BallX - (PaddleX + 50)) / 25; // Adjust based on hit location
            BallSpeedX = Math.Clamp(BallSpeedX, -MaxBallSpeed, MaxBallSpeed); // Limit speed
            Score += 10;
        }

        if (Math.Abs(BallSpeedX) > 1)
        {
            BallSpeedX *= 0.99; // Slow down ball
        }
        // Check collision with blocks
        CheckBlockCollision();

        // Reset if ball falls below the paddle
        if (BallY > FieldHeight)
        {
            Lives--;
            if (Lives <= 0)
            {
                ResetGame();
            }
            else
            {
                ResetBall();
            }
        }

        // Check if level is cleared
        if (Blocks.All(b => b.HitsRemaining == int.MaxValue))
        {
            LevelComplete();
        }
    }
    private void LevelComplete()
    {
        IsPaused = true;
        // Show a "Level Cleared" message or transition to the next level
        // For now, just reset the game
        ResetGame();
    }

    private void CheckBlockCollision()
    {
        for (int i = 0; i < Blocks.Count; i++)
        {
            var block = Blocks[i];
            if (IsCollidingWithBlock(block))
            {
                // Determine collision side
                bool isAbove = BallY + BallRadius <= block.Y + 10;
                bool isBelow = BallY - BallRadius >= block.Y + 10;
                bool isLeft = BallX + BallRadius <= block.X + 10;
                bool isRight = BallX - BallRadius >= block.X + 40;

                if (isAbove)
                {
                    BallSpeedY = Math.Abs(BallSpeedY) * -1; // Bounce vertically
                    if (Math.Abs(BallSpeedY) < MaxBallSpeed)
                    {
                        BallSpeedY *= 1.2; // Slow down ball
                    }

                }
                else
                {
                    BallSpeedY = Math.Abs(BallSpeedY); // Bounce vertically
                    if (Math.Abs(BallSpeedY) < MaxBallSpeed)
                    {
                        BallSpeedY *= 1.2; // Slow down ball
                    }

                }
                if (isLeft)
                {
                    BallSpeedX = Math.Abs(BallSpeedX) * -1; // Bounce horizontally
                }
                if (isRight)
                {
                    BallSpeedX = Math.Abs(BallSpeedX); // Bounce horizontally
                }

                // Update block state if not black
                if (block.HitsRemaining != int.MaxValue)
                {
                    block.HitsRemaining--;
                    block.Color = GetBlockColor(block.HitsRemaining);

                    // Increment score
                    Score += 10;

                    // Remove block if destroyed
                    if (block.HitsRemaining <= 0)
                    {
                        Blocks.RemoveAt(i);
                        i--; // Adjust index after removal
                    }
                }

                break; // Exit loop after collision
            }
        }
    }
    private void ResetBall()
    {
        BallX = 300;
        BallY = 560;
        BallSpeedX = 3;
        BallSpeedY = -3;
    }
    private bool IsCollidingWithBlock(BreakoutBlock block)
    {
        return BallX + BallRadius >= block.X && BallX - BallRadius <= block.X + 50 &&
               BallY + BallRadius >= block.Y && BallY - BallRadius <= block.Y + 20;
    }
    private string GetBlockColor(int hitsRemaining)
    {
        return hitsRemaining switch
        {
            7 => "purple",
            6 => "red",
            5 => "orange",
            4 => "yellow",
            3 => "blue",
            2 => "green",
            1 => "white",
            _ => "black"
        };
    }

    private async Task StartGame()
    {
        await BreakoutContainer.FocusAsync();
        IsPaused = false;
        GameLoopTimer.Start();
    }

    private void PauseGame()
    {
        IsPaused = true;
    }

    private void ResetGame()
    {
        BallX = 300;
        BallY = 560;
        BallSpeedX = 3;
        BallSpeedY = -3;
        IsPaused = true;
        InitializeBlocks();
    }
}

public class BoundingClientRect
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}