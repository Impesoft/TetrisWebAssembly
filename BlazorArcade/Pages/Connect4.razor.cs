using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BlazorArcade.GameLogic;
using System.Threading.Tasks;

namespace BlazorArcade.Pages
{
    public partial class Connect4
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; }
        
        public Connect4Logic GameInstance { get; set; } = new Connect4Logic();
        private Connect4AI _ai = new Connect4AI(2);
        private ElementReference GameContainer;
        public bool IsAiMode { get; set; } = true;
        private bool _isAiThinking = false;

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
            _isAiThinking = false;
            GameContainer.FocusAsync();
        }

        public void ToggleMode()
        {
            IsAiMode = !IsAiMode;
            RestartGame();
        }

        public async Task DropPiece(int col)
        {
            if (_isAiThinking || GameInstance.IsGameOver) return;

            if (GameInstance.DropPiece(col))
            {
                _ = JSRuntime.InvokeVoidAsync("playWaka"); // Using the same sound for piece drop as an arcade feel
                
                if (IsAiMode && !GameInstance.IsGameOver && GameInstance.CurrentPlayer == 2)
                {
                    _isAiThinking = true;
                    StateHasChanged();
                    
                    await Task.Delay(500); // Artificial delay to make AI feel more natural
                    
                    int bestMove = _ai.GetBestMove(GameInstance.Board);
                    if (bestMove != -1 && GameInstance.DropPiece(bestMove))
                    {
                        _ = JSRuntime.InvokeVoidAsync("playWaka");
                    }
                    
                    _isAiThinking = false;
                    StateHasChanged();
                }
            }
        }
    }
}
