using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorArcade.GameLogic;
using System.Threading.Tasks;
using System;

namespace BlazorArcade.Pages
{
    public partial class Game2048
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; }
        
        public Game2048Logic GameInstance { get; set; } = new Game2048Logic();
        private ElementReference GameContainer;
        
        private double? _touchStartX;
        private double? _touchStartY;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GameContainer.FocusAsync();
            }
        }

        public void RestartGame()
        {
            GameInstance.InitializeGame();
            GameContainer.FocusAsync();
        }
        
        public void ContinueGame()
        {
            GameInstance.ContinuePlaying();
            GameContainer.FocusAsync();
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            bool moved = false;
            switch (e.Key)
            {
                case "ArrowUp":
                case "w":
                case "W":
                    moved = GameInstance.Move(Direction.Up);
                    break;
                case "ArrowDown":
                case "s":
                case "S":
                    moved = GameInstance.Move(Direction.Down);
                    break;
                case "ArrowLeft":
                case "a":
                case "A":
                    moved = GameInstance.Move(Direction.Left);
                    break;
                case "ArrowRight":
                case "d":
                case "D":
                    moved = GameInstance.Move(Direction.Right);
                    break;
            }

            if (moved)
            {
                _ = JSRuntime.InvokeVoidAsync("playWaka"); 
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

                bool moved = false;

                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    if (Math.Abs(dx) > 30) // Horizontal swipe
                    {
                        moved = GameInstance.Move(dx > 0 ? Direction.Right : Direction.Left);
                    }
                }
                else
                {
                    if (Math.Abs(dy) > 30) // Vertical swipe
                    {
                        moved = GameInstance.Move(dy > 0 ? Direction.Down : Direction.Up);
                    }
                }

                if (moved)
                {
                    _ = JSRuntime.InvokeVoidAsync("playWaka");
                }

                _touchStartX = null;
                _touchStartY = null;
            }
        }

        private string GetTileClass(int value)
        {
            if (value > 2048) return "tile-super";
            return "";
        }
    }
}
