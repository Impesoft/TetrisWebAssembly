using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorArcade.Pages
{
    public partial class BubbleShooter : ComponentBase, IDisposable
    {
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private NavigationManager Nav { get; set; } = default!;

        // Board Constants
        public const int FieldWidth = 600;
        public const int FieldHeight = 800;
        public const double BubbleRadius = 20.0;
        public const double BubbleDiameter = 40.0;
        public const double RowHeight = 34.641; // 20 * sqrt(3)
        public const int MaxRows = 15;
        public const int EvenRowCols = 15; // 15 * 40 = 600
        public const int OddRowCols = 14;  // 14 * 40 = 560 + 40 padding
        public const double DeadlineY = 620.0;
        public const double LauncherX = 300.0;
        public const double LauncherY = 720.0;

        // Visual Colors
        public static readonly string[] BubbleColors = new[]
        {
            "#ff3366", // Neon Red/Pink
            "#33ccff", // Electric Blue
            "#33ff66", // Bright Green
            "#ffff33", // Neon Yellow
            "#cc33ff", // Vivid Purple
            "#ff9933"  // Bright Orange
        };

        // State Properties
        public BubbleCell?[,] Board { get; private set; } = new BubbleCell?[MaxRows, EvenRowCols];
        public int TopRowParity { get; private set; } = 0; // 0 = row 0 is even (15 cols), 1 = row 0 is odd (14 cols)
        public bool IsPaused { get; private set; } = true;
        public bool IsGameOver { get; private set; } = false;
        public bool IsGameWon { get; private set; } = false;
        public int Score { get; private set; } = 0;
        public int HighScore { get; private set; } = 0;
        public int Level { get; private set; } = 1;
        public int ShotsFired { get; private set; } = 0;
        public int ShotsUntilCeilingDrop { get; private set; } = 6;

        // Cannon Aiming
        public double AimAngle { get; private set; } = -Math.PI / 2; // -90 deg (straight up)
        public List<PointD> TrajectoryPoints { get; private set; } = new();

        // Active Flying Bubble
        public FlyingBubble? ActiveBubble { get; private set; }
        public string CurrentBubbleColor { get; private set; } = BubbleColors[0];
        public string NextBubbleColor { get; private set; } = BubbleColors[1];

        // Animations / Pop Visual Effects
        public List<PopEffect> PopEffects { get; private set; } = new();
        public List<FallingBubble> DroppingBubbles { get; private set; } = new();

        // Container sizing & game timer
        private ElementReference _gameContainer;
        private double _playfieldOffsetLeft = 0;
        private double _playfieldOffsetTop = 0;
        private double _scaleX = 1.0;
        private double _scaleY = 1.0;
        private System.Timers.Timer? _gameLoopTimer;
        private Random _random = new Random();

        public bool IsRowEven(int r) => ((r + TopRowParity) % 2 == 0);
        public int GetMaxCols(int r) => IsRowEven(r) ? EvenRowCols : OddRowCols;

        protected override void OnInitialized()
        {
            ResetGame();

            _gameLoopTimer = new System.Timers.Timer(16); // ~60 FPS
            _gameLoopTimer.Elapsed += OnGameLoopTick;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await FocusGameContainer();
                await UpdatePlayfieldBounds();
            }
        }

        private async Task FocusGameContainer()
        {
            try
            {
                await _gameContainer.FocusAsync();
            }
            catch { }
        }

        private async Task UpdatePlayfieldBounds()
        {
            try
            {
                var rect = await JSRuntime.InvokeAsync<BoundingClientRect>(
                    "eval",
                    new object[] { "document.querySelector('.bubble-svg-field')?.getBoundingClientRect()" }
                );

                if (rect != null && rect.Width > 0 && rect.Height > 0)
                {
                    _playfieldOffsetLeft = rect.Left;
                    _playfieldOffsetTop = rect.Top;
                    _scaleX = rect.Width / FieldWidth;
                    _scaleY = rect.Height / FieldHeight;
                }
            }
            catch { }
        }

        public void StartGame()
        {
            if (IsGameOver || IsGameWon)
            {
                ResetGame();
            }
            IsPaused = false;
            _gameLoopTimer?.Start();
            StateHasChanged();
        }

        public void PauseGame()
        {
            IsPaused = true;
            _gameLoopTimer?.Stop();
            StateHasChanged();
        }

        public void ResetGame()
        {
            IsPaused = true;
            IsGameOver = false;
            IsGameWon = false;
            Score = 0;
            Level = 1;
            ShotsFired = 0;
            ShotsUntilCeilingDrop = 6;
            ActiveBubble = null;
            TopRowParity = 0;
            PopEffects.Clear();
            DroppingBubbles.Clear();

            InitializeLevel(Level);
            PrepareNextBubbles();
            UpdateTrajectory();
        }

        private void InitializeLevel(int level)
        {
            Board = new BubbleCell?[MaxRows, EvenRowCols];
            TopRowParity = 0;
            int colorsCount = Math.Min(3 + level, BubbleColors.Length);
            int startingRows = Math.Min(4 + level, 8);

            for (int r = 0; r < startingRows; r++)
            {
                int colsInRow = GetMaxCols(r);
                for (int c = 0; c < colsInRow; c++)
                {
                    if (_random.NextDouble() > 0.08)
                    {
                        string color = BubbleColors[_random.Next(colorsCount)];
                        Board[r, c] = new BubbleCell { Color = color };
                    }
                }
            }
        }

        private void PrepareNextBubbles()
        {
            var activeColors = GetActiveBoardColors();
            if (activeColors.Count == 0)
            {
                activeColors = BubbleColors.Take(4).ToList();
            }

            CurrentBubbleColor = NextBubbleColor ?? activeColors[_random.Next(activeColors.Count)];
            NextBubbleColor = activeColors[_random.Next(activeColors.Count)];
        }

        private List<string> GetActiveBoardColors()
        {
            var colors = new HashSet<string>();
            for (int r = 0; r < MaxRows; r++)
            {
                int maxCols = GetMaxCols(r);
                for (int c = 0; c < maxCols; c++)
                {
                    if (Board[r, c] != null)
                    {
                        colors.Add(Board[r, c]!.Color);
                    }
                }
            }
            return colors.ToList();
        }

        private void OnGameLoopTick(object? sender, ElapsedEventArgs e)
        {
            if (IsPaused) return;

            InvokeAsync(() =>
            {
                UpdateGameLogic();
                StateHasChanged();
            });
        }

        private void UpdateGameLogic()
        {
            // 1. Update flying bubble physics
            if (ActiveBubble != null)
            {
                double speed = 18.0;
                double nextX = ActiveBubble.X + ActiveBubble.Vx * speed;
                double nextY = ActiveBubble.Y + ActiveBubble.Vy * speed;

                // Wall Bounce Left
                if (nextX - BubbleRadius <= 0)
                {
                    nextX = BubbleRadius;
                    ActiveBubble.Vx = -ActiveBubble.Vx;
                }
                // Wall Bounce Right
                else if (nextX + BubbleRadius >= FieldWidth)
                {
                    nextX = FieldWidth - BubbleRadius;
                    ActiveBubble.Vx = -ActiveBubble.Vx;
                }

                // Check Top Ceiling Collision
                if (nextY - BubbleRadius <= 0)
                {
                    ActiveBubble.Y = BubbleRadius;
                    SnapBubbleToGrid(ActiveBubble.X, ActiveBubble.Y, ActiveBubble.Color);
                    ActiveBubble = null;
                    return;
                }

                // Check Grid Bubble Collision
                bool collided = CheckGridCollision(nextX, nextY);
                if (collided)
                {
                    SnapBubbleToGrid(ActiveBubble.X, ActiveBubble.Y, ActiveBubble.Color);
                    ActiveBubble = null;
                    return;
                }

                ActiveBubble.X = nextX;
                ActiveBubble.Y = nextY;
            }

            // 2. Update Falling Bubbles Animation
            for (int i = DroppingBubbles.Count - 1; i >= 0; i--)
            {
                var drop = DroppingBubbles[i];
                drop.Y += drop.Vy;
                drop.Vy += 0.8; // Gravity
                if (drop.Y > FieldHeight + 50)
                {
                    DroppingBubbles.RemoveAt(i);
                }
            }

            // 3. Update Pop Effects Decay
            for (int i = PopEffects.Count - 1; i >= 0; i--)
            {
                PopEffects[i].Life -= 0.05;
                if (PopEffects[i].Life <= 0)
                {
                    PopEffects.RemoveAt(i);
                }
            }
        }

        private bool CheckGridCollision(double x, double y)
        {
            double collisionThresholdSq = Math.Pow(BubbleDiameter * 0.92, 2);

            for (int r = 0; r < MaxRows; r++)
            {
                int maxCols = GetMaxCols(r);
                for (int c = 0; c < maxCols; c++)
                {
                    if (Board[r, c] != null)
                    {
                        var pos = GetCellCenter(r, c);
                        double distSq = Math.Pow(x - pos.X, 2) + Math.Pow(y - pos.Y, 2);
                        if (distSq <= collisionThresholdSq)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void SnapBubbleToGrid(double x, double y, string color)
        {
            (int bestR, int bestC) = FindClosestEmptyCell(x, y);

            if (bestR < 0 || bestR >= MaxRows)
            {
                TriggerGameOver();
                return;
            }

            Board[bestR, bestC] = new BubbleCell { Color = color };

            // Check match-3
            var matches = FindSameColorMatches(bestR, bestC, color);
            if (matches.Count >= 3)
            {
                PopBubbles(matches);
                DropFloatingBubbles();
            }
            else
            {
                ShotsUntilCeilingDrop--;
                if (ShotsUntilCeilingDrop <= 0)
                {
                    DropCeiling();
                    ShotsUntilCeilingDrop = 6;
                }
            }

            // Check game state conditions
            CheckGameConditions();

            // Prepare next shot
            PrepareNextBubbles();
            UpdateTrajectory();
        }

        private (int r, int c) FindClosestEmptyCell(double x, double y)
        {
            double minDistSq = double.MaxValue;
            int bestR = 0;
            int bestC = 0;

            for (int r = 0; r < MaxRows; r++)
            {
                int maxCols = GetMaxCols(r);
                for (int c = 0; c < maxCols; c++)
                {
                    if (Board[r, c] == null)
                    {
                        var pos = GetCellCenter(r, c);
                        double distSq = Math.Pow(x - pos.X, 2) + Math.Pow(y - pos.Y, 2);
                        if (distSq < minDistSq)
                        {
                            minDistSq = distSq;
                            bestR = r;
                            bestC = c;
                        }
                    }
                }
            }
            return (bestR, bestC);
        }

        public PointD GetCellCenter(int r, int c)
        {
            bool isEven = IsRowEven(r);
            double xOffset = isEven ? BubbleRadius : BubbleRadius * 2.0;
            double x = xOffset + (c * BubbleDiameter);
            double y = BubbleRadius + (r * RowHeight);
            return new PointD(x, y);
        }

        private List<(int r, int c)> FindSameColorMatches(int startR, int startC, string color)
        {
            var visited = new HashSet<(int, int)>();
            var matches = new List<(int, int)>();
            var queue = new Queue<(int, int)>();

            queue.Enqueue((startR, startC));
            visited.Add((startR, startC));

            while (queue.Count > 0)
            {
                var (r, c) = queue.Dequeue();
                matches.Add((r, c));

                foreach (var neighbor in GetNeighbors(r, c))
                {
                    if (!visited.Contains(neighbor) && Board[neighbor.r, neighbor.c] != null && Board[neighbor.r, neighbor.c]!.Color == color)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return matches;
        }

        private IEnumerable<(int r, int c)> GetNeighbors(int r, int c)
        {
            bool isEven = IsRowEven(r);

            var offsets = isEven
                ? new (int dr, int dc)[] { (0, -1), (0, 1), (-1, -1), (-1, 0), (1, -1), (1, 0) }
                : new (int dr, int dc)[] { (0, -1), (0, 1), (-1, 0), (-1, 1), (1, 0), (1, 1) };

            foreach (var (dr, dc) in offsets)
            {
                int nr = r + dr;
                int nc = c + dc;
                int maxCols = GetMaxCols(nr);

                if (nr >= 0 && nr < MaxRows && nc >= 0 && nc < maxCols)
                {
                    yield return (nr, nc);
                }
            }
        }

        private void PopBubbles(List<(int r, int c)> matches)
        {
            int pointsEarned = matches.Count * 15;
            Score += pointsEarned;
            if (Score > HighScore) HighScore = Score;

            foreach (var (r, c) in matches)
            {
                if (Board[r, c] != null)
                {
                    var pos = GetCellCenter(r, c);
                    PopEffects.Add(new PopEffect
                    {
                        X = pos.X,
                        Y = pos.Y,
                        Color = Board[r, c]!.Color,
                        PointsText = $"+{pointsEarned / matches.Count}",
                        Life = 1.0
                    });
                    Board[r, c] = null;
                }
            }
        }

        private void DropFloatingBubbles()
        {
            var anchored = new HashSet<(int, int)>();
            var queue = new Queue<(int, int)>();

            int topCols = GetMaxCols(0);
            for (int c = 0; c < topCols; c++)
            {
                if (Board[0, c] != null)
                {
                    anchored.Add((0, c));
                    queue.Enqueue((0, c));
                }
            }

            while (queue.Count > 0)
            {
                var (r, c) = queue.Dequeue();

                foreach (var neighbor in GetNeighbors(r, c))
                {
                    if (!anchored.Contains(neighbor) && Board[neighbor.r, neighbor.c] != null)
                    {
                        anchored.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            int droppedCount = 0;
            for (int r = 0; r < MaxRows; r++)
            {
                int maxCols = GetMaxCols(r);
                for (int c = 0; c < maxCols; c++)
                {
                    if (Board[r, c] != null && !anchored.Contains((r, c)))
                    {
                        var pos = GetCellCenter(r, c);
                        DroppingBubbles.Add(new FallingBubble
                        {
                            X = pos.X,
                            Y = pos.Y,
                            Color = Board[r, c]!.Color,
                            Vy = (double)_random.Next(-3, 1)
                        });

                        Board[r, c] = null;
                        droppedCount++;
                    }
                }
            }

            if (droppedCount > 0)
            {
                int bonus = droppedCount * 30;
                Score += bonus;
                if (Score > HighScore) HighScore = Score;
            }
        }

        private void DropCeiling()
        {
            // Toggle top row parity so existing rows retain their exact physical X positions and neighbor alignments!
            TopRowParity = 1 - TopRowParity;

            // Shift rows down by 1
            for (int r = MaxRows - 1; r > 0; r--)
            {
                int cols = GetMaxCols(r);
                for (int c = 0; c < cols; c++)
                {
                    Board[r, c] = Board[r - 1, c];
                }
            }

            // Fill new top row (r = 0) according to new TopRowParity
            int newTopCols = GetMaxCols(0);
            int activeColorCount = Math.Min(3 + Level, BubbleColors.Length);
            for (int c = 0; c < newTopCols; c++)
            {
                Board[0, c] = new BubbleCell { Color = BubbleColors[_random.Next(activeColorCount)] };
            }

            // Clean up any bubbles that might float after dropping
            DropFloatingBubbles();
        }

        private void CheckGameConditions()
        {
            bool hasBubbles = false;
            for (int r = 0; r < MaxRows; r++)
            {
                int maxCols = GetMaxCols(r);
                for (int c = 0; c < maxCols; c++)
                {
                    if (Board[r, c] != null)
                    {
                        hasBubbles = true;
                        var pos = GetCellCenter(r, c);
                        if (pos.Y + BubbleRadius >= DeadlineY)
                        {
                            TriggerGameOver();
                            return;
                        }
                    }
                }
            }

            if (!hasBubbles)
            {
                IsGameWon = true;
                IsPaused = true;
                _gameLoopTimer?.Stop();
                Score += 500 * Level;
            }
        }

        public void NextLevel()
        {
            Level++;
            IsGameWon = false;
            IsPaused = false;
            ShotsUntilCeilingDrop = Math.Max(3, 7 - Level);
            InitializeLevel(Level);
            PrepareNextBubbles();
            UpdateTrajectory();
            _gameLoopTimer?.Start();
        }

        private void TriggerGameOver()
        {
            IsGameOver = true;
            IsPaused = true;
            _gameLoopTimer?.Stop();
        }

        // Aiming & Controls
        public async Task OnMouseMove(MouseEventArgs e)
        {
            if (IsPaused || ActiveBubble != null) return;

            if (_scaleX <= 0.01 || _scaleY <= 0.01)
            {
                await UpdatePlayfieldBounds();
            }

            double mouseX = (e.ClientX - _playfieldOffsetLeft) / _scaleX;
            double mouseY = (e.ClientY - _playfieldOffsetTop) / _scaleY;

            double dx = mouseX - LauncherX;
            double dy = mouseY - LauncherY;

            double angle = Math.Atan2(dy, dx);

            // Clamp angle between -165 deg and -15 deg (pointing upward)
            double minAngle = -Math.PI + (15.0 * Math.PI / 180.0);
            double maxAngle = -(15.0 * Math.PI / 180.0);

            AimAngle = Math.Clamp(angle, minAngle, maxAngle);
            UpdateTrajectory();
        }

        public void OnKeyDown(KeyboardEventArgs e)
        {
            if (IsPaused && e.Key != " ") return;

            double angleStep = 0.08;
            switch (e.Key)
            {
                case "ArrowLeft":
                case "a":
                case "A":
                    AimAngle = Math.Max(-Math.PI + (15.0 * Math.PI / 180.0), AimAngle - angleStep);
                    UpdateTrajectory();
                    break;
                case "ArrowRight":
                case "d":
                case "D":
                    AimAngle = Math.Min(-(15.0 * Math.PI / 180.0), AimAngle + angleStep);
                    UpdateTrajectory();
                    break;
                case " ":
                case "ArrowUp":
                case "w":
                case "W":
                    if (IsPaused)
                    {
                        StartGame();
                    }
                    else
                    {
                        ShootBubble();
                    }
                    break;
            }
        }

        public void ShootBubble()
        {
            if (IsPaused || ActiveBubble != null || IsGameOver || IsGameWon) return;

            ActiveBubble = new FlyingBubble
            {
                X = LauncherX,
                Y = LauncherY,
                Vx = Math.Cos(AimAngle),
                Vy = Math.Sin(AimAngle),
                Color = CurrentBubbleColor
            };

            ShotsFired++;
            TrajectoryPoints.Clear();
        }

        private void UpdateTrajectory()
        {
            TrajectoryPoints.Clear();
            if (ActiveBubble != null) return;

            double curX = LauncherX;
            double curY = LauncherY;
            double vx = Math.Cos(AimAngle);
            double vy = Math.Sin(AimAngle);

            TrajectoryPoints.Add(new PointD(curX, curY));

            double minX = BubbleRadius;
            double maxX = FieldWidth - BubbleRadius;
            double minY = BubbleRadius;

            for (int bounce = 0; bounce < 6; bounce++)
            {
                double step = 4.0;
                bool hitTarget = false;

                while (true)
                {
                    curX += vx * step;
                    curY += vy * step;

                    // Check left wall collision
                    if (curX <= minX && vx < 0)
                    {
                        curX = minX;
                        TrajectoryPoints.Add(new PointD(curX, curY));
                        vx = -vx; // bounce
                        break;
                    }
                    // Check right wall collision
                    else if (curX >= maxX && vx > 0)
                    {
                        curX = maxX;
                        TrajectoryPoints.Add(new PointD(curX, curY));
                        vx = -vx; // bounce
                        break;
                    }

                    // Check ceiling collision
                    if (curY <= minY)
                    {
                        curY = minY;
                        TrajectoryPoints.Add(new PointD(curX, curY));
                        hitTarget = true;
                        break;
                    }

                    // Check grid bubble collision
                    if (CheckGridCollision(curX, curY))
                    {
                        TrajectoryPoints.Add(new PointD(curX, curY));
                        hitTarget = true;
                        break;
                    }
                }

                if (hitTarget) break;
            }
        }

        public string FormatDouble(double val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        public void Dispose()
        {
            _gameLoopTimer?.Stop();
            _gameLoopTimer?.Dispose();
        }
    }

    public class BubbleCell
    {
        public string Color { get; set; } = "#ff3366";
    }

    public class FlyingBubble
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Vx { get; set; }
        public double Vy { get; set; }
        public string Color { get; set; } = "#ff3366";
    }

    public class FallingBubble
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Vy { get; set; }
        public string Color { get; set; } = "#ff3366";
    }

    public class PopEffect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Color { get; set; } = "#ff3366";
        public string PointsText { get; set; } = "+15";
        public double Life { get; set; } = 1.0;
    }

    public record struct PointD(double X, double Y);
}
