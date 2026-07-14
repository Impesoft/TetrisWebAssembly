using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorArcade.GameLogic;
using Timer = System.Threading.Timer;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace BlazorArcade.Pages
{
    public partial class Platformer : IDisposable
    {
        public PlatformerGame GameInstance { get; set; } = new PlatformerGame();
        public Timer GameTimer { get; set; }
        
        private ElementReference GameContainer;
        
        // Input state
        private HashSet<string> _pressedKeys = new HashSet<string>();

        // Touch state
        private double? _touchStartX = null;
        private double? _touchStartY = null;
        private bool _swipeJump = false;
        private bool _swipeMoveLeft = false;
        private bool _swipeMoveRight = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                StartGame();
                await GameContainer.FocusAsync();
            }
        }

        public void StartGame()
        {
            GameInstance.InitializeGame();
            _pressedKeys.Clear();
            
            if (GameTimer == null)
            {
                // Run game loop at ~60fps (16ms)
                GameTimer = new Timer(GameLoop, null, 0, 16);
            }
            GameContainer.FocusAsync();
        }

        private void GameLoop(object state)
        {
            bool moveLeft = _pressedKeys.Contains("ArrowLeft") || _pressedKeys.Contains("a") || _pressedKeys.Contains("A") || _swipeMoveLeft;
            bool moveRight = _pressedKeys.Contains("ArrowRight") || _pressedKeys.Contains("d") || _pressedKeys.Contains("D") || _swipeMoveRight;
            bool jump = _pressedKeys.Contains("ArrowUp") || _pressedKeys.Contains("w") || _pressedKeys.Contains("W") || _pressedKeys.Contains(" ") || _swipeJump;

            GameInstance.Update(moveLeft, moveRight, jump);
            InvokeAsync(StateHasChanged);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            _pressedKeys.Add(e.Key);
        }

        private void HandleKeyUp(KeyboardEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        private void HandleTouchStart(TouchEventArgs e)
        {
            if (e.Touches.Length > 0)
            {
                _touchStartX = e.Touches[0].ClientX;
                _touchStartY = e.Touches[0].ClientY;
                
                _swipeJump = false;
                _swipeMoveLeft = false;
                _swipeMoveRight = false;
            }
        }

        private void HandleTouchMove(TouchEventArgs e)
        {
            if (_touchStartX.HasValue && _touchStartY.HasValue && e.Touches.Length > 0)
            {
                var deltaX = e.Touches[0].ClientX - _touchStartX.Value;
                var deltaY = e.Touches[0].ClientY - _touchStartY.Value;

                const double swipeThresholdX = 30;
                const double swipeThresholdY = 30;

                _swipeMoveRight = deltaX > swipeThresholdX;
                _swipeMoveLeft = deltaX < -swipeThresholdX;
                
                if (deltaY < -swipeThresholdY)
                {
                    _swipeJump = true;
                }
                else
                {
                    _swipeJump = false;
                }
            }
        }

        private void HandleTouchEnd(TouchEventArgs e)
        {
            _touchStartX = null;
            _touchStartY = null;
            _swipeMoveLeft = false;
            _swipeMoveRight = false;
            _swipeJump = false;
        }

        public void Dispose()
        {
            if (GameTimer != null)
            {
                GameTimer.Dispose();
                GameTimer = null;
            }
        }
    }
}
