using BlazorArcade.GameLogic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorArcade.Pages
{
    public partial class FlappyBird : IDisposable
    {
        private ElementReference _gameContainer;
        private FlappyBirdGame _game = new();
        private Timer _timer;
        private DateTime _lastUpdateTime;
        private bool _isDisposed;

        protected override void OnInitialized()
        {
            _lastUpdateTime = DateTime.Now;
            _timer = new Timer(GameLoop, null, 0, 16); // ~60fps
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await _gameContainer.FocusAsync();
            }
        }

        private void GameLoop(object state)
        {
            if (_isDisposed) return;

            var now = DateTime.Now;
            var dt = (now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;

            // Cap dt to prevent massive jumps if tab is inactive
            if (dt > 0.1) dt = 0.1;

            _game.Update(dt);

            InvokeAsync(StateHasChanged);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Code == "Space" || e.Key == " ")
            {
                if (_game.IsGameOver)
                {
                    RestartGame();
                }
                else
                {
                    _game.Flap();
                }
            }
        }

        private void HandleClick(MouseEventArgs e)
        {
            if (_game.IsGameOver)
            {
                RestartGame();
            }
            else
            {
                _game.Flap();
            }
        }

        private void RestartGame()
        {
            _game.Reset();
            _lastUpdateTime = DateTime.Now;
            _ = _gameContainer.FocusAsync();
        }

        private double GetBirdRotation()
        {
            // Rotate based on velocity
            // velocity goes from ~ -500 (up) to +1000 (down)
            double rotation = _game.Bird.VelocityY * 0.05;
            
            // Clamp rotation
            if (rotation < -25) rotation = -25;
            if (rotation > 90) rotation = 90;

            return rotation;
        }

        public void Dispose()
        {
            _isDisposed = true;
            _timer?.Dispose();
        }
    }
}
