using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorArcade.GameLogic;

namespace BlazorArcade.Pages
{
    public partial class KingsValley : IDisposable
    {
        [Inject] private NavigationManager _nav { get; set; } = default!;

        public KingsValleyGame GameInstance { get; set; } = new KingsValleyGame();
        public Timer? GameTimer { get; set; }

        private ElementReference GameContainer;

        // Key states
        private HashSet<string> _pressedKeys = new HashSet<string>();

        // Render gate to block OS key-repeat events from triggering duplicate Blazor re-renders
        private bool _isGameLoopRendering = false;

        // One-shot action triggers
        private bool _actionDigTriggered = false;
        private bool _actionThrowTriggered = false;

        // On-screen touch state
        private bool _touchMoveLeft;
        private bool _touchMoveRight;
        private bool _touchMoveUp;
        private bool _touchMoveDown;
        private bool _touchJump;

        protected override bool ShouldRender()
        {
            if (_isGameLoopRendering)
            {
                _isGameLoopRendering = false;
                return true;
            }
            return false; // Suppress all Blazor event-driven re-renders (onkeydown, onkeyup, touch)
        }

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
            _actionDigTriggered = false;
            _actionThrowTriggered = false;

            if (GameTimer == null)
            {
                // 60 FPS tick (16 ms)
                GameTimer = new Timer(GameLoop, null, 0, 16);
            }

            _ = GameContainer.FocusAsync();
        }

        public void RestartStage()
        {
            GameInstance.RestartStage();
            _pressedKeys.Clear();
            _actionDigTriggered = false;
            _actionThrowTriggered = false;
            _ = GameContainer.FocusAsync();
        }

        public void NextStage()
        {
            GameInstance.LoadLevel(GameInstance.Level);
            _pressedKeys.Clear();
            _actionDigTriggered = false;
            _actionThrowTriggered = false;
            _ = GameContainer.FocusAsync();
        }

        private void GameLoop(object? state)
        {
            bool moveLeft = _pressedKeys.Contains("ArrowLeft") || _pressedKeys.Contains("a") || _pressedKeys.Contains("A") || _touchMoveLeft;
            bool moveRight = _pressedKeys.Contains("ArrowRight") || _pressedKeys.Contains("d") || _pressedKeys.Contains("D") || _touchMoveRight;
            bool moveUp = _pressedKeys.Contains("ArrowUp") || _pressedKeys.Contains("w") || _pressedKeys.Contains("W") || _touchMoveUp;
            bool moveDown = _pressedKeys.Contains("ArrowDown") || _pressedKeys.Contains("s") || _pressedKeys.Contains("S") || _touchMoveDown;
            bool jump = _pressedKeys.Contains(" ") || _pressedKeys.Contains("Space") || _touchJump;

            // One-shot triggers for Dig and Throw Knife
            bool actionDig = _actionDigTriggered;
            _actionDigTriggered = false;

            bool actionThrow = _actionThrowTriggered;
            _actionThrowTriggered = false;

            GameInstance.Update(moveLeft, moveRight, moveUp, moveDown, jump, actionDig, actionThrow, 0.016);

            // Gate rendering to GameLoop only
            _isGameLoopRendering = true;
            InvokeAsync(StateHasChanged);
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Repeat) return;

            _pressedKeys.Add(e.Key);

            if (e.Key.Equals("k", StringComparison.OrdinalIgnoreCase))
            {
                _actionDigTriggered = true;
            }
            else if (e.Key.Equals("l", StringComparison.OrdinalIgnoreCase) || e.Key.Equals("f", StringComparison.OrdinalIgnoreCase))
            {
                _actionThrowTriggered = true;
            }
        }

        private void HandleKeyUp(KeyboardEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        // On-screen D-Pad and Action Handlers
        private void SetMoveLeft(bool active) => _touchMoveLeft = active;
        private void SetMoveRight(bool active) => _touchMoveRight = active;
        private void SetMoveUp(bool active) => _touchMoveUp = active;
        private void SetMoveDown(bool active) => _touchMoveDown = active;
        private void SetJump(bool active) => _touchJump = active;

        private void TriggerDig() => _actionDigTriggered = true;
        private void TriggerThrow() => _actionThrowTriggered = true;

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
