using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorArcade.Models
{
    public enum TangramPieceType
    {
        LargeTriangle1,
        LargeTriangle2,
        MediumTriangle,
        SmallTriangle1,
        SmallTriangle2,
        Square,
        Parallelogram
    }

    public enum TangramGameMode
    {
        Silhouette,  // Dark target silhouette
        Guided,      // Outlines of target pieces visible
        ColorMatch,  // Color matched targets
        Sandbox      // Freeform level creation
    }

    public enum TangramTheme
    {
        Neon,
        Wood,
        Zen
    }

    public class Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class TangramPieceTransform
    {
        public TangramPieceType PieceType { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int RotationAngle { get; set; } // 0, 45, 90, 135, 180, 225, 270, 315
        public bool IsFlipped { get; set; }

        public TangramPieceTransform Clone()
        {
            return new TangramPieceTransform
            {
                PieceType = PieceType,
                X = X,
                Y = Y,
                RotationAngle = RotationAngle,
                IsFlipped = IsFlipped
            };
        }
    }

    public class TangramPiece
    {
        public string Id { get; set; } = string.Empty;
        public TangramPieceType Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public int RotationAngle { get; set; } // 0 to 315 step 45
        public bool IsFlipped { get; set; }
        public string Color { get; set; } = "#FFFFFF";
        public string Name { get; set; } = string.Empty;

        public bool IsInTray { get; set; } = true;
        public bool IsSelected { get; set; } = false;

        public Point2D[] LocalVertices { get; set; } = Array.Empty<Point2D>();

        public TangramPiece(TangramPieceType type, string id, string color, string name, Point2D[] localVertices)
        {
            Type = type;
            Id = id;
            Color = color;
            Name = name;
            LocalVertices = localVertices;
        }

        public Point2D[] GetTransformedVertices(double customX = double.NaN, double customY = double.NaN, int? customRot = null, bool? customFlip = null)
        {
            double posX = double.IsNaN(customX) ? X : customX;
            double posY = double.IsNaN(customY) ? Y : customY;
            int rot = customRot ?? RotationAngle;
            bool flip = customFlip ?? IsFlipped;

            double rad = rot * Math.PI / 180.0;
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            Point2D[] result = new Point2D[LocalVertices.Length];
            for (int i = 0; i < LocalVertices.Length; i++)
            {
                double lx = LocalVertices[i].X;
                double ly = LocalVertices[i].Y;

                // Parallelogram horizontal flip relative to centroid
                if (flip && Type == TangramPieceType.Parallelogram)
                {
                    lx = -lx;
                }

                // Rotate
                double rx = lx * cos - ly * sin;
                double ry = lx * sin + ly * cos;

                // Translate
                result[i] = new Point2D(rx + posX, ry + posY);
            }
            return result;
        }

        public string GetSvgPolygonPoints(double customX = double.NaN, double customY = double.NaN, int? customRot = null, bool? customFlip = null)
        {
            var verts = GetTransformedVertices(customX, customY, customRot, customFlip);
            return string.Join(" ", verts.Select(v => $"{v.X:F2},{v.Y:F2}"));
        }

        public Point2D GetCentroid()
        {
            return new Point2D(X, Y);
        }

        public TangramPieceTransform ToTransform()
        {
            return new TangramPieceTransform
            {
                PieceType = Type,
                X = X,
                Y = Y,
                RotationAngle = RotationAngle,
                IsFlipped = IsFlipped
            };
        }
    }

    public class TangramLevel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Difficulty { get; set; } = "Medium";
        public List<TangramPieceTransform> TargetTransforms { get; set; } = new List<TangramPieceTransform>();
        public bool IsCustom { get; set; } = false;
        public int BestTimeSeconds { get; set; } = 0;
        public int Stars { get; set; } = 0;
    }

    /// <summary>
    /// Separating Axis Theorem (SAT) geometry engine for strict non-overlapping verification.
    /// </summary>
    public static class TangramGeometryUtils
    {
        public static bool DoPolygonsOverlap(Point2D[] polyA, Point2D[] polyB, double touchTolerance = 2.0)
        {
            if (HasSeparatingAxis(polyA, polyB, touchTolerance)) return false;
            if (HasSeparatingAxis(polyB, polyA, touchTolerance)) return false;
            return true;
        }

        private static bool HasSeparatingAxis(Point2D[] polyA, Point2D[] polyB, double touchTolerance)
        {
            for (int i = 0; i < polyA.Length; i++)
            {
                Point2D p1 = polyA[i];
                Point2D p2 = polyA[(i + 1) % polyA.Length];

                double edgeX = p2.X - p1.X;
                double edgeY = p2.Y - p1.Y;
                double normalX = -edgeY;
                double normalY = edgeX;

                double len = Math.Sqrt(normalX * normalX + normalY * normalY);
                if (len < 0.0001) continue;
                normalX /= len;
                normalY /= len;

                double minA = double.MaxValue, maxA = double.MinValue;
                foreach (var v in polyA)
                {
                    double proj = v.X * normalX + v.Y * normalY;
                    if (proj < minA) minA = proj;
                    if (proj > maxA) maxA = proj;
                }

                double minB = double.MaxValue, maxB = double.MinValue;
                foreach (var v in polyB)
                {
                    double proj = v.X * normalX + v.Y * normalY;
                    if (proj < minB) minB = proj;
                    if (proj > maxB) maxB = proj;
                }

                double overlap = Math.Min(maxA, maxB) - Math.Max(minA, minB);

                if (overlap <= touchTolerance)
                {
                    return true; // Found a separating axis!
                }
            }

            return false;
        }
    }
}
