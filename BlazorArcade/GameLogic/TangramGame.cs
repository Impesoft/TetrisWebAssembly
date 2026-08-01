using System;
using System.Collections.Generic;
using System.Linq;
using BlazorArcade.Helpers;
using BlazorArcade.Models;

namespace BlazorArcade.GameLogic
{
    public class TangramGame
    {
        public List<TangramLevel> Levels { get; private set; } = new List<TangramLevel>();
        public TangramLevel CurrentLevel { get; private set; } = null!;
        public int CurrentLevelIndex { get; private set; } = 0;

        public List<TangramPiece> Pieces { get; private set; } = new List<TangramPiece>();
        public TangramPiece? SelectedPiece { get; private set; }

        public TangramGameMode Mode { get; set; } = TangramGameMode.Silhouette;
        public TangramTheme Theme { get; set; } = TangramTheme.Neon;

        public bool IsCompleted { get; private set; } = false;
        public int ElapsedSeconds { get; set; } = 0;
        public int HintsUsed { get; set; } = 0;
        public int StarsEarned { get; set; } = 0;
        public double CompletionAccuracy { get; private set; } = 0.0;

        public string SelectedCategory { get; set; } = "All";

        public event Action? OnStateChanged;

        public TangramGame()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            Levels = TangramLevelCatalog.GetLevels();
            LoadLevel(0);
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
            CompletionAccuracy = 0.0;
            SelectedPiece = null;

            CenterTargetShape();

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

        public void RotateSelectedPiece(int angleDelta)
        {
            if (SelectedPiece == null) return;

            SelectedPiece.RotationAngle = (SelectedPiece.RotationAngle + angleDelta + 360) % 360;
            CheckCompletion();
            NotifyStateChanged();
        }

        public void FlipSelectedPiece()
        {
            if (SelectedPiece == null) return;
            if (SelectedPiece.Type == TangramPieceType.Parallelogram)
            {
                SelectedPiece.IsFlipped = !SelectedPiece.IsFlipped;
                CheckCompletion();
                NotifyStateChanged();
            }
        }

        public void MovePiece(string pieceId, double targetX, double targetY, bool snapToGrid = true)
        {
            var piece = Pieces.FirstOrDefault(p => p.Id == pieceId);
            if (piece == null) return;

            piece.IsInTray = false;

            if (snapToGrid)
            {
                // Grid snapping in 12.5 unit steps
                double step = 12.5;
                targetX = Math.Round(targetX / step) * step;
                targetY = Math.Round(targetY / step) * step;

                // Magnetic vertex snapping against target transforms
                targetX = PerformMagneticSnappingX(piece, targetX, targetY);
                targetY = PerformMagneticSnappingY(piece, targetX, targetY);
            }

            piece.X = targetX;
            piece.Y = targetY;

            CheckCompletion();
            NotifyStateChanged();
        }

        private double PerformMagneticSnappingX(TangramPiece piece, double posX, double posY)
        {
            double threshold = 15.0;
            // Check against current level target positions of same type
            var matchingTargets = CurrentLevel.TargetTransforms.Where(t => IsCompatiblePieceType(piece.Type, t.PieceType));
            foreach (var target in matchingTargets)
            {
                if (Math.Abs(posX - target.X) <= threshold)
                {
                    return target.X;
                }
            }
            return posX;
        }

        private double PerformMagneticSnappingY(TangramPiece piece, double posX, double posY)
        {
            double threshold = 15.0;
            var matchingTargets = CurrentLevel.TargetTransforms.Where(t => IsCompatiblePieceType(piece.Type, t.PieceType));
            foreach (var target in matchingTargets)
            {
                if (Math.Abs(posY - target.Y) <= threshold)
                {
                    return target.Y;
                }
            }
            return posY;
        }

        public void ReturnPieceToTray(string pieceId)
        {
            var piece = Pieces.FirstOrDefault(p => p.Id == pieceId);
            if (piece == null) return;

            int index = Pieces.IndexOf(piece);
            piece.IsInTray = true;
            piece.X = 975;
            piece.Y = 100 + index * 90;
            piece.RotationAngle = 0;
            piece.IsFlipped = false;

            CheckCompletion();
            NotifyStateChanged();
        }

        public void ResetCurrentLevel()
        {
            LoadLevel(CurrentLevelIndex);
        }

        public bool ProvideHint()
        {
            if (IsCompleted) return false;

            HintsUsed++;

            // Find first piece not properly placed
            var targetList = CurrentLevel.TargetTransforms.ToList();
            var usedTargets = new HashSet<TangramPieceTransform>();

            TangramPiece? pieceToFix = null;
            TangramPieceTransform? bestTarget = null;

            foreach (var piece in Pieces)
            {
                if (piece.IsInTray || !IsPieceInTargetPosition(piece, targetList, usedTargets, out var targetMatch))
                {
                    pieceToFix = piece;
                    // Find an unassigned matching target for this piece type
                    bestTarget = targetList.FirstOrDefault(t => !usedTargets.Contains(t) && IsCompatiblePieceType(piece.Type, t.PieceType));
                    break;
                }
            }

            if (pieceToFix != null && bestTarget != null)
            {
                pieceToFix.IsInTray = false;
                pieceToFix.X = bestTarget.X;
                pieceToFix.Y = bestTarget.Y;
                pieceToFix.RotationAngle = bestTarget.RotationAngle;
                pieceToFix.IsFlipped = bestTarget.IsFlipped;

                SelectedPiece = pieceToFix;
                foreach (var p in Pieces) p.IsSelected = (p == pieceToFix);

                CheckCompletion();
                NotifyStateChanged();
                return true;
            }

            return false;
        }

        public void CheckCompletion()
        {
            if (Mode == TangramGameMode.Sandbox)
            {
                IsCompleted = false;
                return;
            }

            var targets = CurrentLevel.TargetTransforms.ToList();
            var usedTargets = new HashSet<TangramPieceTransform>();

            int correctCount = 0;

            foreach (var piece in Pieces)
            {
                if (!piece.IsInTray && IsPieceInTargetPosition(piece, targets, usedTargets, out var match))
                {
                    correctCount++;
                    if (match != null) usedTargets.Add(match);
                }
            }

            CompletionAccuracy = (double)correctCount / Pieces.Count * 100.0;

            if (correctCount == Pieces.Count)
            {
                IsCompleted = true;
                CalculateStars();

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
            }
        }

        private bool IsPieceInTargetPosition(TangramPiece piece, List<TangramPieceTransform> targets, HashSet<TangramPieceTransform> usedTargets, out TangramPieceTransform? matchedTarget)
        {
            matchedTarget = null;
            double posTolerance = 25.0; // Distance tolerance

            foreach (var target in targets)
            {
                if (usedTargets.Contains(target)) continue;
                if (!IsCompatiblePieceType(piece.Type, target.PieceType)) continue;

                double dx = piece.X - target.X;
                double dy = piece.Y - target.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist <= posTolerance)
                {
                    // Check angle compatibility considering geometric symmetry
                    if (IsAngleCompatible(piece.Type, piece.RotationAngle, piece.IsFlipped, target.RotationAngle, target.IsFlipped))
                    {
                        matchedTarget = target;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsCompatiblePieceType(TangramPieceType type1, TangramPieceType type2)
        {
            if (type1 == type2) return true;
            if ((type1 == TangramPieceType.LargeTriangle1 && type2 == TangramPieceType.LargeTriangle2) ||
                (type1 == TangramPieceType.LargeTriangle2 && type2 == TangramPieceType.LargeTriangle1)) return true;
            if ((type1 == TangramPieceType.SmallTriangle1 && type2 == TangramPieceType.SmallTriangle2) ||
                (type1 == TangramPieceType.SmallTriangle2 && type2 == TangramPieceType.SmallTriangle1)) return true;
            return false;
        }

        private bool IsAngleCompatible(TangramPieceType type, int angle1, bool flip1, int angle2, bool flip2)
        {
            angle1 = (angle1 % 360 + 360) % 360;
            angle2 = (angle2 % 360 + 360) % 360;

            if (type == TangramPieceType.Square)
            {
                // Square symmetry under 90 deg rotation
                return (angle1 % 90) == (angle2 % 90);
            }

            if (type == TangramPieceType.Parallelogram)
            {
                // Parallelogram requires matching flip state or 180 symmetry
                if (flip1 != flip2) return false;
                return (angle1 % 180) == (angle2 % 180);
            }

            // Triangles have 360 unique orientation except right angle alignment
            return angle1 == angle2;
        }

        private void CalculateStars()
        {
            if (HintsUsed == 0 && ElapsedSeconds <= 90)
            {
                StarsEarned = 3;
            }
            else if (HintsUsed <= 1 && ElapsedSeconds <= 180)
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
