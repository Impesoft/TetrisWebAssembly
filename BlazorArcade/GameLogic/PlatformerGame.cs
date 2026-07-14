using System;
using System.Collections.Generic;

namespace BlazorArcade.GameLogic
{
    public class Rect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public Rect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Intersects(Rect other)
        {
            return X < other.X + other.Width &&
                   X + Width > other.X &&
                   Y < other.Y + other.Height &&
                   Y + Height > other.Y;
        }
    }

    public class PlatformerPlayer : Rect
    {
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public bool IsGrounded { get; set; }

        public PlatformerPlayer(double x, double y, double width, double height) : base(x, y, width, height)
        {
            VelocityX = 0;
            VelocityY = 0;
            IsGrounded = false;
        }
    }

    public class PlatformerGame
    {
        public double Width { get; private set; } = 800;
        public double Height { get; private set; } = 600;

        public PlatformerPlayer Player { get; private set; }
        public List<Rect> Platforms { get; private set; }
        public Rect Goal { get; private set; }

        public bool IsGameOver { get; private set; }
        public bool IsGameWon { get; private set; }

        // Physics constants (tuned for ~60fps)
        private const double Gravity = 0.5;
        private const double JumpForce = -12.0;
        private const double MoveSpeed = 1.0;
        private const double MaxSpeed = 6.0;
        private const double Friction = 0.8; // Multiplier

        public PlatformerGame()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            IsGameOver = false;
            IsGameWon = false;

            // Player is 30x30
            Player = new PlatformerPlayer(50, Height - 100, 30, 30);

            // Goal is 40x60
            Goal = new Rect(700, 50, 40, 60);

            Platforms = new List<Rect>();

            // Ground
            Platforms.Add(new Rect(0, Height - 40, Width, 40));

            // Platforms
            Platforms.Add(new Rect(150, Height - 120, 100, 20));
            Platforms.Add(new Rect(300, Height - 200, 100, 20));
            Platforms.Add(new Rect(150, Height - 280, 100, 20));
            Platforms.Add(new Rect(400, Height - 360, 150, 20));
            Platforms.Add(new Rect(650, Height - 420, 150, 20)); // Under the goal
            Platforms.Add(new Rect(650, 110, 150, 20)); // Platform the goal is on
            
            // A wall just for fun
            Platforms.Add(new Rect(400, Height - 200, 20, 160));
        }

        public void Update(bool moveLeft, bool moveRight, bool jump)
        {
            if (IsGameOver || IsGameWon) return;

            // Apply horizontal movement
            if (moveLeft)
            {
                Player.VelocityX -= MoveSpeed;
            }
            if (moveRight)
            {
                Player.VelocityX += MoveSpeed;
            }

            // Apply friction
            if (!moveLeft && !moveRight)
            {
                Player.VelocityX *= Friction;
                if (Math.Abs(Player.VelocityX) < 0.1) Player.VelocityX = 0;
            }

            // Cap horizontal speed
            if (Player.VelocityX > MaxSpeed) Player.VelocityX = MaxSpeed;
            if (Player.VelocityX < -MaxSpeed) Player.VelocityX = -MaxSpeed;

            // Jump
            if (jump && Player.IsGrounded)
            {
                Player.VelocityY = JumpForce;
                Player.IsGrounded = false;
            }

            // Apply gravity
            Player.VelocityY += Gravity;

            // Cap fall speed
            if (Player.VelocityY > 15) Player.VelocityY = 15;

            // Move X and resolve collisions
            Player.X += Player.VelocityX;
            ResolveCollisionsX();

            // Move Y and resolve collisions
            Player.Y += Player.VelocityY;
            Player.IsGrounded = false; // Reset before checking
            ResolveCollisionsY();

            // Check boundaries
            if (Player.X < 0)
            {
                Player.X = 0;
                Player.VelocityX = 0;
            }
            if (Player.X + Player.Width > Width)
            {
                Player.X = Width - Player.Width;
                Player.VelocityX = 0;
            }

            // If player falls off the bottom of the screen (shouldn't happen with ground, but just in case)
            if (Player.Y > Height)
            {
                IsGameOver = true;
            }

            // Check win condition
            if (Player.Intersects(Goal))
            {
                IsGameWon = true;
            }
        }

        private void ResolveCollisionsX()
        {
            foreach (var platform in Platforms)
            {
                if (Player.Intersects(platform))
                {
                    if (Player.VelocityX > 0) // Moving right
                    {
                        Player.X = platform.X - Player.Width;
                        Player.VelocityX = 0;
                    }
                    else if (Player.VelocityX < 0) // Moving left
                    {
                        Player.X = platform.X + platform.Width;
                        Player.VelocityX = 0;
                    }
                }
            }
        }

        private void ResolveCollisionsY()
        {
            foreach (var platform in Platforms)
            {
                if (Player.Intersects(platform))
                {
                    if (Player.VelocityY > 0) // Falling down
                    {
                        Player.Y = platform.Y - Player.Height;
                        Player.VelocityY = 0;
                        Player.IsGrounded = true;
                    }
                    else if (Player.VelocityY < 0) // Jumping up
                    {
                        Player.Y = platform.Y + platform.Height;
                        Player.VelocityY = 0;
                    }
                }
            }
        }
    }
}
