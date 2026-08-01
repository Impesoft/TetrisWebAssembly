using System;
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorArcade.GameLogic;
using BlazorArcade.Models;

namespace BlazorArcade.Pages
{
    public partial class Tangram : ComponentBase, IDisposable
    {
        [Inject] public NavigationManager Navigation { get; set; } = null!;
        [Inject] public HttpClient Http { get; set; } = null!;
        [Inject] public IJSRuntime JS { get; set; } = null!;

        public TangramGame Game { get; private set; } = new TangramGame();

        private Timer? _gameTimer;
        private bool _isDragging = false;
        private string? _draggedPieceId = null;
        private double _dragStartX = 0;
        private double _dragStartY = 0;
        private double _pieceOriginX = 0;
        private double _pieceOriginY = 0;

        public string CustomShapeName { get; set; } = string.Empty;
        public bool ShowVictoryModal => Game.IsCompleted && Game.Mode != TangramGameMode.Sandbox;
        public bool ShowLevelSelectModal { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            Game.OnStateChanged += HandleStateChanged;
            await Game.InitializeGameAsync(Http, JS);
            StartTimer();
        }

        private void StartTimer()
        {
            _gameTimer?.Dispose();
            _gameTimer = new Timer(_ =>
            {
                if (!Game.IsCompleted && Game.Mode != TangramGameMode.Sandbox)
                {
                    Game.ElapsedSeconds++;
                    InvokeAsync(StateHasChanged);
                }
            }, null, 1000, 1000);
        }

        private void HandleStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        #region Pointer & Drag Handlers
        public void OnPointerDown(PointerEventArgs e, TangramPiece piece)
        {
            Game.SelectPiece(piece.Id);
            _isDragging = true;
            _draggedPieceId = piece.Id;

            _dragStartX = e.OffsetX;
            _dragStartY = e.OffsetY;
            _pieceOriginX = piece.X;
            _pieceOriginY = piece.Y;
        }

        public void OnPointerMove(PointerEventArgs e)
        {
            if (!_isDragging || string.IsNullOrEmpty(_draggedPieceId)) return;

            double deltaX = e.OffsetX - _dragStartX;
            double deltaY = e.OffsetY - _dragStartY;

            double newX = _pieceOriginX + deltaX;
            double newY = _pieceOriginY + deltaY;

            // Restrict bounds within SVG board
            newX = Math.Clamp(newX, 30, 770);
            newY = Math.Clamp(newY, 30, 620);

            Game.MovePiece(_draggedPieceId, newX, newY, snapToGrid: false);
        }

        public async Task OnPointerUp(PointerEventArgs e)
        {
            if (_isDragging && !string.IsNullOrEmpty(_draggedPieceId))
            {
                var piece = Game.Pieces.FirstOrDefault(p => p.Id == _draggedPieceId);
                if (piece != null)
                {
                    // Apply magnetic snapping on drop
                    Game.MovePiece(piece.Id, piece.X, piece.Y, snapToGrid: true);
                    
                    // Only check completion when dropping a piece, to avoid lag
                    await Game.CheckCompletionAsync();
                }
            }

            _isDragging = false;
            _draggedPieceId = null;
        }

        public void OnBoardClick()
        {
            if (!_isDragging)
            {
                // Unselect when clicking on background
            }
        }
        #endregion

        #region Controls & Actions
        public async Task RotateLeft() => await Game.RotateSelectedPieceAsync(-45);
        public async Task RotateRight() => await Game.RotateSelectedPieceAsync(45);
        public async Task FlipPiece() => await Game.FlipSelectedPieceAsync();

        public async Task ReturnSelectedToTray()
        {
            if (Game.SelectedPiece != null)
            {
                await Game.ReturnPieceToTrayAsync(Game.SelectedPiece.Id);
            }
        }

        public async Task UseHint() => await Game.ProvideHintAsync();
        public void ResetLevel() => Game.ResetCurrentLevel();

        public void NextLevel()
        {
            if (Game.CurrentLevelIndex < Game.Levels.Count - 1)
            {
                Game.LoadLevel(Game.CurrentLevelIndex + 1);
            }
        }

        public void PreviousLevel()
        {
            if (Game.CurrentLevelIndex > 0)
            {
                Game.LoadLevel(Game.CurrentLevelIndex - 1);
            }
        }

        public void SelectCategory(ChangeEventArgs e)
        {
            string category = e.Value?.ToString() ?? "All";
            Game.SelectedCategory = category;

            var filtered = GetFilteredLevels();
            if (filtered.Any())
            {
                int targetId = filtered.First().Id - 1;
                Game.LoadLevel(targetId);
            }
        }

        public System.Collections.Generic.List<TangramLevel> GetFilteredLevels()
        {
            if (Game.SelectedCategory == "All" || string.IsNullOrEmpty(Game.SelectedCategory))
                return Game.Levels;

            return Game.Levels.Where(l => l.Category == Game.SelectedCategory).ToList();
        }

        public void SelectTheme(TangramTheme theme)
        {
            Game.Theme = theme;
            StateHasChanged();
        }

        public void SelectMode(TangramGameMode mode)
        {
            Game.Mode = mode;
            Game.ResetCurrentLevel();
        }

        public void SaveCustomLevel()
        {
            Game.SaveCustomSandboxLevel(CustomShapeName, "Custom");
            CustomShapeName = string.Empty;
        }

        public void GoHome()
        {
            Navigation.NavigateTo("/");
        }

        public async Task OnKeyDown(KeyboardEventArgs e)
        {
            switch (e.Key.ToLower())
            {
                case "q":
                case "arrowleft":
                    await RotateLeft();
                    break;
                case "e":
                case "arrowright":
                    await RotateRight();
                    break;
                case "f":
                case " ":
                    await FlipPiece();
                    break;
                case "r":
                    ResetLevel();
                    break;
                case "h":
                    await UseHint();
                    break;
                case "t":
                    await ReturnSelectedToTray();
                    break;
            }
        }
        #endregion

        public void Dispose()
        {
            if (Game != null)
            {
                Game.OnStateChanged -= HandleStateChanged;
            }
            _gameTimer?.Dispose();
        }
    }
}
