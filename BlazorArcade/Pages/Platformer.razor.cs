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
            bool moveLeft = _pressedKeys.Contains("ArrowLeft") || _pressedKeys.Contains("a") || _pressedKeys.Contains("A");
            bool moveRight = _pressedKeys.Contains("ArrowRight") || _pressedKeys.Contains("d") || _pressedKeys.Contains("D");
            bool jump = _pressedKeys.Contains("ArrowUp") || _pressedKeys.Contains("w") || _pressedKeys.Contains("W") || _pressedKeys.Contains(" ");

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
