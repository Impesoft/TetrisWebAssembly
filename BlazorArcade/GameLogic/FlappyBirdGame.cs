using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorArcade.GameLogic
{
    public class FBBird
    {
        public double Y { get; set; }
        public double VelocityY { get; set; }
        public double Radius { get; set; } = 15;
        public double X { get; set; } = 200; // Fixed X position
    }

    public class FBPipe
    {
        public double X { get; set; }
        public double Width { get; set; } = 70;
        public double GapY { get; set; } // Center of the gap
        public double GapHeight { get; set; } = 160;
        public bool Passed { get; set; } = false;
    }

    public class FlappyBirdGame
    {
        public FBBird Bird { get; private set; }
        public List<FBPipe> Pipes { get; private set; }
        public int Score { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsGameStarted { get; private set; }
        
        public double Width { get; private set; } = 800;
        public double Height { get; private set; } = 600;

        private double Gravity = 1600;
        private double FlapStrength = -550;
        private double PipeSpeed = 250;
        private double PipeSpawnDistance = 350;

        private Random _random = new Random();

        public FlappyBirdGame()
        {
            Reset();
        }

        public void Reset()
        {
            Bird = new FBBird { Y = Height / 2, VelocityY = 0 };
            Pipes = new List<FBPipe>();
            Score = 0;
            IsGameOver = false;
            IsGameStarted = false;
            
            // Spawn initial pipes
            SpawnPipe(Width + 200);
            SpawnPipe(Width + 200 + PipeSpawnDistance);
        }

        private void SpawnPipe(double x)
        {
            double minGapY = 150;
            double maxGapY = Height - 150;
            double gapY = _random.NextDouble() * (maxGapY - minGapY) + minGapY;

            Pipes.Add(new FBPipe
            {
                X = x,
                GapY = gapY
            });
        }

        public void Flap()
        {
            if (IsGameOver) return;
            if (!IsGameStarted) IsGameStarted = true;
            Bird.VelocityY = FlapStrength;
        }

        public void Update(double dt)
        {
            if (IsGameOver || !IsGameStarted) return;

            // Apply gravity
            Bird.VelocityY += Gravity * dt;
            Bird.Y += Bird.VelocityY * dt;

            // Move pipes
            foreach (var pipe in Pipes)
            {
                pipe.X -= PipeSpeed * dt;

                // Score check
                if (!pipe.Passed && pipe.X + pipe.Width < Bird.X - Bird.Radius)
                {
                    pipe.Passed = true;
                    Score++;
                }
            }

            // Spawn new pipes
            var lastPipe = Pipes.LastOrDefault();
            if (lastPipe != null && lastPipe.X < Width - PipeSpawnDistance)
            {
                SpawnPipe(lastPipe.X + PipeSpawnDistance);
            }

            // Remove off-screen pipes
            Pipes.RemoveAll(p => p.X + p.Width < 0);

            // Check Collisions
            CheckCollisions();
        }

        private void CheckCollisions()
        {
            // Ground / Ceiling collision
            if (Bird.Y + Bird.Radius >= Height)
            {
                Bird.Y = Height - Bird.Radius;
                IsGameOver = true;
            }
            if (Bird.Y - Bird.Radius <= 0)
            {
                Bird.Y = Bird.Radius;
                IsGameOver = true;
            }

            // Pipe collisions
            // Treat bird as a circle, pipes as rectangles
            double bx = Bird.X;
            double by = Bird.Y;
            double br = Bird.Radius - 2; // slightly forgiving hitbox

            foreach (var pipe in Pipes)
            {
                // Top pipe rectangle
                double topRectX = pipe.X;
                double topRectY = 0;
                double topRectWidth = pipe.Width;
                double topRectHeight = pipe.GapY - (pipe.GapHeight / 2);

                if (CircleRectIntersect(bx, by, br, topRectX, topRectY, topRectWidth, topRectHeight))
                {
                    IsGameOver = true;
                    return;
                }

                // Bottom pipe rectangle
                double bottomRectX = pipe.X;
                double bottomRectY = pipe.GapY + (pipe.GapHeight / 2);
                double bottomRectWidth = pipe.Width;
                double bottomRectHeight = Height - bottomRectY;

                if (CircleRectIntersect(bx, by, br, bottomRectX, bottomRectY, bottomRectWidth, bottomRectHeight))
                {
                    IsGameOver = true;
                    return;
                }
            }
        }

        private bool CircleRectIntersect(double cx, double cy, double cr, double rx, double ry, double rw, double rh)
        {
            // Find the closest point to the circle within the rectangle
            double closestX = Math.Max(rx, Math.Min(cx, rx + rw));
            double closestY = Math.Max(ry, Math.Min(cy, ry + rh));

            // Calculate the distance between the circle's center and this closest point
            double distanceX = cx - closestX;
            double distanceY = cy - closestY;

            // If the distance is less than the circle's radius, an intersection occurs
            return (distanceX * distanceX + distanceY * distanceY) < (cr * cr);
        }
    }
}
