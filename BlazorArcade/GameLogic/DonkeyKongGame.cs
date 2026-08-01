using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorArcade.GameLogic
{
    public class DKPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public DKPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class DKGirder
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }

        public DKGirder(double x1, double y1, double x2, double y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public bool ContainsX(double x)
        {
            double minX = Math.Min(X1, X2);
            double maxX = Math.Max(X1, X2);
            return x >= minX - 5 && x <= maxX + 5;
        }

        public double GetYAtX(double x)
        {
            if (Math.Abs(X2 - X1) < 0.001) return Y1;
            double clampedX = Math.Clamp(x, Math.Min(X1, X2), Math.Max(X1, X2));
            return Y1 + (clampedX - X1) * (Y2 - Y1) / (X2 - X1);
        }

        public int GetDownwardDirection()
        {
            if (Y2 > Y1 + 2) return 1;  // Slants down to the right (+1)
            if (Y1 > Y2 + 2) return -1; // Slants down to the left (-1)
            return -1;                  // Bottom floor: rolls left (-1) towards Oil Drum
        }
    }

    public class DKLadder
    {
        public double X { get; set; }
        public double YTop { get; set; }
        public double YBottom { get; set; }
        public bool IsFullLadder { get; set; }

        public DKLadder(double x, double yTop, double yBottom, bool isFullLadder = true)
        {
            X = x;
            YTop = yTop;
            YBottom = yBottom;
            IsFullLadder = isFullLadder;
        }
    }

    public class DKItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 20;
        public double Height { get; set; } = 20;
        public string Type { get; set; } = "Hammer";
        public bool IsCollected { get; set; }
    }

    public class DKScorePopup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public int Points { get; set; }
        public double LifeTimer { get; set; } = 1.0;
    }

    public class DKPlayer
    {
        public double X { get; set; } = 80;
        public double Y { get; set; } = 532;
        public double Width { get; set; } = 24;
        public double Height { get; set; } = 28;
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }

        public bool IsGrounded { get; set; }
        public bool IsClimbing { get; set; }
        public bool FacingLeft { get; set; }
        public bool IsDead { get; set; }

        public bool HasHammer { get; set; }
        public double HammerTimer { get; set; } = 0;
        public int HammerFrame { get; set; } = 0;

        public HashSet<string> JumpedBarrels { get; } = new HashSet<string>();

        public void Reset(double startX = 80, double startY = 532)
        {
            X = startX;
            Y = startY;
            VelocityX = 0;
            VelocityY = 0;
            IsGrounded = true;
            IsClimbing = false;
            FacingLeft = false;
            IsDead = false;
            HasHammer = false;
            HammerTimer = 0;
            HammerFrame = 0;
            JumpedBarrels.Clear();
        }
    }

    public class DKBarrel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 20;
        public double Height { get; set; } = 20;
        public double Speed { get; set; } = 2.5;
        public int Direction { get; set; } = 1; // 1 = right, -1 = left
        public bool IsFalling { get; set; }
        public bool IsDescendingLadder { get; set; }
        public double TargetY { get; set; }
        public bool IsBlue { get; set; }
        public bool IsIntroBarrel { get; set; }
        public double Rotation { get; set; }
    }

    public class DKFireball
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 20;
        public double Height { get; set; } = 20;
        public int Direction { get; set; } = 1;
        public double Speed { get; set; } = 1.2;
        public double LifeTime { get; set; } = 15.0; // Lifetime in seconds before despawning
    }

    public class DonkeyKongGame
    {
        public double FieldWidth { get; private set; } = 800;
        public double FieldHeight { get; private set; } = 600;

        public DKPlayer Player { get; private set; } = new DKPlayer();
        public List<DKGirder> Girders { get; private set; } = new List<DKGirder>();
        public List<DKLadder> Ladders { get; private set; } = new List<DKLadder>();
        public List<DKBarrel> Barrels { get; private set; } = new List<DKBarrel>();
        public List<DKFireball> Fireballs { get; private set; } = new List<DKFireball>();
        public List<DKItem> Items { get; private set; } = new List<DKItem>();
        public List<DKScorePopup> ScorePopups { get; private set; } = new List<DKScorePopup>();

        public DKPoint DonkeyKongPos { get; private set; } = new DKPoint(120, 65);
        public DKPoint PaulinePos { get; private set; } = new DKPoint(390, 8); // Positioned ON TOP of platform Y=55

        public int Score { get; private set; } = 0;
        public int HighScore { get; private set; } = 7600;
        public int Lives { get; private set; } = 3;
        public int Level { get; private set; } = 1;
        public int BonusTimer { get; private set; } = 5000;

        public bool IsOilLit { get; private set; } = false;
        public bool IsGameOver { get; private set; }
        public bool IsGameWon { get; private set; }
        public bool IsPaused { get; private set; } = false;

        private double _barrelSpawnTimer = 0;
        private double _barrelSpawnInterval = 3.5;
        private double _bonusDecrementTimer = 0;
        private Random _random = new Random();

        // DK animation state (0 = chest beat, 1 = grab barrel, 2 = roll barrel)
        public int DKFrame { get; private set; } = 0;
        private double _dkAnimTimer = 0;

        public DonkeyKongGame()
        {
            InitializeLayout();
            InitializeGame();
        }

        public void InitializeLayout()
        {
            Girders.Clear();
            Ladders.Clear();

            // Girders Layout with extended high ends to outer walls (X=20 / X=780)
            // 1. Tier 0 (Bottom Floor: X 20 to 780)
            Girders.Add(new DKGirder(20, 560, 780, 560));

            // 2. Tier 1 (Slanted right to left: high on right wall X=780, drop gap on left X=65)
            Girders.Add(new DKGirder(65, 485, 780, 465));

            // 3. Tier 2 (Slanted left to right: high on left wall X=20, drop gap on right X=735)
            Girders.Add(new DKGirder(20, 375, 735, 395));

            // 4. Tier 3 (Slanted right to left: high on right wall X=780, drop gap on left X=65)
            Girders.Add(new DKGirder(65, 305, 780, 285));

            // 5. Tier 4 (Slanted left to right: high on left wall X=20, drop gap on right X=735)
            Girders.Add(new DKGirder(20, 195, 735, 215));

            // 6. Tier 5 - Donkey Kong Platform (Top Left: X 20 to 320)
            Girders.Add(new DKGirder(20, 115, 320, 115));

            // 7. Tier 6 - Pauline Platform (Top Center: X 340 to 460)
            Girders.Add(new DKGirder(340, 55, 460, 55));

            // Ladders Layout
            // Tier 0 -> Tier 1
            Ladders.Add(new DKLadder(680, 467, 560, isFullLadder: true));
            Ladders.Add(new DKLadder(300, 500, 560, isFullLadder: false)); // Half ladder

            // Tier 1 -> Tier 2
            Ladders.Add(new DKLadder(120, 378, 480, isFullLadder: true));
            Ladders.Add(new DKLadder(480, 385, 435, isFullLadder: false)); // Half ladder

            // Tier 2 -> Tier 3
            Ladders.Add(new DKLadder(660, 288, 390, isFullLadder: true));
            Ladders.Add(new DKLadder(360, 325, 385, isFullLadder: false)); // Half ladder

            // Tier 3 -> Tier 4
            Ladders.Add(new DKLadder(140, 200, 300, isFullLadder: true));
            Ladders.Add(new DKLadder(500, 245, 290, isFullLadder: false)); // Half ladder

            // Tier 4 -> Top DK Platform
            Ladders.Add(new DKLadder(280, 115, 205, isFullLadder: true));

            // Tier 4 -> Pauline Platform
            Ladders.Add(new DKLadder(360, 55, 205, isFullLadder: true));
            Ladders.Add(new DKLadder(440, 55, 205, isFullLadder: true));
        }

        public void InitializeGame()
        {
            Score = 0;
            Lives = 3;
            Level = 1;
            IsGameOver = false;
            IsGameWon = false;
            IsPaused = false;
            ResetStage();
        }

        public void ResetStage()
        {
            Player.Reset(80, 532);
            Barrels.Clear();
            Fireballs.Clear();
            ScorePopups.Clear();
            Items.Clear();
            IsOilLit = false;

            // Spawn Hammers
            Items.Add(new DKItem { X = 520, Y = 445, Type = "Hammer" });
            Items.Add(new DKItem { X = 240, Y = 265, Type = "Hammer" });

            // Spawn initial Intro Blue Barrel at DK's hands (X=182, Y=95)
            Barrels.Add(new DKBarrel
            {
                X = 182,
                Y = 95,
                Speed = 2.6,
                Direction = 1,
                IsBlue = true,
                IsIntroBarrel = true
            });

            BonusTimer = 5000;
            _barrelSpawnTimer = 0;
            _barrelSpawnInterval = Math.Max(2.0, 4.0 - (Level - 1) * 0.4);
            IsGameWon = false;
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        public DKGirder? GetGirderBelow(double x, double currentY, double maxDistance = 35)
        {
            DKGirder? bestGirder = null;
            double minDiff = double.MaxValue;

            foreach (var g in Girders)
            {
                if (g.ContainsX(x))
                {
                    double girderY = g.GetYAtX(x);
                    double diff = girderY - currentY;
                    if (diff >= -15 && diff <= maxDistance && diff < minDiff)
                    {
                        minDiff = diff;
                        bestGirder = g;
                    }
                }
            }
            return bestGirder;
        }

        public void Update(bool moveLeft, bool moveRight, bool climbUp, bool climbDown, bool jump, double deltaTime = 0.016)
        {
            if (IsGameOver || IsGameWon || IsPaused) return;

            UpdateTimers(deltaTime);

            if (!Player.IsDead)
            {
                UpdatePlayer(moveLeft, moveRight, climbUp, climbDown, jump, deltaTime);
                CheckItemCollisions();
            }

            UpdateBarrels(deltaTime);
            UpdateFireballs(deltaTime);
            UpdatePopups(deltaTime);
            CheckCollisions();

            // Win condition (Touch Pauline)
            if (!Player.IsDead && Math.Abs(Player.X - PaulinePos.X) < 30 && Math.Abs(Player.Y - PaulinePos.Y) < 35)
            {
                OnStageCleared();
            }
        }

        private void UpdateTimers(double deltaTime)
        {
            _barrelSpawnTimer += deltaTime;

            if (_barrelSpawnTimer >= _barrelSpawnInterval)
            {
                _barrelSpawnTimer = 0;
                SpawnBarrel();
                DKFrame = 0; // Held barrel vanishes exact moment real barrel spawns!
            }
            else
            {
                double progress = _barrelSpawnTimer / _barrelSpawnInterval;
                if (progress > 0.8)
                {
                    DKFrame = 2; // Holding & laying barrel onto girder at (182, 95)
                }
                else if (progress > 0.55)
                {
                    DKFrame = 1; // Reaching left to grab barrel from stack
                }
                else
                {
                    _dkAnimTimer += deltaTime;
                    if (_dkAnimTimer > 0.3)
                    {
                        _dkAnimTimer = 0;
                        DKFrame = (DKFrame == 0) ? 3 : 0;
                    }
                }
            }

            _bonusDecrementTimer += deltaTime;
            if (_bonusDecrementTimer >= 0.5)
            {
                _bonusDecrementTimer = 0;
                if (BonusTimer > 0) BonusTimer = Math.Max(0, BonusTimer - 100);
            }

            if (Player.HasHammer)
            {
                Player.HammerTimer -= deltaTime;
                Player.HammerFrame++;
                if (Player.HammerTimer <= 0)
                {
                    Player.HasHammer = false;
                }
            }
        }

        private void SpawnBarrel()
        {
            bool isBlue = _random.Next(0, 5) == 0;
            // Spawn barrel right at DK's hands (X=182, Y=95) when he rolls it!
            Barrels.Add(new DKBarrel
            {
                X = 182,
                Y = 95,
                Speed = 2.4 + (Level * 0.3),
                Direction = 1,
                IsBlue = isBlue
            });
        }

        private void UpdatePlayer(bool moveLeft, bool moveRight, bool climbUp, bool climbDown, bool jump, double deltaTime)
        {
            const double moveSpeed = 3.2;
            const double climbSpeed = 2.2;
            const double gravity = 0.45;
            const double jumpForce = -7.0;

            double playerCenterX = Player.X + Player.Width / 2;
            double playerFootY = Player.Y + Player.Height;

            // Find ladder player is touching
            DKLadder? nearLadder = Ladders.FirstOrDefault(l =>
                Math.Abs(playerCenterX - l.X) < 14 &&
                playerFootY >= l.YTop - 8 &&
                Player.Y <= l.YBottom + 8);

            // Handle ladder mounting
            if (nearLadder != null)
            {
                if (climbUp || climbDown)
                {
                    Player.IsClimbing = true;
                    Player.IsGrounded = false;
                    Player.X = nearLadder.X - Player.Width / 2;
                    Player.VelocityX = 0;
                }
            }
            else if (Player.IsClimbing)
            {
                Player.IsClimbing = false;
            }

            if (Player.IsClimbing && nearLadder != null)
            {
                Player.VelocityY = 0;

                if (climbUp && Player.Y > nearLadder.YTop - Player.Height)
                {
                    Player.Y -= climbSpeed;
                }
                else if (climbDown && Player.Y + Player.Height < nearLadder.YBottom)
                {
                    Player.Y += climbSpeed;
                }

                // Dismount conditions
                if (climbUp && nearLadder.IsFullLadder && Player.Y <= nearLadder.YTop - Player.Height)
                {
                    Player.IsClimbing = false;
                    Player.IsGrounded = true;
                }
                else if (climbDown && Player.Y + Player.Height >= nearLadder.YBottom - 4)
                {
                    Player.Y = nearLadder.YBottom - Player.Height;
                    Player.IsClimbing = false;
                    Player.IsGrounded = true;
                }
                else if ((moveLeft || moveRight) && (Player.Y + Player.Height >= nearLadder.YBottom - 10 || Player.Y <= nearLadder.YTop - Player.Height + 10))
                {
                    Player.IsClimbing = false;
                    Player.IsGrounded = true;
                }
            }
            else
            {
                // Walking Left / Right
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

                // Jumping
                if (jump && Player.IsGrounded && !Player.HasHammer)
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

                // Screen bounds
                if (Player.X < 10) Player.X = 10;
                if (Player.X + Player.Width > FieldWidth - 10) Player.X = FieldWidth - 10 - Player.Width;

                // Snap to girder surface
                double currentFootY = Player.Y + Player.Height;
                DKGirder? g = GetGirderBelow(playerCenterX, currentFootY - 10, 35);

                if (g != null && Player.VelocityY >= 0)
                {
                    double targetY = g.GetYAtX(playerCenterX) - Player.Height;
                    if (currentFootY >= targetY - 4 && currentFootY <= targetY + 12)
                    {
                        Player.Y = targetY;
                        Player.VelocityY = 0;
                        Player.IsGrounded = true;
                    }
                }
                else if (!Player.IsClimbing)
                {
                    Player.IsGrounded = false;
                }
            }

            // Bottom pit fall death
            if (Player.Y > FieldHeight)
            {
                KillPlayer();
            }
        }

        private void UpdateBarrels(double deltaTime)
        {
            for (int i = Barrels.Count - 1; i >= 0; i--)
            {
                var b = Barrels[i];
                b.Rotation += b.Direction * b.Speed * 4;

                double barrelCenterX = b.X + b.Width / 2;

                if (b.IsDescendingLadder)
                {
                    b.Y += b.Speed * 1.3;
                    if (b.Y >= b.TargetY)
                    {
                        b.Y = b.TargetY;
                        b.IsDescendingLadder = false;
                        
                        // When finishing ladder descent, set direction to the downward slope direction of the lower girder!
                        DKGirder? lowerGirder = GetGirderBelow(barrelCenterX, b.Y, 30);
                        if (lowerGirder != null)
                        {
                            b.Direction = lowerGirder.GetDownwardDirection();
                        }
                    }
                    continue;
                }

                // Move horizontally
                b.X += b.Direction * b.Speed;
                barrelCenterX = b.X + b.Width / 2;

                // Outer Wall Bounce at high girder ends
                if (b.X <= 20 && b.Direction == -1)
                {
                    b.X = 20;
                    b.Direction = 1;
                }
                else if (b.X >= 760 && b.Direction == 1)
                {
                    b.X = 760;
                    b.Direction = -1;
                }

                // Get closest girder directly below the barrel's Y position
                DKGirder? currentGirder = GetGirderBelow(barrelCenterX, b.Y, 70);

                if (currentGirder != null)
                {
                    b.Y = currentGirder.GetYAtX(barrelCenterX) - b.Height;
                    b.IsFalling = false;

                    // Check full ladder descent (Barrels ONLY roll down full ladders, ~14% chance)
                    DKLadder? ladderBelow = Ladders.FirstOrDefault(l =>
                        l.IsFullLadder &&
                        Math.Abs(l.X - barrelCenterX) < 10 &&
                        Math.Abs(l.YTop - (b.Y + b.Height)) < 8);

                    if (ladderBelow != null && _random.Next(0, 7) == 0 && !b.IsIntroBarrel)
                    {
                        b.IsDescendingLadder = true;
                        b.TargetY = ladderBelow.YBottom - b.Height;
                        // Center barrel perfectly between the two ladder rails!
                        b.X = ladderBelow.X - b.Width / 2;
                    }
                }
                else
                {
                    // Fall off girder edge
                    b.Y += 5.0;
                    b.IsFalling = true;

                    // Look for girder below as barrel falls
                    DKGirder? nextGirder = GetGirderBelow(barrelCenterX, b.Y, 50);
                    if (nextGirder != null)
                    {
                        b.Y = nextGirder.GetYAtX(barrelCenterX) - b.Height;
                        b.IsFalling = false;
                        // When landing on a lower girder, always roll down its slope!
                        b.Direction = nextGirder.GetDownwardDirection();
                    }
                }

                // Oil drum collision at bottom left (X <= 65, Y >= 520)
                if (b.X <= 65 && b.Y >= 520)
                {
                    IsOilLit = true;
                    if (Fireballs.Count < 3)
                    {
                        Fireballs.Add(new DKFireball { X = 70, Y = 535, Direction = 1, Speed = 1.2, LifeTime = 16.0 });
                    }
                    Barrels.RemoveAt(i);
                    continue;
                }

                // Out of bounds cleanup
                if (b.Y > FieldHeight + 50 || b.X > FieldWidth + 50 || b.X < -50)
                {
                    Barrels.RemoveAt(i);
                }
            }
        }

        private void UpdateFireballs(double deltaTime)
        {
            for (int i = Fireballs.Count - 1; i >= 0; i--)
            {
                var f = Fireballs[i];
                f.LifeTime -= deltaTime;
                if (f.LifeTime <= 0)
                {
                    Fireballs.RemoveAt(i);
                    continue;
                }

                f.X += f.Direction * f.Speed;

                double centerX = f.X + f.Width / 2;
                DKGirder? g = GetGirderBelow(centerX, f.Y, 40);
                if (g != null)
                {
                    f.Y = g.GetYAtX(centerX) - f.Height;
                }

                // Bounce off left/right limits
                if (f.X <= 30)
                {
                    f.X = 30;
                    f.Direction = 1;
                }
                else if (f.X >= 740)
                {
                    f.X = 740;
                    f.Direction = -1;
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
                    Math.Abs((Player.X + Player.Width / 2) - (item.X + item.Width / 2)) < 24 &&
                    Math.Abs((Player.Y + Player.Height / 2) - (item.Y + item.Height / 2)) < 24)
                {
                    item.IsCollected = true;
                    if (item.Type == "Hammer")
                    {
                        Player.HasHammer = true;
                        Player.HammerTimer = 8.0;
                        AddScore(300, item.X, item.Y);
                    }
                }
            }
        }

        private void CheckCollisions()
        {
            if (Player.IsDead) return;

            double playerCenterX = Player.X + Player.Width / 2;
            double playerCenterY = Player.Y + Player.Height / 2;

            // Check Jump-over barrel score
            if (!Player.IsGrounded && !Player.IsClimbing)
            {
                foreach (var b in Barrels)
                {
                    if (!Player.JumpedBarrels.Contains(b.Id))
                    {
                        if (Math.Abs(playerCenterX - (b.X + b.Width / 2)) < 24 &&
                            Player.Y < b.Y - 4)
                        {
                            Player.JumpedBarrels.Add(b.Id);
                            AddScore(100, b.X, b.Y - 15);
                        }
                    }
                }
            }

            // Barrel Collisions
            for (int i = Barrels.Count - 1; i >= 0; i--)
            {
                var b = Barrels[i];
                if (Math.Abs(playerCenterX - (b.X + b.Width / 2)) < 18 &&
                    Math.Abs(playerCenterY - (b.Y + b.Height / 2)) < 18)
                {
                    if (Player.HasHammer)
                    {
                        AddScore(300, b.X, b.Y);
                        Barrels.RemoveAt(i);
                    }
                    else
                    {
                        KillPlayer();
                        return;
                    }
                }
            }

            // Fireball Collisions
            for (int i = Fireballs.Count - 1; i >= 0; i--)
            {
                var f = Fireballs[i];
                if (Math.Abs(playerCenterX - (f.X + f.Width / 2)) < 18 &&
                    Math.Abs(playerCenterY - (f.Y + f.Height / 2)) < 18)
                {
                    if (Player.HasHammer)
                    {
                        AddScore(500, f.X, f.Y);
                        Fireballs.RemoveAt(i);
                    }
                    else
                    {
                        KillPlayer();
                        return;
                    }
                }
            }
        }

        private void AddScore(int points, double x, double y)
        {
            Score += points;
            if (Score > HighScore) HighScore = Score;

            ScorePopups.Add(new DKScorePopup
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
                Player.Reset(80, 532);
            }
        }

        private void OnStageCleared()
        {
            IsGameWon = true;
            Score += BonusTimer;
            if (Score > HighScore) HighScore = Score;
            Level++;
        }
    }
}
