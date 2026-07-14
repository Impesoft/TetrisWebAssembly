using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorArcade.GameLogic;
using Timer = System.Threading.Timer;

namespace BlazorArcade.Pages
{
    public partial class Pacman : IDisposable
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; }
        
        public PacmanGame GameInstance { get; set; } = new PacmanGame();
        public Timer GameTimer { get; set; }
        
        public double BlockSize { get; set; } = 20; // 20px per block

        public bool IsMouthOpen { get; set; } = true;
        public bool IsPowerPelletBlinking { get; set; } = true;
        public bool IsFrightenedBlinking { get; set; } = true;

        private ElementReference GameContainer;
        
        private int _tickCount = 0;

        // Touch handling
        private double? _touchStartX;
        private double? _touchStartY;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GameContainer.FocusAsync();
            }
        }

        public void StartGame()
        {
            if (GameInstance.IsGameOver || GameInstance.IsGameWon)
            {
                GameInstance.InitializeGame();
            }
            
            if (GameTimer == null)
            {
                // Run game loop roughly 6 times a second
                GameTimer = new Timer(GameLoop, null, 0, 150);
            }
            GameContainer.FocusAsync();
        }

        public void PauseGame()
        {
            if (GameTimer != null)
            {
                GameTimer.Dispose();
                GameTimer = null;
            }
        }

        private void GameLoop(object state)
        {
            GameInstance.Update();
            
            if (GameInstance.JustAteDot) _ = JSRuntime.InvokeVoidAsync("playWaka");
            if (GameInstance.JustAteGhost) _ = JSRuntime.InvokeVoidAsync("playEatGhost");
            if (GameInstance.JustDied) _ = JSRuntime.InvokeVoidAsync("playDie");
            
            _tickCount++;
            
            // Toggle animations
            IsMouthOpen = !IsMouthOpen;
            if (_tickCount % 2 == 0) IsPowerPelletBlinking = !IsPowerPelletBlinking;
            if (_tickCount % 2 == 0) IsFrightenedBlinking = !IsFrightenedBlinking;

            InvokeAsync(StateHasChanged);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case "ArrowUp":
                case "w":
                case "W":
                    GameInstance.SetNextDirection(Direction.Up);
                    break;
                case "ArrowDown":
                case "s":
                case "S":
                    GameInstance.SetNextDirection(Direction.Down);
                    break;
                case "ArrowLeft":
                case "a":
                case "A":
                    GameInstance.SetNextDirection(Direction.Left);
                    break;
                case "ArrowRight":
                case "d":
                case "D":
                    GameInstance.SetNextDirection(Direction.Right);
                    break;
            }
        }

        private void HandleTouchStart(TouchEventArgs e)
        {
            if (e.Touches.Length > 0)
            {
                _touchStartX = e.Touches[0].ClientX;
                _touchStartY = e.Touches[0].ClientY;
            }
        }

        private void HandleTouchEnd(TouchEventArgs e)
        {
            if (e.ChangedTouches.Length > 0 && _touchStartX.HasValue && _touchStartY.HasValue)
            {
                var touchEndX = e.ChangedTouches[0].ClientX;
                var touchEndY = e.ChangedTouches[0].ClientY;

                var dx = touchEndX - _touchStartX.Value;
                var dy = touchEndY - _touchStartY.Value;

                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    // Horizontal swipe
                    if (Math.Abs(dx) > 30) // Minimum swipe distance
                    {
                        if (dx > 0)
                            GameInstance.SetNextDirection(Direction.Right);
                        else
                            GameInstance.SetNextDirection(Direction.Left);
                    }
                }
                else
                {
                    // Vertical swipe
                    if (Math.Abs(dy) > 30)
                    {
                        if (dy > 0)
                            GameInstance.SetNextDirection(Direction.Down);
                        else
                            GameInstance.SetNextDirection(Direction.Up);
                    }
                }

                _touchStartX = null;
                _touchStartY = null;
            }
        }

        public void Dispose()
        {
            GameTimer?.Dispose();
        }
    }
}
