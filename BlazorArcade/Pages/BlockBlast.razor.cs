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
        /// Authentic Block Blast feature: Starts with 5 to 8 pre-placed random blocks on the board
        /// so the board is NEVER empty at game start.
        /// </summary>
        private void GenerateStarterBoard()
        {
            int starterCount = _random.Next(5, 9); // 5 to 8 starter blocks
            int placed = 0;
            int attempts = 0;

            while (placed < starterCount && attempts < 100)
            {
                attempts++;
                int r = _random.Next(0, GridSize);
                int c = _random.Next(0, GridSize);

                // Avoid blocking entire rows/cols right at start
                if (Grid[r, c] == 0)
                {
                    int colorId = _random.Next(1, 11);
                    Grid[r, c] = colorId;
                    placed++;
                }
            }
        }

        private void GenerateShapes()
        {
            AvailableShapes.Clear();
            var allShapes = BlockShape.GetAllShapes();
            for (int i = 0; i < 3; i++)
            {
                var shapeTemplate = allShapes[_random.Next(allShapes.Count)];
                AvailableShapes.Add(new BlockShape
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    Matrix = shapeTemplate.Matrix,
                    ColorClass = shapeTemplate.ColorClass,
                    ColorId = shapeTemplate.ColorId
                });
            }
            CheckGameOver();
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

        public async Task HandleCellClick(int r, int c)
        {
            if (IsGameOver || SelectedShape == null) return;

            if (CanPlaceShape(SelectedShape, r, c))
            {
                int blocksCount = PlaceShape(SelectedShape, r, c);
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
            Score += blocksPlaced;
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

                Score += totalTurnScore;

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

                FloatingSubMessage = $"+{totalTurnScore}";
                ShowFloatingMessage = true;

                _ = Task.Delay(1200).ContinueWith(_ =>
                {
                    ShowFloatingMessage = false;
                    InvokeAsync(StateHasChanged);
                });

                return true;
            }

            return false;
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

            int sr = r - HoverRow.Value;
            int sc = c - HoverCol.Value;

            if (sr >= 0 && sr < SelectedShape.Matrix.GetLength(0) &&
                sc >= 0 && sc < SelectedShape.Matrix.GetLength(1))
            {
                if (SelectedShape.Matrix[sr, sc] == 1)
                {
                    bool isValid = CanPlaceShape(SelectedShape, HoverRow.Value, HoverCol.Value);
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
                new BlockShape { Matrix = new int[,] { {0,1}, {1,1}, {0,1} }, ColorClass = "shape-lime", ColorId = 10 }
            };
        }
    }
}
