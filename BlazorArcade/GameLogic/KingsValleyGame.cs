using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorArcade.GameLogic
{
    public class KVPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public KVPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class KVItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 24;
        public double Height { get; set; } = 24;
        public string Type { get; set; } = "Gem"; // "Gem", "Pickaxe", "Knife"
        public string Color { get; set; } = "#ffd700";
        public bool IsCollected { get; set; }
    }

    public class KVScorePopup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public int Points { get; set; }
        public double LifeTimer { get; set; } = 1.0;
    }

    public class KVProjectile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 16;
        public double Height { get; set; } = 8;
        public int Direction { get; set; } = 1; // 1 = right, -1 = left
        public double Speed { get; set; } = 7.0;
        public bool IsActive { get; set; } = true;
    }

    public class KVMummy
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 26;
        public double Height { get; set; } = 32;
        public double Speed { get; set; } = 1.4;
        public int Direction { get; set; } = 1;
        public string Color { get; set; } = "#ffffff"; // White (normal), Red (fast), Yellow (jumper)
        public bool IsStunned { get; set; }
        public double StunTimer { get; set; } = 0;
    }

    public class KVPlayer
    {
        public double X { get; set; } = 80;
        public double Y { get; set; } = 480;
        public double Width { get; set; } = 26;
        public double Height { get; set; } = 32;
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }

        public bool IsGrounded { get; set; }
        public bool IsClimbingLadder { get; set; }
        public bool FacingLeft { get; set; }
        public bool IsDead { get; set; }

        public bool HasPickaxe { get; set; }
        public int KnivesCount { get; set; } = 0;
        public bool IsDigging { get; set; }
        public double DigTimer { get; set; } = 0;

        public void Reset(double startX = 80, double startY = 480)
        {
            X = startX;
            Y = startY;
            VelocityX = 0;
            VelocityY = 0;
            IsGrounded = true;
            IsClimbingLadder = false;
            FacingLeft = false;
            IsDead = false;
            HasPickaxe = false;
            KnivesCount = 0;
            IsDigging = false;
            DigTimer = 0;
        }
    }

    public class KingsValleyGame
    {
        public double FieldWidth { get; private set; } = 800;
        public double FieldHeight { get; private set; } = 600;

        public const int Cols = 20;
        public const int Rows = 15;
        public const double TileSize = 40.0;

        // Tilemap Grid: 0 = Air, 1 = Solid Wall, 2 = Diggable Brick, 3 = Ladder, 4 = Stair Up-Left, 5 = Stair Up-Right, 9 = Exit Door
        public int[,] Grid { get; private set; } = new int[Rows, Cols];

        public KVPlayer Player { get; private set; } = new KVPlayer();
        public List<KVMummy> Mummies { get; private set; } = new List<KVMummy>();
        public List<KVItem> Items { get; private set; } = new List<KVItem>();
        public List<KVProjectile> Projectiles { get; private set; } = new List<KVProjectile>();
        public List<KVScorePopup> ScorePopups { get; private set; } = new List<KVScorePopup>();

        public KVPoint ExitDoorPos { get; private set; } = new KVPoint(640, 80);

        public int Score { get; private set; } = 0;
        public int HighScore { get; private set; } = 12500;
        public int Lives { get; private set; } = 3;
        public int Level { get; private set; } = 1;
        public int GemsRemaining { get; private set; } = 0;

        public bool IsExitUnlocked { get; private set; } = false;
        public bool IsGameOver { get; private set; }
        public bool IsGameWon { get; private set; }
        public bool IsPaused { get; private set; } = false;

        private Random _random = new Random();

        public KingsValleyGame()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            Score = 0;
            Lives = 3;
            Level = 1;
            IsGameOver = false;
            IsGameWon = false;
            IsPaused = false;
            LoadLevel(Level);
        }

        public void LoadLevel(int level)
        {
            Player.Reset(80, 508);
            Mummies.Clear();
            Items.Clear();
            Projectiles.Clear();
            ScorePopups.Clear();

            // Clear Grid
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    Grid[r, c] = 0;

            // Outer border walls
            for (int c = 0; c < Cols; c++)
            {
                Grid[0, c] = 1;        // Roof
                Grid[Rows - 1, c] = 1; // Floor
            }
            for (int r = 0; r < Rows; r++)
            {
                Grid[r, 0] = 1;        // Left wall
                Grid[r, Cols - 1] = 1; // Right wall
            }

            if (level == 1)
            {
                // Level 1: Entrance Tomb
                for (int c = 1; c <= 18; c++) Grid[11, c] = 1;
                Grid[11, 4] = 2; Grid[11, 14] = 2;

                for (int c = 1; c <= 18; c++) Grid[7, c] = 1;
                Grid[7, 6] = 2; Grid[7, 12] = 2;

                for (int c = 1; c <= 18; c++) Grid[3, c] = 1;

                // Continuous Ladders
                for (int r = 11; r <= 13; r++) Grid[r, 9] = 3;
                for (int r = 7; r <= 10; r++) Grid[r, 16] = 3;
                for (int r = 3; r <= 6; r++) Grid[r, 16] = 3;

                // Stairs
                Grid[10, 5] = 5; Grid[9, 6] = 5; Grid[8, 7] = 5;
                Grid[6, 13] = 4; Grid[5, 12] = 4; Grid[4, 11] = 4;

                ExitDoorPos = new KVPoint(15 * TileSize, 2 * TileSize - 8);
                Grid[2, 15] = 9;

                Items.Add(new KVItem { X = 2 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Gem", Color = "#ffd700" });
                Items.Add(new KVItem { X = 17 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Gem", Color = "#00f5d4" });
                Items.Add(new KVItem { X = 3 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Gem", Color = "#ff007f" });
                Items.Add(new KVItem { X = 10 * TileSize + 8, Y = 2 * TileSize + 8, Type = "Gem", Color = "#ffff00" });

                Items.Add(new KVItem { X = 8 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Pickaxe" });
                Items.Add(new KVItem { X = 14 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Knife" });

                Mummies.Add(new KVMummy { X = 13 * TileSize, Y = 10 * TileSize + 8, Color = "#ffffff", Speed = 1.3 });
                Mummies.Add(new KVMummy { X = 5 * TileSize, Y = 6 * TileSize + 8, Color = "#ffcc00", Speed = 1.5 });
            }
            else if (level == 2)
            {
                // Level 2: Deep Crypt
                for (int c = 1; c <= 18; c++) Grid[11, c] = 1;
                Grid[11, 3] = 2; Grid[11, 4] = 2; Grid[11, 15] = 2;

                for (int c = 1; c <= 18; c++) Grid[7, c] = 1;
                Grid[7, 5] = 2; Grid[7, 13] = 2;

                for (int c = 1; c <= 18; c++) Grid[3, c] = 1;

                for (int r = 11; r <= 13; r++) Grid[r, 10] = 3;
                for (int r = 7; r <= 10; r++) Grid[r, 3] = 3;
                for (int r = 3; r <= 6; r++) Grid[r, 17] = 3;

                Grid[10, 14] = 4; Grid[9, 13] = 4; Grid[8, 12] = 4;
                Grid[6, 7] = 5; Grid[5, 8] = 5; Grid[4, 9] = 5;

                ExitDoorPos = new KVPoint(10 * TileSize, 2 * TileSize - 8);
                Grid[2, 10] = 9;

                Items.Add(new KVItem { X = 2 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Gem", Color = "#ffd700" });
                Items.Add(new KVItem { X = 17 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Gem", Color = "#00f5d4" });
                Items.Add(new KVItem { X = 4 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Gem", Color = "#ff007f" });
                Items.Add(new KVItem { X = 14 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Gem", Color = "#00ffff" });
                Items.Add(new KVItem { X = 15 * TileSize + 8, Y = 2 * TileSize + 8, Type = "Gem", Color = "#ffd700" });

                Items.Add(new KVItem { X = 7 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Pickaxe" });
                Items.Add(new KVItem { X = 11 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Pickaxe" });
                Items.Add(new KVItem { X = 2 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Knife" });

                Mummies.Add(new KVMummy { X = 12 * TileSize, Y = 10 * TileSize + 8, Color = "#ffffff", Speed = 1.3 });
                Mummies.Add(new KVMummy { X = 15 * TileSize, Y = 6 * TileSize + 8, Color = "#ff3366", Speed = 1.8 });
                Mummies.Add(new KVMummy { X = 5 * TileSize, Y = 2 * TileSize + 8, Color = "#ffcc00", Speed = 1.5 });
            }
            else
            {
                // Level 3: Pharaoh's Vault
                for (int c = 1; c <= 18; c++) Grid[11, c] = 1;
                Grid[11, 5] = 2; Grid[11, 14] = 2;

                for (int c = 1; c <= 18; c++) Grid[7, c] = 1;
                Grid[7, 4] = 2; Grid[7, 14] = 2;

                for (int c = 1; c <= 18; c++) Grid[3, c] = 1;

                for (int r = 11; r <= 13; r++) Grid[r, 2] = 3;
                for (int r = 7; r <= 10; r++) Grid[r, 18] = 3;
                for (int r = 3; r <= 6; r++) Grid[r, 2] = 3;

                Grid[10, 4] = 5; Grid[9, 5] = 5; Grid[8, 6] = 5;
                Grid[6, 15] = 4; Grid[5, 14] = 4; Grid[4, 13] = 4;

                ExitDoorPos = new KVPoint(4 * TileSize, 2 * TileSize - 8);
                Grid[2, 4] = 9;

                Items.Add(new KVItem { X = 3 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Gem", Color = "#ffd700" });
                Items.Add(new KVItem { X = 16 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Gem", Color = "#00f5d4" });
                Items.Add(new KVItem { X = 6 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Gem", Color = "#ff007f" });
                Items.Add(new KVItem { X = 12 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Gem", Color = "#00ffff" });
                Items.Add(new KVItem { X = 8 * TileSize + 8, Y = 2 * TileSize + 8, Type = "Gem", Color = "#ffff00" });
                Items.Add(new KVItem { X = 17 * TileSize + 8, Y = 2 * TileSize + 8, Type = "Gem", Color = "#ffd700" });

                Items.Add(new KVItem { X = 9 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Pickaxe" });
                Items.Add(new KVItem { X = 8 * TileSize + 8, Y = 6 * TileSize + 8, Type = "Pickaxe" });
                Items.Add(new KVItem { X = 14 * TileSize + 8, Y = 10 * TileSize + 8, Type = "Knife" });

                Mummies.Add(new KVMummy { X = 7 * TileSize, Y = 10 * TileSize + 8, Color = "#ffffff", Speed = 1.3 });
                Mummies.Add(new KVMummy { X = 13 * TileSize, Y = 10 * TileSize + 8, Color = "#ff3366", Speed = 1.8 });
                Mummies.Add(new KVMummy { X = 10 * TileSize, Y = 6 * TileSize + 8, Color = "#ffcc00", Speed = 1.6 });
            }

            GemsRemaining = Items.Count(i => i.Type == "Gem");
            IsExitUnlocked = false;
        }

        public void RestartStage()
        {
            LoadLevel(Level);
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        public void DigBrick()
        {
            if (!Player.HasPickaxe || Player.IsDead || Player.IsDigging) return;

            int col = (int)((Player.X + Player.Width / 2) / TileSize);
            int row = (int)((Player.Y + Player.Height + 5) / TileSize);

            // Check brick underneath player or in front
            if (row >= 0 && row < Rows && col >= 0 && col < Cols)
            {
                if (Grid[row, col] == 2)
                {
                    Grid[row, col] = 0; // Dig out brick!
                    Player.IsDigging = true;
                    Player.DigTimer = 0.4;
                    AddScore(150, col * TileSize + 20, row * TileSize);
                    return;
                }
            }

            int frontCol = Player.FacingLeft ? (int)((Player.X - 5) / TileSize) : (int)((Player.X + Player.Width + 5) / TileSize);
            int playerRow = (int)((Player.Y + Player.Height / 2) / TileSize);

            if (playerRow >= 0 && playerRow < Rows && frontCol >= 0 && frontCol < Cols)
            {
                if (Grid[playerRow, frontCol] == 2)
                {
                    Grid[playerRow, frontCol] = 0;
                    Player.IsDigging = true;
                    Player.DigTimer = 0.4;
                    AddScore(150, frontCol * TileSize + 20, playerRow * TileSize);
                }
            }
        }

        public void ThrowKnife()
        {
            if (Player.KnivesCount <= 0 || Player.IsDead) return;

            Player.KnivesCount--;
            Projectiles.Add(new KVProjectile
            {
                X = Player.FacingLeft ? Player.X - 16 : Player.X + Player.Width,
                Y = Player.Y + 12,
                Direction = Player.FacingLeft ? -1 : 1
            });
        }

        public void Update(bool moveLeft, bool moveRight, bool moveUp, bool moveDown, bool jump, bool actionDig, bool actionThrow, double deltaTime = 0.016)
        {
            if (IsGameOver || IsGameWon || IsPaused) return;

            if (Player.IsDigging)
            {
                Player.DigTimer -= deltaTime;
                if (Player.DigTimer <= 0) Player.IsDigging = false;
            }

            if (actionDig) DigBrick();
            if (actionThrow) ThrowKnife();

            if (!Player.IsDead)
            {
                UpdatePlayer(moveLeft, moveRight, moveUp, moveDown, jump, deltaTime);
                CheckItemCollisions();
            }

            UpdateProjectiles(deltaTime);
            UpdateMummies(deltaTime);
            UpdatePopups(deltaTime);
            CheckMummyCollisions();

            // Check Exit Door win condition
            if (!Player.IsDead && IsExitUnlocked &&
                Math.Abs(Player.X - ExitDoorPos.X) < 28 &&
                Math.Abs(Player.Y - ExitDoorPos.Y) < 32)
            {
                OnLevelCleared();
            }
        }

        private void UpdatePlayer(bool moveLeft, bool moveRight, bool moveUp, bool moveDown, bool jump, double deltaTime)
        {
            const double moveSpeed = 3.2;
            const double climbSpeed = 2.4;
            const double gravity = 0.45;
            const double jumpForce = -7.2;

            double footX = Player.X + Player.Width / 2;
            double footY = Player.Y + Player.Height;

            int tileCol = (int)(footX / TileSize);
            int centerRow = (int)((Player.Y + Player.Height / 2) / TileSize);
            int footRow = (int)((footY - 2) / TileSize);

            int centerTile = (centerRow >= 0 && centerRow < Rows && tileCol >= 0 && tileCol < Cols) ? Grid[centerRow, tileCol] : 0;
            int footTile = (footRow >= 0 && footRow < Rows && tileCol >= 0 && tileCol < Cols) ? Grid[footRow, tileCol] : 0;

            bool onLadder = centerTile == 3 || footTile == 3;

            // Check Stair Tiles (Stair Up-Left 4, Stair Up-Right 5)
            bool onStairLeft = centerTile == 4 || footTile == 4;
            bool onStairRight = centerTile == 5 || footTile == 5;

            // Handle Ladder Mounting & Dismounting
            if (onLadder)
            {
                if (moveUp || (moveDown && Player.Y < footRow * TileSize))
                {
                    Player.IsClimbingLadder = true;
                    Player.IsGrounded = false;
                    Player.VelocityX = 0;
                }
            }

            if (Player.IsClimbingLadder)
            {
                Player.VelocityY = 0;

                if (moveUp)
                {
                    Player.Y -= climbSpeed;
                    int currentFootRow = (int)((Player.Y + Player.Height) / TileSize);
                    if (currentFootRow >= 0 && currentFootRow < Rows && tileCol >= 0 && tileCol < Cols)
                    {
                        if (Grid[currentFootRow, tileCol] == 1 || Grid[currentFootRow, tileCol] == 2)
                        {
                            Player.Y = currentFootRow * TileSize - Player.Height;
                            Player.IsClimbingLadder = false;
                            Player.IsGrounded = true;
                        }
                    }
                }
                else if (moveDown)
                {
                    Player.Y += climbSpeed;
                    int currentFootRow = (int)((Player.Y + Player.Height) / TileSize);
                    if (currentFootRow >= 0 && currentFootRow < Rows && tileCol >= 0 && tileCol < Cols)
                    {
                        if (Grid[currentFootRow, tileCol] == 1 || Grid[currentFootRow, tileCol] == 2)
                        {
                            Player.Y = currentFootRow * TileSize - Player.Height;
                            Player.IsClimbingLadder = false;
                            Player.IsGrounded = true;
                        }
                    }
                }

                if (!onLadder && (moveLeft || moveRight))
                {
                    Player.IsClimbingLadder = false;
                    Player.IsGrounded = true;
                }
            }
            else if (onStairLeft && (moveUp || moveDown))
            {
                // Stair Up-Left: Only climb diagonally when UP or DOWN is pressed!
                if (moveUp)
                {
                    Player.VelocityX = -moveSpeed;
                    Player.VelocityY = -climbSpeed;
                    Player.FacingLeft = true;
                    Player.IsGrounded = true;
                }
                else if (moveDown)
                {
                    Player.VelocityX = moveSpeed;
                    Player.VelocityY = climbSpeed;
                    Player.FacingLeft = false;
                    Player.IsGrounded = true;
                }
                Player.X += Player.VelocityX;
                Player.Y += Player.VelocityY;
            }
            else if (onStairRight && (moveUp || moveDown))
            {
                // Stair Up-Right: Only climb diagonally when UP or DOWN is pressed!
                if (moveUp)
                {
                    Player.VelocityX = moveSpeed;
                    Player.VelocityY = -climbSpeed;
                    Player.FacingLeft = false;
                    Player.IsGrounded = true;
                }
                else if (moveDown)
                {
                    Player.VelocityX = -moveSpeed;
                    Player.VelocityY = climbSpeed;
                    Player.FacingLeft = true;
                    Player.IsGrounded = true;
                }
                Player.X += Player.VelocityX;
                Player.Y += Player.VelocityY;
            }
            else
            {
                // Normal Horizontal Walking (Walks straight past stairs if Up/Down is not pressed)
                if (moveLeft)
                {
                    Player.VelocityX = -moveSpeed;
                    Player.FacingLeft = true;
                }
                else if (moveRight)
                {
                    Player.VelocityX = moveSpeed;
                    Player.FacingLeft = false;
                }
                else
                {
                    Player.VelocityX = 0;
                }

                // Jump
                if (jump && Player.IsGrounded && !Player.IsDigging)
                {
                    Player.VelocityY = jumpForce;
                    Player.IsGrounded = false;
                }

                // Gravity
                if (!Player.IsGrounded)
                {
                    Player.VelocityY += gravity;
                }

                Player.X += Player.VelocityX;
                Player.Y += Player.VelocityY;

                // Map Collision
                ResolvePlayerMapCollisions(moveDown);
            }

            if (Player.Y > FieldHeight)
            {
                KillPlayer();
            }
        }

        private void ResolvePlayerMapCollisions(bool moveDown)
        {
            double footX = Player.X + Player.Width / 2;
            double footY = Player.Y + Player.Height;

            int colLeft = (int)((Player.X + 4) / TileSize);
            int colRight = (int)((Player.X + Player.Width - 4) / TileSize);
            int rowFoot = (int)(footY / TileSize);
            int rowHead = (int)(Player.Y / TileSize);

            // Floor snap (Solid Walls 1, Diggable Bricks 2, and Ladder Tops 3 when walking across without pressing Down)
            if (rowFoot >= 0 && rowFoot < Rows)
            {
                bool isFloorSolid = Grid[rowFoot, colLeft] == 1 || Grid[rowFoot, colLeft] == 2 ||
                                    Grid[rowFoot, colRight] == 1 || Grid[rowFoot, colRight] == 2 ||
                                    ((Grid[rowFoot, colLeft] == 3 || Grid[rowFoot, colRight] == 3) && !Player.IsClimbingLadder && !moveDown);

                if (isFloorSolid)
                {
                    double targetY = rowFoot * TileSize - Player.Height;
                    if (Player.Y + Player.Height >= targetY - 6 && Player.VelocityY >= 0)
                    {
                        Player.Y = targetY;
                        Player.VelocityY = 0;
                        Player.IsGrounded = true;
                    }
                }
                else if (!Player.IsClimbingLadder)
                {
                    Player.IsGrounded = false;
                }
            }

            // Left/Right Wall boundary
            if (colLeft >= 0 && colLeft < Cols && (Grid[rowHead, colLeft] == 1 || Grid[rowHead, colLeft] == 2))
            {
                Player.X = (colLeft + 1) * TileSize - 4;
            }
            if (colRight >= 0 && colRight < Cols && (Grid[rowHead, colRight] == 1 || Grid[rowHead, colRight] == 2))
            {
                Player.X = colRight * TileSize - Player.Width + 4;
            }
        }

        private void UpdateMummies(double deltaTime)
        {
            foreach (var m in Mummies)
            {
                if (m.IsStunned)
                {
                    m.StunTimer -= deltaTime;
                    if (m.StunTimer <= 0) m.IsStunned = false;
                    continue;
                }

                m.X += m.Direction * m.Speed;

                double centerX = m.X + m.Width / 2;
                int frontCol = m.Direction == 1 ? (int)((m.X + m.Width + 4) / TileSize) : (int)((m.X - 4) / TileSize);
                int headRow = (int)((m.Y + 8) / TileSize);

                if (frontCol <= 0 || frontCol >= Cols - 1 || Grid[headRow, frontCol] == 1 || Grid[headRow, frontCol] == 2)
                {
                    m.Direction = -m.Direction;
                }
            }
        }

        private void UpdateProjectiles(double deltaTime)
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                var p = Projectiles[i];
                p.X += p.Direction * p.Speed;

                // Wall hit check
                int col = (int)((p.X + p.Width / 2) / TileSize);
                int row = (int)((p.Y + p.Height / 2) / TileSize);

                if (col <= 0 || col >= Cols - 1 || (row >= 0 && row < Rows && (Grid[row, col] == 1 || Grid[row, col] == 2)))
                {
                    Projectiles.RemoveAt(i);
                    continue;
                }

                // Mummy hit check
                foreach (var m in Mummies)
                {
                    if (!m.IsStunned &&
                        Math.Abs((p.X + p.Width / 2) - (m.X + m.Width / 2)) < 20 &&
                        Math.Abs((p.Y + p.Height / 2) - (m.Y + m.Height / 2)) < 20)
                    {
                        m.IsStunned = true;
                        m.StunTimer = 5.0; // Stun mummy for 5 seconds
                        AddScore(300, m.X, m.Y);
                        Projectiles.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private void UpdatePopups(double deltaTime)
        {
            for (int i = ScorePopups.Count - 1; i >= 0; i--)
            {
                var pop = ScorePopups[i];
                pop.Y -= 0.8;
                pop.LifeTimer -= deltaTime;
                if (pop.LifeTimer <= 0)
                {
                    ScorePopups.RemoveAt(i);
                }
            }
        }

        private void CheckItemCollisions()
        {
            foreach (var item in Items)
            {
                if (!item.IsCollected &&
                    Math.Abs((Player.X + Player.Width / 2) - (item.X + item.Width / 2)) < 22 &&
                    Math.Abs((Player.Y + Player.Height / 2) - (item.Y + item.Height / 2)) < 22)
                {
                    item.IsCollected = true;
                    if (item.Type == "Gem")
                    {
                        GemsRemaining--;
                        AddScore(200, item.X, item.Y);
                        if (GemsRemaining <= 0)
                        {
                            IsExitUnlocked = true;
                        }
                    }
                    else if (item.Type == "Pickaxe")
                    {
                        Player.HasPickaxe = true;
                        AddScore(300, item.X, item.Y);
                    }
                    else if (item.Type == "Knife")
                    {
                        Player.KnivesCount += 3;
                        AddScore(150, item.X, item.Y);
                    }
                }
            }
        }

        private void CheckMummyCollisions()
        {
            if (Player.IsDead) return;

            foreach (var m in Mummies)
            {
                if (!m.IsStunned &&
                    Math.Abs((Player.X + Player.Width / 2) - (m.X + m.Width / 2)) < 18 &&
                    Math.Abs((Player.Y + Player.Height / 2) - (m.Y + m.Height / 2)) < 22)
                {
                    KillPlayer();
                    return;
                }
            }
        }

        private void AddScore(int points, double x, double y)
        {
            Score += points;
            if (Score > HighScore) HighScore = Score;

            ScorePopups.Add(new KVScorePopup
            {
                X = x,
                Y = y,
                Points = points
            });
        }

        private void KillPlayer()
        {
            Player.IsDead = true;
            Lives--;
            if (Lives <= 0)
            {
                IsGameOver = true;
            }
            else
            {
                Player.Reset(80, 508);
            }
        }

        private void OnLevelCleared()
        {
            IsGameWon = true;
            Score += 1000;
            if (Score > HighScore) HighScore = Score;
            Level++;
        }
    }
}
