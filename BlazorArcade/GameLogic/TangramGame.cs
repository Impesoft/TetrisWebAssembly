using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using BlazorArcade.Helpers;
using BlazorArcade.Models;

namespace BlazorArcade.GameLogic
{
    public class TangramGame
    {
        public List<TangramLevel> Levels { get; private set; } = new List<TangramLevel>();
        public TangramLevel CurrentLevel { get; private set; } = null!;
        public int CurrentLevelIndex { get; private set; } = 0;
        public bool IsLoaded { get; private set; } = false;

        private IJSRuntime? _js;

        public List<TangramPiece> Pieces { get; private set; } = new List<TangramPiece>();
        public TangramPiece? SelectedPiece { get; private set; }

        public TangramGameMode Mode { get; set; } = TangramGameMode.Silhouette;
        public TangramTheme Theme { get; set; } = TangramTheme.Neon;

        public bool IsCompleted { get; private set; } = false;
        public int ElapsedSeconds { get; set; } = 0;
        public int HintsUsed { get; set; } = 0;
        public int StarsEarned { get; set; } = 0;
        public double CompletionAccuracy { get; private set; } = 0.0;
        public int RotationsUsed { get; set; } = 0;
        public int MinimumRotations { get; private set; } = 0;
        public int Score { get; private set; } = 0;

        public string SelectedCategory { get; set; } = "All";

        public event Action? OnStateChanged;

        public TangramGame()
        {
        }

        public async Task InitializeGameAsync(HttpClient http, IJSRuntime js)
        {
            _js = js;
            Levels = await TangramLevelCatalog.LoadLevelsAsync(http);
            if (Levels.Count > 0)
            {
                LoadLevel(0);
            }
            IsLoaded = true;
            NotifyStateChanged();
        }

        public void LoadLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= Levels.Count) return;

            CurrentLevelIndex = levelIndex;
            
            var baseLevel = Levels[levelIndex];
            CurrentLevel = new TangramLevel
            {
                Id = baseLevel.Id,
                Name = baseLevel.Name,
                Category = baseLevel.Category,
                Difficulty = baseLevel.Difficulty,
                TargetTransforms = baseLevel.TargetTransforms.Select(t => t.Clone()).ToList(),
                IsCustom = baseLevel.IsCustom,
                BestTimeSeconds = baseLevel.BestTimeSeconds,
                Stars = baseLevel.Stars
            };
            
            IsCompleted = false;
            ElapsedSeconds = 0;
            HintsUsed = 0;
            StarsEarned = 0;
            Score = 0;
            RotationsUsed = 0;
            CompletionAccuracy = 0.0;
            SelectedPiece = null;

            CenterTargetShape();
            CalculateMinimumRotations();

            // Reset pieces into tray layout
            Pieces = TangramLevelCatalog.CreateStandardTans();
            ArrangePiecesInTray();

            NotifyStateChanged();
        }

        private void CenterTargetShape()
        {
            if (CurrentLevel == null || CurrentLevel.TargetTransforms.Count == 0) return;

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            var dummyPieces = TangramLevelCatalog.CreateStandardTans();

            foreach (var target in CurrentLevel.TargetTransforms)
            {
                var dummyPiece = dummyPieces.FirstOrDefault(p => p.Type == target.PieceType);
                if (dummyPiece != null)
                {
                    var vertices = dummyPiece.GetTransformedVertices(target.X, target.Y, target.RotationAngle, target.IsFlipped);
                    foreach (var v in vertices)
                    {
                        if (v.X < minX) minX = v.X;
                        if (v.X > maxX) maxX = v.X;
                        if (v.Y < minY) minY = v.Y;
                        if (v.Y > maxY) maxY = v.Y;
                    }
                }
            }

            double targetCenterX = (minX + maxX) / 2.0;
            double targetCenterY = (minY + maxY) / 2.0;

            // Center of the play area (play area is 850x750, tray is 250 wide on the right, total SVG 1100x750)
            double desiredCX = 425.0;
            double desiredCY = 375.0;

            double offsetX = desiredCX - targetCenterX;
            double offsetY = desiredCY - targetCenterY;

            foreach (var target in CurrentLevel.TargetTransforms)
            {
                target.X += offsetX;
                target.Y += offsetY;
            }
        }

        private void CalculateMinimumRotations()
        {
            if (CurrentLevel == null) return;
            int totalMin = 0;
            foreach (var target in CurrentLevel.TargetTransforms)
            {
                int mod = 360;
                if (target.PieceType == TangramPieceType.Square) mod = 90;
                if (target.PieceType == TangramPieceType.Parallelogram) mod = 180;
                
                int diff = Math.Abs(target.RotationAngle) % mod;
                int steps1 = diff / 45;
                int steps2 = (mod - diff) / 45;
                
                int rotCost = Math.Min(steps1, steps2);
                int flipCost = (target.PieceType == TangramPieceType.Parallelogram && target.IsFlipped) ? 1 : 0;
                
                totalMin += rotCost + flipCost;
            }
            MinimumRotations = totalMin;
        }

        public void ArrangePiecesInTray()
        {
            double trayX = 975;
            double startY = 100;
            double spacingY = 90;

            for (int i = 0; i < Pieces.Count; i++)
            {
                Pieces[i].IsInTray = true;
                Pieces[i].IsSelected = false;
                Pieces[i].X = trayX;
                Pieces[i].Y = startY + i * spacingY;
                Pieces[i].RotationAngle = 0;
                Pieces[i].IsFlipped = false;
            }
        }

        public void SelectPiece(string pieceId)
        {
            foreach (var p in Pieces)
            {
                p.IsSelected = (p.Id == pieceId);
                if (p.IsSelected)
                {
                    SelectedPiece = p;
                }
            }
            NotifyStateChanged();
        }

        public void ClearSelection()
        {
            foreach (var p in Pieces)
            {
                p.IsSelected = false;
            }
            SelectedPiece = null;
            NotifyStateChanged();
        }

        public async Task RotateSelectedPieceAsync(int angleDelta)
        {
            if (SelectedPiece == null) return;

            SelectedPiece.RotationAngle = (SelectedPiece.RotationAngle + angleDelta + 360) % 360;
            RotationsUsed++;
            await CheckCompletionAsync();
            NotifyStateChanged();
        }

        public async Task FlipSelectedPieceAsync()
        {
            if (SelectedPiece == null) return;
            if (SelectedPiece.Type == TangramPieceType.Parallelogram)
            {
                SelectedPiece.IsFlipped = !SelectedPiece.IsFlipped;
                RotationsUsed++;
                await CheckCompletionAsync();
                NotifyStateChanged();
            }
        }

        public void MovePiece(string pieceId, double targetX, double targetY, bool snapToGrid = true)
        {
            var piece = Pieces.FirstOrDefault(p => p.Id == pieceId);
            if (piece == null) return;

            piece.IsInTray = false;
            piece.X = targetX;
            piece.Y = targetY;

            if (snapToGrid)
            {
                // Grid snapping in 12.5 unit steps
                double step = 12.5;
                piece.X = Math.Round(piece.X / step) * step;
                piece.Y = Math.Round(piece.Y / step) * step;

                PerformVertexSnapping(piece);
            }

            NotifyStateChanged();
        }

        private void PerformVertexSnapping(TangramPiece draggedPiece)
        {
            var attractors = new List<Point2D>();
            
            if (CurrentLevel != null && Mode != TangramGameMode.Sandbox)
            {
                foreach (var t in CurrentLevel.TargetTransforms)
                {
                    var dummyPiece = Pieces.FirstOrDefault(p => p.Type == t.PieceType);
                    if (dummyPiece != null)
                    {
                        var verts = dummyPiece.GetTransformedVertices(t.X, t.Y, t.RotationAngle, t.IsFlipped);
                        attractors.AddRange(verts);
                    }
                }
            }

            foreach (var p in Pieces)
            {
                if (p == draggedPiece || p.IsInTray) continue;
                attractors.AddRange(p.GetTransformedVertices());
            }

            if (attractors.Count == 0) return;

            var draggedVerts = draggedPiece.GetTransformedVertices();
            double minDistance = double.MaxValue;
            Point2D? bestDraggedVert = null;
            Point2D? bestAttractor = null;

            foreach (var dVert in draggedVerts)
            {
                foreach (var aVert in attractors)
                {
                    double dist = Math.Sqrt(Math.Pow(dVert.X - aVert.X, 2) + Math.Pow(dVert.Y - aVert.Y, 2));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestDraggedVert = dVert;
                        bestAttractor = aVert;
                    }
                }
            }

            double threshold = 25.0; // Snapping distance
            if (minDistance <= threshold && bestDraggedVert != null && bestAttractor != null)
            {
                draggedPiece.X += (bestAttractor.X - bestDraggedVert.X);
                draggedPiece.Y += (bestAttractor.Y - bestDraggedVert.Y);
            }
        }

        public async Task ReturnPieceToTrayAsync(string pieceId)
        {
            var piece = Pieces.FirstOrDefault(p => p.Id == pieceId);
            if (piece == null) return;

            int index = Pieces.IndexOf(piece);
            piece.IsInTray = true;
            piece.X = 975;
            piece.Y = 100 + index * 90;
            piece.RotationAngle = 0;
            piece.IsFlipped = false;

            await CheckCompletionAsync();
            NotifyStateChanged();
        }



        public void ResetCurrentLevel()
        {
            LoadLevel(CurrentLevelIndex);
        }

        public async Task<bool> ProvideHintAsync()
        {
            // Canvas rasterization doesn't easily support blueprint hints.
            // For now, hint just flashes a random piece to help.
            return false;
        }

        public async Task CheckCompletionAsync()
        {
            if (Mode == TangramGameMode.Sandbox || CurrentLevel == null || _js == null)
            {
                IsCompleted = false;
                return;
            }

            if (Pieces.Any(p => p.IsInTray))
            {
                IsCompleted = false;
                return;
            }

            // Gather target polygons
            var targetPolys = new List<object>();
            foreach (var t in CurrentLevel.TargetTransforms)
            {
                var dummyPiece = Pieces.FirstOrDefault(p => p.Type == t.PieceType);
                if (dummyPiece != null)
                {
                    var verts = dummyPiece.GetTransformedVertices(t.X, t.Y, t.RotationAngle, t.IsFlipped);
                    targetPolys.Add(verts.Select(v => new { x = v.X, y = v.Y }).ToArray());
                }
            }

            // Gather piece polygons
            var piecePolys = new List<object>();
            foreach (var p in Pieces)
            {
                var verts = p.GetTransformedVertices();
                piecePolys.Add(verts.Select(v => new { x = v.X, y = v.Y }).ToArray());
            }

            bool isSolved = false;
            try
            {
                isSolved = await _js.InvokeAsync<bool>("tangramInterop.checkSolution", targetPolys, piecePolys);
            }
            catch
            {
                isSolved = false;
            }

            if (isSolved)
            {
                IsCompleted = true;
                CompletionAccuracy = 100.0;
                CalculateStarsAndScore();

                var origLevel = Levels[CurrentLevelIndex];
                if (origLevel.Stars < StarsEarned)
                {
                    origLevel.Stars = StarsEarned;
                    CurrentLevel.Stars = StarsEarned;
                }
                if (origLevel.BestTimeSeconds == 0 || ElapsedSeconds < origLevel.BestTimeSeconds)
                {
                    origLevel.BestTimeSeconds = ElapsedSeconds;
                    CurrentLevel.BestTimeSeconds = ElapsedSeconds;
                }
            }
            else
            {
                IsCompleted = false;
                CompletionAccuracy = 0; // Partial accuracy isn't calculated with this method
            }
            
            NotifyStateChanged();
        }

        private void CalculateStarsAndScore()
        {
            int baseScore = 5000;
            int timeBonus = Math.Max(0, 3000 - (ElapsedSeconds * 20));
            int extraRotations = Math.Max(0, RotationsUsed - MinimumRotations);
            int rotationBonus = Math.Max(0, 2000 - (extraRotations * 50));
            
            if (HintsUsed > 0) baseScore -= 1000;
            
            Score = baseScore + timeBonus + rotationBonus;

            if (HintsUsed == 0 && ElapsedSeconds <= 60 && extraRotations <= 2)
            {
                StarsEarned = 3;
            }
            else if (HintsUsed <= 1 && ElapsedSeconds <= 120 && extraRotations <= 8)
            {
                StarsEarned = 2;
            }
            else
            {
                StarsEarned = 1;
            }
        }

        public void SaveCustomSandboxLevel(string name, string category)
        {
            var newLevel = new TangramLevel
            {
                Id = Levels.Count + 1,
                Name = string.IsNullOrWhiteSpace(name) ? $"Custom Shape #{Levels.Count + 1}" : name,
                Category = string.IsNullOrWhiteSpace(category) ? "Custom" : category,
                Difficulty = "Medium",
                IsCustom = true,
                TargetTransforms = Pieces.Select(p => p.ToTransform()).ToList()
            };

            Levels.Add(newLevel);
            LoadLevel(Levels.Count - 1);
        }

        public void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}
