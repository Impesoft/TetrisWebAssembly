using ChessDotNet;
using System;
using System.Linq;

namespace BlazorArcade.GameLogic
{
    public class ChessAI
    {
        private readonly Player _aiColor;
        private readonly int _maxDepth;

        public ChessAI(Player aiColor, int maxDepth = 3)
        {
            _aiColor = aiColor;
            _maxDepth = maxDepth;
        }

        public Move? GetBestMove(ChessGame game)
        {
            var moves = game.GetValidMoves(_aiColor).ToList();
            if (moves.Count == 0) return null;

            Move? bestMove = null;
            int bestValue = int.MinValue;

            foreach (var move in moves)
            {
                var clone = new ChessGame(game.GetFen());
                clone.MakeMove(move, true);

                int boardValue = Minimax(clone, _maxDepth - 1, int.MinValue, int.MaxValue, false);
                
                if (boardValue > bestValue)
                {
                    bestValue = boardValue;
                    bestMove = move;
                }
            }

            // Fallback if all moves seem equally bad
            if (bestMove == null && moves.Count > 0)
            {
                var rng = new Random();
                bestMove = moves[rng.Next(moves.Count)];
            }

            return bestMove;
        }

        private int Minimax(ChessGame game, int depth, int alpha, int beta, bool isMaximizingPlayer)
        {
            Player opponent = _aiColor == Player.White ? Player.Black : Player.White;

            if (depth == 0 || game.IsCheckmated(Player.White) || game.IsCheckmated(Player.Black) || game.IsStalemated(Player.White) || game.IsStalemated(Player.Black))
            {
                return EvaluateBoard(game);
            }

            if (isMaximizingPlayer)
            {
                int bestVal = int.MinValue;
                var moves = game.GetValidMoves(_aiColor);
                if (!moves.Any())
                {
                    return EvaluateBoard(game);
                }

                foreach (var move in moves)
                {
                    var clone = new ChessGame(game.GetFen());
                    clone.MakeMove(move, true);
                    int value = Minimax(clone, depth - 1, alpha, beta, false);
                    bestVal = Math.Max(bestVal, value);
                    alpha = Math.Max(alpha, bestVal);
                    if (beta <= alpha) break;
                }
                return bestVal;
            }
            else
            {
                int bestVal = int.MaxValue;
                var moves = game.GetValidMoves(opponent);
                if (!moves.Any())
                {
                    return EvaluateBoard(game);
                }

                foreach (var move in moves)
                {
                    var clone = new ChessGame(game.GetFen());
                    clone.MakeMove(move, true);
                    int value = Minimax(clone, depth - 1, alpha, beta, true);
                    bestVal = Math.Min(bestVal, value);
                    beta = Math.Min(beta, bestVal);
                    if (beta <= alpha) break;
                }
                return bestVal;
            }
        }

        private int EvaluateBoard(ChessGame game)
        {
            Player opponent = _aiColor == Player.White ? Player.Black : Player.White;
            if (game.IsCheckmated(_aiColor)) return -10000;
            if (game.IsCheckmated(opponent)) return 10000;
            if (game.IsStalemated(_aiColor) || game.IsStalemated(opponent)) return 0;

            int score = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    var pos = new Position((ChessDotNet.File)c, 8 - r);
                    var piece = game.GetPieceAt(pos);
                    if (piece != null)
                    {
                        int value = GetPieceValue(piece);
                        score += piece.Owner == _aiColor ? value : -value;
                    }
                }
            }
            return score;
        }

        private int GetPieceValue(Piece piece)
        {
            if (piece is ChessDotNet.Pieces.Pawn) return 10;
            if (piece is ChessDotNet.Pieces.Knight) return 30;
            if (piece is ChessDotNet.Pieces.Bishop) return 30;
            if (piece is ChessDotNet.Pieces.Rook) return 50;
            if (piece is ChessDotNet.Pieces.Queen) return 90;
            if (piece is ChessDotNet.Pieces.King) return 900;
            return 0;
        }
    }
}
