using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorArcade.GameLogic;

namespace BlazorArcade.Pages
{
    public partial class DonkeyKong : IDisposable
    {
        [Inject] private NavigationManager _nav { get; set; } = default!;

        public DonkeyKongGame GameInstance { get; set; } = new DonkeyKongGame();
        public Timer? GameTimer { get; set; }

        private ElementReference GameContainer;

        // Key states
        private HashSet<string> _pressedKeys = new HashSet<string>();

        // On-screen touch state
        private bool _touchMoveLeft;
        private bool _touchMoveRight;
        private bool _touchClimbUp;
        private bool _touchClimbDown;
        private bool _touchJump;

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
                // Run game loop at ~60 FPS (16 ms tick)
                GameTimer = new Timer(GameLoop, null, 0, 16);
            }

            _ = GameContainer.FocusAsync();
        }

        public void RestartStage()
        {
            GameInstance.ResetStage();
            _pressedKeys.Clear();
            _ = GameContainer.FocusAsync();
        }

        private void GameLoop(object? state)
        {
            bool moveLeft = _pressedKeys.Contains("ArrowLeft") || _pressedKeys.Contains("a") || _pressedKeys.Contains("A") || _touchMoveLeft;
            bool moveRight = _pressedKeys.Contains("ArrowRight") || _pressedKeys.Contains("d") || _pressedKeys.Contains("D") || _touchMoveRight;
            bool climbUp = _pressedKeys.Contains("ArrowUp") || _pressedKeys.Contains("w") || _pressedKeys.Contains("W") || _touchClimbUp;
            bool climbDown = _pressedKeys.Contains("ArrowDown") || _pressedKeys.Contains("s") || _pressedKeys.Contains("S") || _touchClimbDown;
            bool jump = _pressedKeys.Contains(" ") || _pressedKeys.Contains("Space") || _touchJump;

            GameInstance.Update(moveLeft, moveRight, climbUp, climbDown, jump, 0.016);
            InvokeAsync(StateHasChanged);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Repeat) return;
            _pressedKeys.Add(e.Key);
        }

        private void HandleKeyUp(KeyboardEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        // On-screen D-Pad and Button Handlers for Touch/Mobile
        private void SetMoveLeft(bool active) => _touchMoveLeft = active;
        private void SetMoveRight(bool active) => _touchMoveRight = active;
        private void SetClimbUp(bool active) => _touchClimbUp = active;
        private void SetClimbDown(bool active) => _touchClimbDown = active;
        private void SetJump(bool active) => _touchJump = active;

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
