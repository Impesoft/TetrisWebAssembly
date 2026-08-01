using BlazorArcade.GameLogic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorArcade.Pages
{
    public partial class BlockDude : ComponentBase, IDisposable
    {
        [Inject]
        public NavigationManager Nav { get; set; }

        private ElementReference _gameContainer;
        private BlockDudeGame _game;
        private Timer _timer;
        private DateTime _lastUpdateTime;
        private const int TileSize = 32;

        protected override void OnInitialized()
        {
            _game = new BlockDudeGame();
            _game.LoadLevel(0);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await _gameContainer.FocusAsync();
                
                _lastUpdateTime = DateTime.UtcNow;
                _timer = new Timer(GameLoop, null, 0, 16); // ~60 FPS
            }
        }

        private void GameLoop(object state)
        {
            var now = DateTime.UtcNow;
            var dt = (now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;

            if (_game != null)
            {
                _game.Update(dt);
                InvokeAsync(StateHasChanged);
            }
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (_game.GameComplete)
            {
                return;
            }

            if (_game.LevelComplete)
            {
                if (e.Code == "Space" || e.Code == "Enter")
                {
                    NextLevel();
                }
                return;
            }

            switch (e.Code)
            {
                case "ArrowLeft":
                case "KeyA":
                    _game.MoveLeft();
                    break;
                case "ArrowRight":
                case "KeyD":
                    _game.MoveRight();
                    break;
                case "ArrowUp":
                case "KeyW":
                case "Space":
                    _game.Action();
                    break;
                case "KeyR":
                    ResetLevel();
                    break;
            }
        }

        private void ResetLevel()
        {
            _game.ResetLevel();
        }

        private void NextLevel()
        {
            _game.NextLevel();
        }

        private void RestartGame()
        {
            _game.LoadLevel(0);
        }

        private void GoHome()
        {
            Nav.NavigateTo("/");
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
