using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorArcade.Pages
{
    public partial class BlockBlast : ComponentBase
    {
        [Inject]
        private NavigationManager _nav { get; set; } = default!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        public int GridSize { get; set; } = 8;
        public int[,] Grid { get; set; } = new int[8, 8]; // 0 = empty, >0 = color id
        public int Score { get; set; } = 0;
        public int HighScore { get; set; } = 0;
        public int ComboStreak { get; set; } = 0;

        public bool IsGameOver { get; set; } = false;
        public bool IsGameStarted { get; set; } = false;

        private bool _clearedLineInCurrentTurn = false;

        public List<BlockShape> AvailableShapes { get; set; } = new();
        public BlockShape? SelectedShape { get; set; }
        public int? HoverRow { get; set; }
        public int? HoverCol { get; set; }

        public string FloatingMessage { get; set; } = "";
        public string FloatingSubMessage { get; set; } = "";
        public bool ShowFloatingMessage { get; set; } = false;
        private int _floatingAnimationId = 0;

        private Random _random = new Random();

        protected override async Task OnInitializedAsync()
        {
            await LoadHighScore();
            StartGame();
        }

        private async Task LoadHighScore()
        {
            try
            {
                HighScore = await JSRuntime.InvokeAsync<int>("blockBlastStorage.getHighScore");
            }
            catch
            {
                HighScore = 0;
            }
        }

        private async Task SaveHighScore()
        {
            if (Score > HighScore)
            {
                HighScore = Score;
                try
                {
                    await JSRuntime.InvokeVoidAsync("blockBlastStorage.saveHighScore", HighScore);
                }
                catch { }
            }
        }

        public void StartGame()
        {
            Grid = new int[GridSize, GridSize];
            Score = 0;
            ComboStreak = 0;
            IsGameOver = false;
            IsGameStarted = true;
            SelectedShape = null;
            HoverRow = null;
            HoverCol = null;
            FloatingMessage = "";
            ShowFloatingMessage = false;
            _clearedLineInCurrentTurn = false;

            GenerateStarterBoard();
            GenerateShapes();
        }

        /// <summary>
        /// Authentic Block Blast feature: Starts with an organic-looking board of 8-16 blocks
        /// that looks like it's the result of previous placements.
        /// </summary>
        private void GenerateStarterBoard()
        {
            var allShapes = BlockShape.GetAllShapes();
            bool validBoard = false;
            int maxAttempts = 1000;
            int attempt = 0;

            while (!validBoard && attempt < maxAttempts)
            {
                attempt++;
                int[,] tempGrid = new int[GridSize, GridSize];
                
                // Try to place 3 to 6 shapes to simulate previous moves
                int shapesToPlace = _random.Next(3, 7);
                
                for (int i = 0; i < shapesToPlace; i++)
                {
                    var shape = allShapes[_random.Next(allShapes.Count)];
                    var validPositions = new List<(int r, int c)>();
                    
                    for (int r = 0; r < GridSize; r++)
                    {
                        for (int c = 0; c < GridSize; c++)
                        {
                            if (CanPlaceShapeOnGrid(shape, r, c, tempGrid))
                            {
                                validPositions.Add((r, c));
                            }
                        }
                    }
                    
                    if (validPositions.Count > 0)
                    {
                        // Weight positions by adjacency to create clumps instead of random noise
                        var weightedPositions = new List<(int r, int c, int weight)>();
                        foreach (var pos in validPositions)
                        {
                            int adjacencyScore = 1;
                            adjacencyScore += GetAdjacencyScore(shape, pos.r, pos.c, tempGrid);
                            weightedPositions.Add((pos.r, pos.c, adjacencyScore));
                        }

                        int totalWeight = weightedPositions.Sum(x => x.weight);
                        int randomValue = _random.Next(totalWeight);
                        int currentSum = 0;
                        var selectedPos = weightedPositions.First();

                        foreach (var wPos in weightedPositions)
                        {
                            currentSum += wPos.weight;
                            if (randomValue < currentSum)
                            {
                                selectedPos = wPos;
                                break;
                            }
                        }

                        PlaceShapeOnGrid(shape, selectedPos.r, selectedPos.c, tempGrid);
                    }
                }
                
                ClearCompletedLinesOnGrid(tempGrid);
                
                int placedCells = 0;
                for (int r = 0; r < GridSize; r++)
                    for (int c = 0; c < GridSize; c++)
                        if (tempGrid[r, c] > 0) placedCells++;
                        
                // Check if board meets the good opening criteria
                if (placedCells >= 8 && placedCells <= 16)
                {
                    if (HasEmpty3x3(tempGrid) && !HasIsolatedHoles(tempGrid))
                    {
                        // Prefer boards with almost complete lines, relax after 100 attempts
                        if (HasAlmostCompleteLine(tempGrid) || attempt > 100)
                        {
                            for (int r = 0; r < GridSize; r++)
                                for (int c = 0; c < GridSize; c++)
                                    Grid[r, c] = tempGrid[r, c];
                            validBoard = true;
                        }
                    }
                }
            }

            // Fallback for safety
            if (!validBoard)
            {
                Grid = new int[GridSize, GridSize];
                Grid[GridSize - 1, 0] = _random.Next(1, 11);
                Grid[GridSize - 1, 1] = _random.Next(1, 11);
                Grid[GridSize - 2, 0] = _random.Next(1, 11);
                Grid[GridSize - 2, 1] = _random.Next(1, 11);
                Grid[GridSize - 3, 0] = _random.Next(1, 11);
            }
        }

        private bool CanPlaceShapeOnGrid(BlockShape shape, int r, int c, int[,] grid)
        {
            int rows = shape.Matrix.GetLength(0);
            int cols = shape.Matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (shape.Matrix[i, j] == 1)
                    {
                        int targetR = r + i;
                        int targetC = c + j;
                        if (targetR < 0 || targetR >= GridSize || targetC < 0 || targetC >= GridSize) return false;
                        if (grid[targetR, targetC] > 0) return false;
                    }
                }
            }
            return true;
        }

        private int GetAdjacencyScore(BlockShape shape, int r, int c, int[,] grid)
        {
            int score = 0;
            int rows = shape.Matrix.GetLength(0);
            int cols = shape.Matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (shape.Matrix[i, j] == 1)
                    {
                        int targetR = r + i;
                        int targetC = c + j;
                        
                        if (targetR > 0 && grid[targetR - 1, targetC] > 0) score += 5;
                        if (targetR < GridSize - 1 && grid[targetR + 1, targetC] > 0) score += 5;
                        if (targetC > 0 && grid[targetR, targetC - 1] > 0) score += 5;
                        if (targetC < GridSize - 1 && grid[targetR, targetC + 1] > 0) score += 5;
                        
                        if (targetR == 0 || targetR == GridSize - 1) score += 2;
                        if (targetC == 0 || targetC == GridSize - 1) score += 2;
                    }
                }
            }
            return score;
        }

        private void PlaceShapeOnGrid(BlockShape shape, int r, int c, int[,] grid)
        {
            int rows = shape.Matrix.GetLength(0);
            int cols = shape.Matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (shape.Matrix[i, j] == 1)
                    {
                        grid[r + i, c + j] = shape.ColorId;
                    }
                }
            }
        }

        private void ClearCompletedLinesOnGrid(int[,] grid)
        {
            List<int> rowsToClear = new List<int>();
            List<int> colsToClear = new List<int>();

            for (int r = 0; r < GridSize; r++)
            {
                bool fullRow = true;
                for (int c = 0; c < GridSize; c++)
                {
                    if (grid[r, c] == 0) { fullRow = false; break; }
                }
                if (fullRow) rowsToClear.Add(r);
            }

            for (int c = 0; c < GridSize; c++)
            {
                bool fullCol = true;
                for (int r = 0; r < GridSize; r++)
                {
                    if (grid[r, c] == 0) { fullCol = false; break; }
                }
                if (fullCol) colsToClear.Add(c);
            }

            foreach (int r in rowsToClear)
                for (int c = 0; c < GridSize; c++) grid[r, c] = 0;

            foreach (int c in colsToClear)
                for (int r = 0; r < GridSize; r++) grid[r, c] = 0;
        }

        private bool HasEmpty3x3(int[,] grid)
        {
            for (int r = 0; r <= GridSize - 3; r++)
            {
                for (int c = 0; c <= GridSize - 3; c++)
                {
                    bool empty = true;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (grid[r + i, c + j] > 0)
                            {
                                empty = false;
                                break;
                            }
                        }
                        if (!empty) break;
                    }
                    if (empty) return true;
                }
            }
            return false;
        }

        private bool HasIsolatedHoles(int[,] grid)
        {
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (grid[r, c] == 0)
                    {
                        bool top = r == 0 || grid[r - 1, c] > 0;
                        bool bottom = r == GridSize - 1 || grid[r + 1, c] > 0;
                        bool left = c == 0 || grid[r, c - 1] > 0;
                        bool right = c == GridSize - 1 || grid[r, c + 1] > 0;
                        if (top && bottom && left && right) return true;
                    }
                }
            }
            return false;
        }

        private bool HasAlmostCompleteLine(int[,] grid)
        {
            for (int r = 0; r < GridSize; r++)
            {
                int count = 0;
                for (int c = 0; c < GridSize; c++)
                    if (grid[r, c] > 0) count++;
                if (count == GridSize - 1 || count == GridSize - 2) return true;
            }
            for (int c = 0; c < GridSize; c++)
            {
                int count = 0;
                for (int r = 0; r < GridSize; r++)
                    if (grid[r, c] > 0) count++;
                if (count == GridSize - 1 || count == GridSize - 2) return true;
            }
            return false;
        }

        private int _dfsNodesExplored = 0;
        private const int MaxDfsNodes = 5000;

        private void GenerateShapes()
        {
            AvailableShapes.Clear();
            var allShapes = BlockShape.GetAllShapes();
            
            BlockShape[] chosenShapes = new BlockShape[3];
            bool foundValidSet = false;
            
            // Try up to 50 times to find a set of 3 shapes that can be fully played
            for (int attempt = 0; attempt < 50; attempt++)
            {
                for (int i = 0; i < 3; i++)
                {
                    chosenShapes[i] = allShapes[_random.Next(allShapes.Count)];
                }
                
                if (CanPlayAllShapes(chosenShapes, Grid))
                {
                    foundValidSet = true;
                    break;
                }
            }
            
            if (!foundValidSet)
            {
                // Fallback: provide 1x1 blocks so the game can potentially continue or naturally end
                chosenShapes[0] = allShapes[0];
                chosenShapes[1] = allShapes[0];
                chosenShapes[2] = allShapes[0];
            }
            
            for (int i = 0; i < 3; i++)
            {
                AvailableShapes.Add(new BlockShape
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    Matrix = chosenShapes[i].Matrix,
                    ColorClass = chosenShapes[i].ColorClass,
                    ColorId = chosenShapes[i].ColorId
                });
            }
            CheckGameOver();
        }

        private bool CanPlayAllShapes(BlockShape[] shapes, int[,] initialGrid)
        {
            _dfsNodesExplored = 0;
            int[][] permutations = new int[][]
            {
                new int[] { 0, 1, 2 },
                new int[] { 0, 2, 1 },
                new int[] { 1, 0, 2 },
                new int[] { 1, 2, 0 },
                new int[] { 2, 0, 1 },
                new int[] { 2, 1, 0 }
            };
            
            foreach (var perm in permutations)
            {
                if (CanPlayPermutation(shapes, perm, 0, initialGrid))
                {
                    return true;
                }
            }
            return false;
        }

        private bool CanPlayPermutation(BlockShape[] shapes, int[] perm, int shapeIndex, int[,] currentGrid)
        {
            if (shapeIndex >= shapes.Length) return true;
            if (_dfsNodesExplored > MaxDfsNodes) return false; // Fail safe to avoid lag
            
            BlockShape shape = shapes[perm[shapeIndex]];
            
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (CanPlaceShapeOnGrid(shape, r, c, currentGrid))
                    {
                        _dfsNodesExplored++;
                        
                        // Clone grid to test placement
                        int[,] nextGrid = new int[GridSize, GridSize];
                        for (int i = 0; i < GridSize; i++)
                            for (int j = 0; j < GridSize; j++)
                                nextGrid[i, j] = currentGrid[i, j];
                                
                        PlaceShapeOnGrid(shape, r, c, nextGrid);
                        ClearCompletedLinesOnGrid(nextGrid);
                        
                        if (CanPlayPermutation(shapes, perm, shapeIndex + 1, nextGrid))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void SelectShape(BlockShape shape)
        {
            if (IsGameOver) return;
            if (SelectedShape == shape)
            {
                SelectedShape = null; // deselect
            }
            else
            {
                SelectedShape = shape;
            }
        }

        public void HandleCellHover(int r, int c)
        {
            if (SelectedShape != null && !IsGameOver)
            {
                HoverRow = r;
                HoverCol = c;
            }
        }

        public void ClearHover()
        {
            HoverRow = null;
            HoverCol = null;
        }

        private (int r, int c) GetTopLeftFromCenter(BlockShape shape, int centerR, int centerC)
        {
            int rows = shape.Matrix.GetLength(0);
            int cols = shape.Matrix.GetLength(1);
            int topR = centerR - (rows / 2);
            int topC = centerC - (cols / 2);
            return (topR, topC);
        }

        public async Task HandleCellClick(int r, int c)
        {
            if (IsGameOver || SelectedShape == null) return;

            var (topR, topC) = GetTopLeftFromCenter(SelectedShape, r, c);

            if (CanPlaceShape(SelectedShape, topR, topC))
            {
                int blocksCount = PlaceShape(SelectedShape, topR, topC);
                AvailableShapes.Remove(SelectedShape);
                SelectedShape = null;
                HoverRow = null;
                HoverCol = null;

                bool clearedAnyLine = await ClearCompletedLines(blocksCount);

                if (!clearedAnyLine)
                {
                    try { await JSRuntime.InvokeVoidAsync("blockBlastAudio.playPlace"); } catch { }
                }
                else
                {
                    _clearedLineInCurrentTurn = true;
                }

                await SaveHighScore();

                if (AvailableShapes.Count == 0)
                {
                    if (!_clearedLineInCurrentTurn)
                    {
                        // Reset combo streak when a full turn (3 blocks) finishes with NO lines cleared.
                        ComboStreak = 0;
                    }

                    GenerateShapes();
                    _clearedLineInCurrentTurn = false;
                }
                else
                {
                    CheckGameOver();
                }
            }
        }

        private bool CanPlaceShape(BlockShape shape, int r, int c)
        {
            int rows = shape.Matrix.GetLength(0);
            int cols = shape.Matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (shape.Matrix[i, j] == 1)
                    {
                        int targetR = r + i;
                        int targetC = c + j;

                        if (targetR < 0 || targetR >= GridSize || targetC < 0 || targetC >= GridSize)
                        {
                            return false; // out of bounds
                        }
                        if (Grid[targetR, targetC] > 0)
                        {
                            return false; // overlap
                        }
                    }
                }
            }
            return true;
        }

        private int PlaceShape(BlockShape shape, int r, int c)
        {
            int rows = shape.Matrix.GetLength(0);
            int cols = shape.Matrix.GetLength(1);
            int blocksPlaced = 0;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (shape.Matrix[i, j] == 1)
                    {
                        Grid[r + i, c + j] = shape.ColorId;
                        blocksPlaced++;
                    }
                }
            }
            Score += blocksPlaced * 23;
            return blocksPlaced;
        }

        private async Task<bool> ClearCompletedLines(int blocksPlaced)
        {
            List<int> rowsToClear = new List<int>();
            List<int> colsToClear = new List<int>();

            // Check rows
            for (int r = 0; r < GridSize; r++)
            {
                bool fullRow = true;
                for (int c = 0; c < GridSize; c++)
                {
                    if (Grid[r, c] == 0)
                    {
                        fullRow = false;
                        break;
                    }
                }
                if (fullRow) rowsToClear.Add(r);
            }

            // Check columns
            for (int c = 0; c < GridSize; c++)
            {
                bool fullCol = true;
                for (int r = 0; r < GridSize; r++)
                {
                    if (Grid[r, c] == 0)
                    {
                        fullCol = false;
                        break;
                    }
                }
                if (fullCol) colsToClear.Add(c);
            }

            int linesCleared = rowsToClear.Count + colsToClear.Count;

            // Clear cells
            foreach (int r in rowsToClear)
            {
                for (int c = 0; c < GridSize; c++) Grid[r, c] = 0;
            }
            foreach (int c in colsToClear)
            {
                for (int r = 0; r < GridSize; r++) Grid[r, c] = 0;
            }

            if (linesCleared > 0)
            {
                ComboStreak++;
                int baseScore = linesCleared * 100;
                int streakBonus = (ComboStreak > 1) ? (ComboStreak * 50) : 0;
                int multiLineBonus = (linesCleared > 1) ? (linesCleared * 100) : 0;
                int totalTurnScore = baseScore + streakBonus + multiLineBonus;
                int pointsGained = totalTurnScore * 25;

                Score += pointsGained;

                // Sound & visual feedback
                try
                {
                    if (ComboStreak > 1)
                    {
                        await JSRuntime.InvokeVoidAsync("blockBlastAudio.playCombo", ComboStreak);
                    }
                    else
                    {
                        await JSRuntime.InvokeVoidAsync("blockBlastAudio.playBlast");
                    }
                }
                catch { }

                // Popup message
                if (linesCleared >= 3)
                {
                    FloatingMessage = "TRIPLE BLAST!";
                }
                else if (linesCleared == 2)
                {
                    FloatingMessage = "DOUBLE BLAST!";
                }
                else if (ComboStreak > 1)
                {
                    FloatingMessage = $"STREAK x{ComboStreak}!";
                }
                else
                {
                    FloatingMessage = "LINE CLEAR!";
                }

                ShowFloatingMessage = true;
                _ = AnimateFloatingScore(pointsGained);

                return true;
            }

            return false;
        }

        private async Task AnimateFloatingScore(int totalGained)
        {
            int currentId = ++_floatingAnimationId;
            int steps = 20;
            int delayPerStep = 30; // 600ms total animation time
            
            for (int i = 1; i <= steps; i++)
            {
                if (_floatingAnimationId != currentId) return;
                int currentDisplay = (int)(totalGained * (i / (double)steps));
                FloatingSubMessage = $"+{currentDisplay}";
                await InvokeAsync(StateHasChanged);
                await Task.Delay(delayPerStep);
            }
            
            if (_floatingAnimationId != currentId) return;
            FloatingSubMessage = $"+{totalGained}";
            await InvokeAsync(StateHasChanged);
            
            // Show the result a little longer
            await Task.Delay(2000);
            
            if (_floatingAnimationId == currentId)
            {
                ShowFloatingMessage = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private void CheckGameOver()
        {
            foreach (var shape in AvailableShapes)
            {
                for (int r = 0; r < GridSize; r++)
                {
                    for (int c = 0; c < GridSize; c++)
                    {
                        if (CanPlaceShape(shape, r, c))
                        {
                            return; // still possible to play
                        }
                    }
                }
            }
            IsGameOver = true;
            try { JSRuntime.InvokeVoidAsync("blockBlastAudio.playGameOver"); } catch { }
        }

        public string GetPreviewCellClass(int r, int c)
        {
            if (SelectedShape == null || HoverRow == null || HoverCol == null) return "";

            var (topR, topC) = GetTopLeftFromCenter(SelectedShape, HoverRow.Value, HoverCol.Value);

            int sr = r - topR;
            int sc = c - topC;

            if (sr >= 0 && sr < SelectedShape.Matrix.GetLength(0) &&
                sc >= 0 && sc < SelectedShape.Matrix.GetLength(1))
            {
                if (SelectedShape.Matrix[sr, sc] == 1)
                {
                    bool isValid = CanPlaceShape(SelectedShape, topR, topC);
                    return isValid ? $"preview-valid {SelectedShape.ColorClass}" : "preview-invalid";
                }
            }
            return "";
        }
        
        public string GetCellClass(int colorId)
        {
            return colorId switch
            {
                1 => "shape-blue",
                2 => "shape-red",
                3 => "shape-green",
                4 => "shape-yellow",
                5 => "shape-purple",
                6 => "shape-cyan",
                7 => "shape-orange",
                8 => "shape-pink",
                9 => "shape-teal",
                10 => "shape-lime",
                11 => "shape-blue", // Tetris L
                12 => "shape-orange", // Tetris J
                13 => "shape-green", // Tetris S
                14 => "shape-red", // Tetris Z
                _ => ""
            };
        }
    }

    public class BlockShape
    {
        public int Id { get; set; }
        public int[,] Matrix { get; set; } = new int[0,0];
        public string ColorClass { get; set; } = "";
        public int ColorId { get; set; }

        public static List<BlockShape> GetAllShapes()
        {
            return new List<BlockShape>
            {
                // 1x1
                new BlockShape { Matrix = new int[,] { {1} }, ColorClass = "shape-blue", ColorId = 1 },
                
                // 2x1 and 1x2
                new BlockShape { Matrix = new int[,] { {1,1} }, ColorClass = "shape-red", ColorId = 2 },
                new BlockShape { Matrix = new int[,] { {1},{1} }, ColorClass = "shape-red", ColorId = 2 },
                
                // 3x1 and 1x3
                new BlockShape { Matrix = new int[,] { {1,1,1} }, ColorClass = "shape-green", ColorId = 3 },
                new BlockShape { Matrix = new int[,] { {1},{1},{1} }, ColorClass = "shape-green", ColorId = 3 },
                
                // 4x1 and 1x4
                new BlockShape { Matrix = new int[,] { {1,1,1,1} }, ColorClass = "shape-yellow", ColorId = 4 },
                new BlockShape { Matrix = new int[,] { {1},{1},{1},{1} }, ColorClass = "shape-yellow", ColorId = 4 },
                
                // 5x1 and 1x5
                new BlockShape { Matrix = new int[,] { {1,1,1,1,1} }, ColorClass = "shape-purple", ColorId = 5 },
                new BlockShape { Matrix = new int[,] { {1},{1},{1},{1},{1} }, ColorClass = "shape-purple", ColorId = 5 },

                // 2x2 square
                new BlockShape { Matrix = new int[,] { {1,1}, {1,1} }, ColorClass = "shape-cyan", ColorId = 6 },

                // 3x3 square
                new BlockShape { Matrix = new int[,] { {1,1,1}, {1,1,1}, {1,1,1} }, ColorClass = "shape-orange", ColorId = 7 },

                // 2x2 L
                new BlockShape { Matrix = new int[,] { {1,0}, {1,1} }, ColorClass = "shape-pink", ColorId = 8 },
                new BlockShape { Matrix = new int[,] { {0,1}, {1,1} }, ColorClass = "shape-pink", ColorId = 8 },
                new BlockShape { Matrix = new int[,] { {1,1}, {1,0} }, ColorClass = "shape-pink", ColorId = 8 },
                new BlockShape { Matrix = new int[,] { {1,1}, {0,1} }, ColorClass = "shape-pink", ColorId = 8 },

                // 3x3 L
                new BlockShape { Matrix = new int[,] { {1,0,0}, {1,0,0}, {1,1,1} }, ColorClass = "shape-teal", ColorId = 9 },
                new BlockShape { Matrix = new int[,] { {0,0,1}, {0,0,1}, {1,1,1} }, ColorClass = "shape-teal", ColorId = 9 },
                new BlockShape { Matrix = new int[,] { {1,1,1}, {1,0,0}, {1,0,0} }, ColorClass = "shape-teal", ColorId = 9 },
                new BlockShape { Matrix = new int[,] { {1,1,1}, {0,0,1}, {0,0,1} }, ColorClass = "shape-teal", ColorId = 9 },

                // T shape
                new BlockShape { Matrix = new int[,] { {1,1,1}, {0,1,0} }, ColorClass = "shape-lime", ColorId = 10 },
                new BlockShape { Matrix = new int[,] { {0,1,0}, {1,1,1} }, ColorClass = "shape-lime", ColorId = 10 },
                new BlockShape { Matrix = new int[,] { {1,0}, {1,1}, {1,0} }, ColorClass = "shape-lime", ColorId = 10 },
                new BlockShape { Matrix = new int[,] { {0,1}, {1,1}, {0,1} }, ColorClass = "shape-lime", ColorId = 10 },

                // Standard Tetris L (3x2)
                new BlockShape { Matrix = new int[,] { {1,0}, {1,0}, {1,1} }, ColorClass = "shape-blue", ColorId = 11 },
                new BlockShape { Matrix = new int[,] { {1,1,1}, {1,0,0} }, ColorClass = "shape-blue", ColorId = 11 },
                new BlockShape { Matrix = new int[,] { {1,1}, {0,1}, {0,1} }, ColorClass = "shape-blue", ColorId = 11 },
                new BlockShape { Matrix = new int[,] { {0,0,1}, {1,1,1} }, ColorClass = "shape-blue", ColorId = 11 },

                // Standard Tetris J (3x2)
                new BlockShape { Matrix = new int[,] { {0,1}, {0,1}, {1,1} }, ColorClass = "shape-orange", ColorId = 12 },
                new BlockShape { Matrix = new int[,] { {1,0,0}, {1,1,1} }, ColorClass = "shape-orange", ColorId = 12 },
                new BlockShape { Matrix = new int[,] { {1,1}, {1,0}, {1,0} }, ColorClass = "shape-orange", ColorId = 12 },
                new BlockShape { Matrix = new int[,] { {1,1,1}, {0,0,1} }, ColorClass = "shape-orange", ColorId = 12 },

                // Standard Tetris S
                new BlockShape { Matrix = new int[,] { {0,1,1}, {1,1,0} }, ColorClass = "shape-green", ColorId = 13 },
                new BlockShape { Matrix = new int[,] { {1,0}, {1,1}, {0,1} }, ColorClass = "shape-green", ColorId = 13 },

                // Standard Tetris Z
                new BlockShape { Matrix = new int[,] { {1,1,0}, {0,1,1} }, ColorClass = "shape-red", ColorId = 14 },
                new BlockShape { Matrix = new int[,] { {0,1}, {1,1}, {1,0} }, ColorClass = "shape-red", ColorId = 14 },

                // Diagonal domino (2 blocks)
                new BlockShape { Matrix = new int[,] { {1,0}, {0,1} }, ColorClass = "shape-pink", ColorId = 8 },
                new BlockShape { Matrix = new int[,] { {0,1}, {1,0} }, ColorClass = "shape-pink", ColorId = 8 }
            };
        }
    }
}
