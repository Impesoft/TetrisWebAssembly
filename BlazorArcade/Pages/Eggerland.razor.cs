using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorArcade.GameLogic;

namespace BlazorArcade.Pages
{
    public partial class Eggerland : IDisposable
    {
        [Inject] private NavigationManager _nav { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        public EggerlandGame GameInstance { get; set; } = new EggerlandGame();
        public Timer? GameTimer { get; set; }

        private ElementReference GameContainer;

        private HashSet<string> _pressedKeys = new HashSet<string>();

        // Touch states
        private bool _touchUp;
        private bool _touchDown;
        private bool _touchLeft;
        private bool _touchRight;
        private bool _touchShoot;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // StartGame();
                await GameContainer.FocusAsync();
            }
        }

        public void StartGame()
        {
            GameInstance.LoadLevel(0);
            _pressedKeys.Clear();

            if (GameTimer == null)
            {
                GameTimer = new Timer(GameLoop, null, 0, 16);
            }

            _ = GameContainer.FocusAsync();
        }

        private async void GameLoop(object? state)
        {
            bool up = _pressedKeys.Contains("ArrowUp") || _pressedKeys.Contains("w") || _pressedKeys.Contains("W") || _touchUp;
            bool down = _pressedKeys.Contains("ArrowDown") || _pressedKeys.Contains("s") || _pressedKeys.Contains("S") || _touchDown;
            bool left = _pressedKeys.Contains("ArrowLeft") || _pressedKeys.Contains("a") || _pressedKeys.Contains("A") || _touchLeft;
            bool right = _pressedKeys.Contains("ArrowRight") || _pressedKeys.Contains("d") || _pressedKeys.Contains("D") || _touchRight;
            bool shoot = _pressedKeys.Contains(" ") || _pressedKeys.Contains("Space") || _pressedKeys.Contains("Enter") || _touchShoot;

            GameInstance.Update(up, down, left, right, shoot, 0.016);

            // Play audio synthesizers if triggered
            if (GameInstance.SoundPlayStep) TriggerSound("step");
            if (GameInstance.SoundPlayShot) TriggerSound("shot");
            if (GameInstance.SoundPlayEgg) TriggerSound("egg");
            if (GameInstance.SoundPlayPush) TriggerSound("push");
            if (GameInstance.SoundPlayHeart) TriggerSound("heart");
            if (GameInstance.SoundPlayChest) TriggerSound("chest");
            if (GameInstance.SoundPlayWin) TriggerSound("win");
            if (GameInstance.SoundPlayDeath) TriggerSound("death");

            await InvokeAsync(StateHasChanged);
        }

        private void TriggerSound(string soundType)
        {
            _ = JSRuntime.InvokeVoidAsync("playEggerlandSound", soundType);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            _pressedKeys.Add(e.Key);

            if (e.Key == "u" || e.Key == "U")
            {
                UndoMove();
            }
            else if (e.Key == "r" || e.Key == "R")
            {
                RestartStage();
            }
        }

        private void HandleKeyUp(KeyboardEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        public void RestartStage()
        {
            GameInstance.RestartLevel();
            _pressedKeys.Clear();
            _ = GameContainer.FocusAsync();
        }

        public void UndoMove()
        {
            GameInstance.Undo();
            _ = GameContainer.FocusAsync();
        }

        public void SelectLevel(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int levelIndex))
            {
                GameInstance.LoadLevel(levelIndex);
                _pressedKeys.Clear();
                _ = GameContainer.FocusAsync();
            }
        }

        private void SetTouchDir(string dir, bool active)
        {
            switch (dir)
            {
                case "up": _touchUp = active; break;
                case "down": _touchDown = active; break;
                case "left": _touchLeft = active; break;
                case "right": _touchRight = active; break;
                case "shoot": _touchShoot = active; break;
            }
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

