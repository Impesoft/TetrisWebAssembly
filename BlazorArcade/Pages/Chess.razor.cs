using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessDotNet;
using BlazorArcade.GameLogic;

namespace BlazorArcade.Pages
{
    public partial class Chess
    {
        public ChessGame Game { get; set; } = new ChessGame();
        public Position? SelectedPosition { get; set; }
        public List<Move> ValidMoves { get; set; } = new List<Move>();
        
        public bool PlayAgainstComputer { get; set; } = true;
        private ChessAI _ai = new ChessAI(Player.Black, 3); // AI plays Black, Depth 3
        private bool _isAiThinking = false;

        protected override void OnInitialized()
        {
            NewGame();
        }

        private void NewGame()
        {
            Game = new ChessGame();
            SelectedPosition = null;
            ValidMoves.Clear();
            _isAiThinking = false;
        }

        private Position GetPosition(int row, int col)
        {
            // ChessDotNet uses Rank 1-8 (bottom to top), File A-H (left to right)
            // UI row 0 is Rank 8. UI col 0 is File A.
            return new Position((ChessDotNet.File)col, 8 - row);
        }

        private string GetPieceImage(Piece piece)
        {
            if (piece == null) return "";
            
            string color = piece.Owner == Player.White ? "w" : "b";
            string type = "";

            if (piece is ChessDotNet.Pieces.Pawn) type = "p";
            else if (piece is ChessDotNet.Pieces.Knight) type = "n";
            else if (piece is ChessDotNet.Pieces.Bishop) type = "b";
            else if (piece is ChessDotNet.Pieces.Rook) type = "r";
            else if (piece is ChessDotNet.Pieces.Queen) type = "q";
            else if (piece is ChessDotNet.Pieces.King) type = "k";

            return $"./images/chess/{color}{type}.svg";
        }

        private async Task OnSquareClick(Position pos)
        {
            if (_isAiThinking) return;

            if (Game.IsCheckmated(Player.White) || Game.IsCheckmated(Player.Black) || Game.IsStalemated(Player.White) || Game.IsStalemated(Player.Black))
                return; // Game over

            // If we have a selected piece and we clicked a valid move destination
            var move = ValidMoves.FirstOrDefault(m => m.NewPosition.Equals(pos));
            if (move != null)
            {
                Game.MakeMove(move, true);
                SelectedPosition = null;
                ValidMoves.Clear();
                
                if (PlayAgainstComputer && Game.WhoseTurn == Player.Black && !Game.IsCheckmated(Player.Black) && !Game.IsStalemated(Player.Black))
                {
                    _isAiThinking = true;
                    StateHasChanged();
                    
                    // Allow UI to update before AI calculation blocks thread
                    await Task.Delay(50);
                    
                    var aiMove = _ai.GetBestMove(Game);
                    if (aiMove != null)
                    {
                        Game.MakeMove(aiMove, true);
                    }
                    
                    _isAiThinking = false;
                }
            }
            else
            {
                // Select piece
                var piece = Game.GetPieceAt(pos);
                if (piece != null && piece.Owner == Game.WhoseTurn)
                {
                    SelectedPosition = pos;
                    ValidMoves = Game.GetValidMoves(pos).ToList();
                }
                else
                {
                    SelectedPosition = null;
                    ValidMoves.Clear();
                }
            }
        }
    }
}
