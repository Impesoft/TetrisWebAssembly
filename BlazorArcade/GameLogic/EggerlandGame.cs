using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorArcade.GameLogic
{
    public enum EggerlandTileType
    {
        Floor = 0,
        Wall = 1,
        Tree = 2,
        Water = 3,
        Lava = 4,
        ArrowUp = 5,
        ArrowDown = 6,
        ArrowLeft = 7,
        ArrowRight = 8,
        HeartFrame = 9,
        JewelChest = 10,
        ExitDoor = 11
    }

    public enum EggerlandDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum MonsterType
    {
        Snakey,     // Stationary, harmless
        Alma,       // Chases player
        Leeper,     // Chases player, sleeps when near player to block path
        Skull,      // Sleeps until hearts collected, then fast chaser
        Gol,        // Sleeps until hearts collected, then shoots fireballs along line of sight
        Medusa,     // Stationary mask with cardinal gaze ray death
        DonMedusa   // Moving mask along fixed axis with gaze ray death
    }

    public class EggerlandMonster
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MonsterType Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }
        public EggerlandDirection Facing { get; set; } = EggerlandDirection.Down;
        
        // Movement state
        public double AnimX { get; set; }
        public double AnimY { get; set; }
        public double MoveProgress { get; set; } = 1.0;
        public int TargetX { get; set; }
        public int TargetY { get; set; }

        // Specific monster properties
        public bool IsAwake { get; set; }
        public bool IsSleeping { get; set; } // For Leeper after it traps
        public double MoveCooldown { get; set; }
        
        // Don Medusa movement axis
        public bool IsHorizontalMovement { get; set; } = true;
        public int MoveDir { get; set; } = 1; // 1 or -1

        public EggerlandMonster Clone()
        {
            return new EggerlandMonster
            {
                Id = Id,
                Type = Type,
                X = X,
                Y = Y,
                SpawnX = SpawnX,
                SpawnY = SpawnY,
                Facing = Facing,
                AnimX = AnimX,
                AnimY = AnimY,
                MoveProgress = MoveProgress,
                TargetX = TargetX,
                TargetY = TargetY,
                IsAwake = IsAwake,
                IsSleeping = IsSleeping,
                MoveCooldown = MoveCooldown,
                IsHorizontalMovement = IsHorizontalMovement,
                MoveDir = MoveDir
            };
        }
    }

    public class EggerlandEgg
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public MonsterType OriginalMonsterType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }
        public double HatchTimer { get; set; } = 12.0; // Seconds before hatching back
        public bool IsInWater { get; set; }
        public double WaterSinkingTimer { get; set; } = 10.0; // Seconds raft lasts in water
        public bool IsBlastedOff { get; set; }
        public double RespawnTimer { get; set; } = 6.0;

        public double AnimX { get; set; }
        public double AnimY { get; set; }

        public EggerlandEgg Clone()
        {
            return new EggerlandEgg
            {
                Id = Id,
                OriginalMonsterType = OriginalMonsterType,
                X = X,
                Y = Y,
                SpawnX = SpawnX,
                SpawnY = SpawnY,
                HatchTimer = HatchTimer,
                IsInWater = IsInWater,
                WaterSinkingTimer = WaterSinkingTimer,
                IsBlastedOff = IsBlastedOff,
                RespawnTimer = RespawnTimer,
                AnimX = AnimX,
                AnimY = AnimY
            };
        }
    }

    public class EggerlandBlock
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int X { get; set; }
        public int Y { get; set; }
        public double AnimX { get; set; }
        public double AnimY { get; set; }
        public bool IsInWater { get; set; }

        public EggerlandBlock Clone()
        {
            return new EggerlandBlock
            {
                Id = Id,
                X = X,
                Y = Y,
                AnimX = AnimX,
                AnimY = AnimY,
                IsInWater = IsInWater
            };
        }
    }

    public class EggerlandShot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double X { get; set; }
        public double Y { get; set; }
        public EggerlandDirection Dir { get; set; }
        public bool IsFromPlayer { get; set; } = true; // true = Egg shot, false = Fireball

        public EggerlandShot Clone()
        {
            return new EggerlandShot
            {
                Id = Id,
                X = X,
                Y = Y,
                Dir = Dir,
                IsFromPlayer = IsFromPlayer
            };
        }
    }

    public class EggerlandPlayer
    {
        public int X { get; set; } = 5;
        public int Y { get; set; } = 9;
        public double AnimX { get; set; } = 5;
        public double AnimY { get; set; } = 9;
        public EggerlandDirection Facing { get; set; } = EggerlandDirection.Up;
        public int Shots { get; set; } = 0;
        public bool IsMoving { get; set; }
        public int TargetX { get; set; } = 5;
        public int TargetY { get; set; } = 9;
        public double MoveProgress { get; set; } = 1.0;

        public EggerlandPlayer Clone()
        {
            return new EggerlandPlayer
            {
                X = X,
                Y = Y,
                AnimX = AnimX,
                AnimY = AnimY,
                Facing = Facing,
                Shots = Shots,
                IsMoving = IsMoving,
                TargetX = TargetX,
                TargetY = TargetY,
                MoveProgress = MoveProgress
            };
        }
    }

    public class EggerlandStateSnapshot
    {
        public int LevelIndex { get; set; }
        public EggerlandTileType[,] Map { get; set; } = new EggerlandTileType[11, 11];
        public EggerlandPlayer Player { get; set; } = new EggerlandPlayer();
        public List<EggerlandMonster> Monsters { get; set; } = new List<EggerlandMonster>();
        public List<EggerlandEgg> Eggs { get; set; } = new List<EggerlandEgg>();
        public List<EggerlandBlock> Blocks { get; set; } = new List<EggerlandBlock>();
        public int HeartsRemaining { get; set; }
        public bool IsChestOpen { get; set; }
        public bool IsJewelCollected { get; set; }
        public bool IsExitOpen { get; set; }
        public int Score { get; set; }
    }

    public class EggerlandGame
    {
        public const int Width = 11;
        public const int Height = 11;

        public EggerlandTileType[,] Map { get; private set; } = new EggerlandTileType[Width, Height];
        public EggerlandPlayer Player { get; private set; } = new EggerlandPlayer();
        public List<EggerlandMonster> Monsters { get; private set; } = new List<EggerlandMonster>();
        public List<EggerlandEgg> Eggs { get; private set; } = new List<EggerlandEgg>();
        public List<EggerlandBlock> Blocks { get; private set; } = new List<EggerlandBlock>();
        public List<EggerlandShot> Shots { get; private set; } = new List<EggerlandShot>();

        public int CurrentLevelIndex { get; set; } = 0;
        public int TotalLevels => 8;
        public string CurrentLevelName => LevelNames[Math.Clamp(CurrentLevelIndex, 0, TotalLevels - 1)];

        public int Score { get; set; } = 0;
        public int Lives { get; set; } = 3;
        public int HeartsRemaining { get; set; } = 0;
        public double ShootCooldown { get; set; } = 0;
        
        public bool IsChestOpen { get; set; } = false;
        public bool IsJewelCollected { get; set; } = false;
        public bool IsExitOpen { get; set; } = false;
        public bool IsGameOver { get; set; } = false;
        public bool IsGameWon { get; set; } = false;
        public bool IsDying { get; set; } = false;
        public double DeathTimer { get; set; } = 0;

        // Sound trigger flags for UI/Audio interop
        public bool SoundPlayStep { get; set; }
        public bool SoundPlayShot { get; set; }
        public bool SoundPlayEgg { get; set; }
        public bool SoundPlayPush { get; set; }
        public bool SoundPlayHeart { get; set; }
        public bool SoundPlayChest { get; set; }
        public bool SoundPlayWin { get; set; }
        public bool SoundPlayDeath { get; set; }

        private Stack<EggerlandStateSnapshot> _undoStack = new Stack<EggerlandStateSnapshot>();

        private static readonly string[] LevelNames = new string[]
        {
            "Stage 1: First Steps",
            "Stage 2: Water Crossing",
            "Stage 3: Alma's Corridor",
            "Stage 4: Medusa's Chamber",
            "Stage 5: Gol's Awakening",
            "Stage 6: Leeper Trap & Arrows",
            "Stage 7: Don Medusa & Skulls",
            "Stage 8: The Master Citadel"
        };

        public EggerlandGame()
        {
            LoadLevel(0);
        }

        public void ResetAudioTriggers()
        {
            SoundPlayStep = false;
            SoundPlayShot = false;
            SoundPlayEgg = false;
            SoundPlayPush = false;
            SoundPlayHeart = false;
            SoundPlayChest = false;
            SoundPlayWin = false;
            SoundPlayDeath = false;
        }

        public void SaveUndoState()
        {
            var snapshot = new EggerlandStateSnapshot
            {
                LevelIndex = CurrentLevelIndex,
                Player = Player.Clone(),
                Monsters = Monsters.Select(m => m.Clone()).ToList(),
                Eggs = Eggs.Select(e => e.Clone()).ToList(),
                Blocks = Blocks.Select(b => b.Clone()).ToList(),
                HeartsRemaining = HeartsRemaining,
                IsChestOpen = IsChestOpen,
                IsJewelCollected = IsJewelCollected,
                IsExitOpen = IsExitOpen,
                Score = Score
            };

            var mapCopy = new EggerlandTileType[Width, Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    mapCopy[x, y] = Map[x, y];
            snapshot.Map = mapCopy;

            _undoStack.Push(snapshot);
        }

        public bool CanUndo => _undoStack.Count > 0 && !IsDying && !IsGameOver;

        public void Undo()
        {
            if (!CanUndo) return;

            var snapshot = _undoStack.Pop();
            CurrentLevelIndex = snapshot.LevelIndex;
            Player = snapshot.Player.Clone();
            Monsters = snapshot.Monsters.Select(m => m.Clone()).ToList();
            Eggs = snapshot.Eggs.Select(e => e.Clone()).ToList();
            Blocks = snapshot.Blocks.Select(b => b.Clone()).ToList();
            HeartsRemaining = snapshot.HeartsRemaining;
            IsChestOpen = snapshot.IsChestOpen;
            IsJewelCollected = snapshot.IsJewelCollected;
            IsExitOpen = snapshot.IsExitOpen;
            Score = snapshot.Score;

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    Map[x, y] = snapshot.Map[x, y];

            Shots.Clear();
            IsDying = false;
        }

        public void RestartLevel()
        {
            LoadLevel(CurrentLevelIndex);
        }

        public void NextLevel()
        {
            if (CurrentLevelIndex + 1 < TotalLevels)
            {
                CurrentLevelIndex++;
                LoadLevel(CurrentLevelIndex);
            }
            else
            {
                IsGameWon = true;
                SoundPlayWin = true;
            }
        }

        public void LoadLevel(int levelIndex)
        {
            CurrentLevelIndex = Math.Clamp(levelIndex, 0, TotalLevels - 1);
            _undoStack.Clear();
            Monsters.Clear();
            Eggs.Clear();
            Blocks.Clear();
            Shots.Clear();
            IsDying = false;
            IsGameOver = false;
            IsGameWon = false;
            IsChestOpen = false;
            IsJewelCollected = false;
            IsExitOpen = false;

            // Clear map to Floor
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Map[x, y] = EggerlandTileType.Floor;

            switch (CurrentLevelIndex)
            {
                case 0: BuildLevel1(); break;
                case 1: BuildLevel2(); break;
                case 2: BuildLevel3(); break;
                case 3: BuildLevel4(); break;
                case 4: BuildLevel5(); break;
                case 5: BuildLevel6(); break;
                case 6: BuildLevel7(); break;
                case 7: BuildLevel8(); break;
            }

            // Count initial heart frames
            HeartsRemaining = 0;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Map[x, y] == EggerlandTileType.HeartFrame)
                    {
                        HeartsRemaining++;
                    }
                }
            }
        }

        #region Level Construction

        private void BuildLevel1()
        {
            // First Steps: 1 Snakey, 1 Emerald Block, 2 Hearts, Jewel Chest
            Player.X = 5; Player.Y = 9;
            Player.AnimX = 5; Player.AnimY = 9;
            Player.TargetX = 5; Player.TargetY = 9;
            Player.Shots = 0;
            Player.Facing = EggerlandDirection.Up;

            // Walls around top border nooks
            Map[5, 0] = EggerlandTileType.ExitDoor;
            Map[5, 1] = EggerlandTileType.JewelChest;

            Map[2, 2] = EggerlandTileType.HeartFrame; // Gives 2 shots
            Map[8, 2] = EggerlandTileType.HeartFrame;

            // Trees as barriers
            Map[1, 1] = EggerlandTileType.Tree;
            Map[9, 1] = EggerlandTileType.Tree;
            Map[3, 4] = EggerlandTileType.Tree;
            Map[7, 4] = EggerlandTileType.Tree;

            // Block in center
            Blocks.Add(new EggerlandBlock { X = 5, Y = 6, AnimX = 5, AnimY = 6 });

            // Snakey monster
            Monsters.Add(new EggerlandMonster
            {
                Type = MonsterType.Snakey,
                X = 5, Y = 4,
                SpawnX = 5, SpawnY = 4,
                AnimX = 5, AnimY = 4,
                TargetX = 5, TargetY = 4
            });
        }

        private void BuildLevel2()
        {
            // Water Crossing: Water channel across middle (y=5), Snakeys to egg & push into water
            Player.X = 5; Player.Y = 9;
            Player.AnimX = 5; Player.AnimY = 9;
            Player.TargetX = 5; Player.TargetY = 9;
            Player.Shots = 0;

            Map[5, 0] = EggerlandTileType.ExitDoor;
            Map[5, 1] = EggerlandTileType.JewelChest;

            // Water stream across y = 5 (except trees at edges)
            for (int x = 1; x < 10; x++)
            {
                Map[x, 5] = EggerlandTileType.Water;
            }

            Map[2, 2] = EggerlandTileType.HeartFrame;
            Map[8, 2] = EggerlandTileType.HeartFrame;
            Map[5, 7] = EggerlandTileType.HeartFrame; // Gives shots

            // Snakeys on southern side
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Snakey, X = 3, Y = 7, SpawnX = 3, SpawnY = 7, AnimX = 3, AnimY = 7, TargetX = 3, TargetY = 7 });
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Snakey, X = 7, Y = 7, SpawnX = 7, SpawnY = 7, AnimX = 7, AnimY = 7, TargetX = 7, TargetY = 7 });
        }

        private void BuildLevel3()
        {
            // Alma's Corridor: Alma enemy chasing, Emerald blocks for defense
            Player.X = 5; Player.Y = 9;
            Player.AnimX = 5; Player.AnimY = 9;
            Player.TargetX = 5; Player.TargetY = 9;
            Player.Shots = 0;

            Map[5, 0] = EggerlandTileType.ExitDoor;
            Map[5, 1] = EggerlandTileType.JewelChest;

            // Heart Frames
            Map[1, 3] = EggerlandTileType.HeartFrame;
            Map[9, 3] = EggerlandTileType.HeartFrame;
            Map[5, 3] = EggerlandTileType.HeartFrame;

            // Pushable blocks to isolate or shield against Alma
            Blocks.Add(new EggerlandBlock { X = 3, Y = 6, AnimX = 3, AnimY = 6 });
            Blocks.Add(new EggerlandBlock { X = 7, Y = 6, AnimX = 7, AnimY = 6 });

            // Trees
            Map[2, 4] = EggerlandTileType.Tree;
            Map[8, 4] = EggerlandTileType.Tree;

            // Alma monster in center corridor
            Monsters.Add(new EggerlandMonster
            {
                Type = MonsterType.Alma,
                X = 5, Y = 5,
                SpawnX = 5, SpawnY = 5,
                AnimX = 5, AnimY = 5,
                TargetX = 5, TargetY = 5
            });
        }

        private void BuildLevel4()
        {
            // Medusa's Chamber: Stationary Medusa in middle (5, 5). Block gaze line of sight!
            Player.X = 5; Player.Y = 9;
            Player.AnimX = 5; Player.AnimY = 9;
            Player.TargetX = 5; Player.TargetY = 9;
            Player.Shots = 0;

            Map[5, 0] = EggerlandTileType.ExitDoor;
            Map[5, 1] = EggerlandTileType.JewelChest;

            // Medusa in middle
            Monsters.Add(new EggerlandMonster
            {
                Type = MonsterType.Medusa,
                X = 5, Y = 5,
                SpawnX = 5, SpawnY = 5,
                AnimX = 5, AnimY = 5,
                TargetX = 5, TargetY = 5
            });

            // Hearts placed in Medusa's gaze lines
            Map[5, 2] = EggerlandTileType.HeartFrame;
            Map[2, 5] = EggerlandTileType.HeartFrame;
            Map[8, 5] = EggerlandTileType.HeartFrame;

            // Blocks provided to shield gaze
            Blocks.Add(new EggerlandBlock { X = 4, Y = 7, AnimX = 4, AnimY = 7 });
            Blocks.Add(new EggerlandBlock { X = 6, Y = 7, AnimX = 6, AnimY = 7 });

            // Trees as cover
            Map[3, 3] = EggerlandTileType.Tree;
            Map[7, 3] = EggerlandTileType.Tree;
        }

        private void BuildLevel5()
        {
            // Gol's Awakening: Gols wake up when hearts are all collected & fire fireballs!
            Player.X = 5; Player.Y = 9;
            Player.AnimX = 5; Player.AnimY = 9;
            Player.TargetX = 5; Player.TargetY = 9;
            Player.Shots = 0;

            Map[5, 0] = EggerlandTileType.ExitDoor;
            Map[5, 1] = EggerlandTileType.JewelChest;

            // Gols at top
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Gol, X = 3, Y = 3, SpawnX = 3, SpawnY = 3, AnimX = 3, AnimY = 3, TargetX = 3, TargetY = 3, Facing = EggerlandDirection.Down });
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Gol, X = 7, Y = 3, SpawnX = 7, SpawnY = 3, AnimX = 7, AnimY = 7, TargetX = 7, TargetY = 3, Facing = EggerlandDirection.Down });

            Map[3, 6] = EggerlandTileType.HeartFrame;
            Map[7, 6] = EggerlandTileType.HeartFrame;
            Map[5, 4] = EggerlandTileType.HeartFrame;

            Blocks.Add(new EggerlandBlock { X = 5, Y = 7, AnimX = 5, AnimY = 7 });
        }

        private void BuildLevel6()
        {
            // Leeper Trap & Arrows: Leeper chases and sleeps, One-way arrows limit movement.
            Player.X = 5; Player.Y = 9;
            Player.AnimX = 5; Player.AnimY = 9;
            Player.TargetX = 5; Player.TargetY = 9;
            Player.Shots = 0;

            Map[5, 0] = EggerlandTileType.ExitDoor;
            Map[5, 1] = EggerlandTileType.JewelChest;

            // One-Way Arrows
            Map[3, 5] = EggerlandTileType.ArrowRight;
            Map[7, 5] = EggerlandTileType.ArrowLeft;
            Map[5, 6] = EggerlandTileType.ArrowUp;

            Map[1, 2] = EggerlandTileType.HeartFrame;
            Map[9, 2] = EggerlandTileType.HeartFrame;

            // Leeper
            Monsters.Add(new EggerlandMonster
            {
                Type = MonsterType.Leeper,
                X = 5, Y = 4,
                SpawnX = 5, SpawnY = 4,
                AnimX = 5, AnimY = 4,
                TargetX = 5, TargetY = 4
            });
        }

        private void BuildLevel7()
        {
            // Don Medusa & Skulls: Moving Don Medusa gaze patrol + Skulls awakening.
            Player.X = 5; Player.Y = 9;
            Player.AnimX = 5; Player.AnimY = 9;
            Player.TargetX = 5; Player.TargetY = 9;
            Player.Shots = 0;

            Map[5, 0] = EggerlandTileType.ExitDoor;
            Map[5, 1] = EggerlandTileType.JewelChest;

            // Don Medusa patrolling horizontally on row 4
            Monsters.Add(new EggerlandMonster
            {
                Type = MonsterType.DonMedusa,
                X = 2, Y = 4,
                SpawnX = 2, SpawnY = 4,
                AnimX = 2, AnimY = 4,
                TargetX = 2, TargetY = 4,
                IsHorizontalMovement = true,
                MoveDir = 1
            });

            // Skulls at bottom corners
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Skull, X = 1, Y = 8, SpawnX = 1, SpawnY = 8, AnimX = 1, AnimY = 8, TargetX = 1, TargetY = 8 });
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Skull, X = 9, Y = 8, SpawnX = 9, SpawnY = 8, AnimX = 9, AnimY = 8, TargetX = 9, TargetY = 8 });

            Map[1, 2] = EggerlandTileType.HeartFrame;
            Map[9, 2] = EggerlandTileType.HeartFrame;
            Map[5, 6] = EggerlandTileType.HeartFrame;

            Blocks.Add(new EggerlandBlock { X = 5, Y = 7, AnimX = 5, AnimY = 7 });
        }

        private void BuildLevel8()
        {
            // Master Citadel: Ultimate test combining Medusa, Gols, Water, Eggs, and Blocks!
            Player.X = 5; Player.Y = 9;
            Player.AnimX = 5; Player.AnimY = 9;
            Player.TargetX = 5; Player.TargetY = 9;
            Player.Shots = 0;

            Map[5, 0] = EggerlandTileType.ExitDoor;
            Map[5, 1] = EggerlandTileType.JewelChest;

            // Water moats
            Map[2, 5] = EggerlandTileType.Water;
            Map[3, 5] = EggerlandTileType.Water;
            Map[7, 5] = EggerlandTileType.Water;
            Map[8, 5] = EggerlandTileType.Water;

            // Medusa in middle top
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Medusa, X = 5, Y = 3, SpawnX = 5, SpawnY = 3, AnimX = 5, AnimY = 3, TargetX = 5, TargetY = 3 });

            // Gol dragons on flanks
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Gol, X = 1, Y = 4, SpawnX = 1, SpawnY = 4, AnimX = 1, AnimY = 4, TargetX = 1, TargetY = 4, Facing = EggerlandDirection.Right });
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Gol, X = 9, Y = 4, SpawnX = 9, SpawnY = 4, AnimX = 9, AnimY = 4, TargetX = 9, TargetY = 4, Facing = EggerlandDirection.Left });

            // Snakey for egg bridge
            Monsters.Add(new EggerlandMonster { Type = MonsterType.Snakey, X = 5, Y = 7, SpawnX = 5, SpawnY = 7, AnimX = 5, AnimY = 7, TargetX = 5, TargetY = 7 });

            Map[1, 2] = EggerlandTileType.HeartFrame;
            Map[9, 2] = EggerlandTileType.HeartFrame;
            Map[5, 8] = EggerlandTileType.HeartFrame; // Gives shots

            Blocks.Add(new EggerlandBlock { X = 4, Y = 6, AnimX = 4, AnimY = 6 });
            Blocks.Add(new EggerlandBlock { X = 6, Y = 6, AnimX = 6, AnimY = 6 });
        }

        #endregion

        #region Game Loop & Physics Update

        public void Update(bool inputUp, bool inputDown, bool inputLeft, bool inputRight, bool inputShoot, double dt)
        {
            ResetAudioTriggers();

            if (IsGameOver || IsGameWon) return;

            if (IsDying)
            {
                DeathTimer -= dt;
                if (DeathTimer <= 0)
                {
                    Lives--;
                    if (Lives <= 0)
                    {
                        IsGameOver = true;
                    }
                    else
                    {
                        RestartLevel();
                    }
                }
                return;
            }

            // 1. Update Player Movement Animation & Interpolation
            UpdatePlayerMovement(inputUp, inputDown, inputLeft, inputRight, dt);

            // 2. Player Action: Shoot Magic Egg Shot
            if (ShootCooldown > 0)
            {
                ShootCooldown -= dt;
            }

            if (inputShoot && Player.Shots > 0 && !Player.IsMoving && ShootCooldown <= 0)
            {
                ShootMagicShot();
                ShootCooldown = 0.5; // Half second cooldown between shots
            }

            // 3. Update Projectile Shots (Magic Shots & Fireballs)
            UpdateShots(dt);

            // 4. Update Eggs (Hatching, Respawning, Water Raft Sinking)
            UpdateEggs(dt);

            // 5. Update Monsters AI & Patrols
            UpdateMonsters(dt);

            // 6. Check Line-of-Sight Hazards (Medusa, Don Medusa, Gol)
            CheckLineOfSightHazards();

            // 7. Check Player Collision with Enemies / Fireballs
            CheckPlayerCollisions();
        }

        private void UpdatePlayerMovement(bool inputUp, bool inputDown, bool inputLeft, bool inputRight, double dt)
        {
            if (Player.IsMoving)
            {
                Player.MoveProgress += dt * 8.0; // Smooth move speed
                if (Player.MoveProgress >= 1.0)
                {
                    Player.MoveProgress = 1.0;
                    Player.X = Player.TargetX;
                    Player.Y = Player.TargetY;
                    Player.AnimX = Player.X;
                    Player.AnimY = Player.Y;
                    Player.IsMoving = false;

                    // Check tile triggers on grid entry
                    OnPlayerEnteredTile(Player.X, Player.Y);
                }
                else
                {
                    Player.AnimX = Player.X + (Player.TargetX - Player.X) * Player.MoveProgress;
                    Player.AnimY = Player.Y + (Player.TargetY - Player.Y) * Player.MoveProgress;
                }
                return;
            }

            // Handle new direction input when stationary
            EggerlandDirection? newDir = null;
            if (inputUp) newDir = EggerlandDirection.Up;
            else if (inputDown) newDir = EggerlandDirection.Down;
            else if (inputLeft) newDir = EggerlandDirection.Left;
            else if (inputRight) newDir = EggerlandDirection.Right;

            if (newDir.HasValue)
            {
                Player.Facing = newDir.Value;
                (int dx, int dy) = GetDirectionOffset(newDir.Value);
                int destX = Player.X + dx;
                int destY = Player.Y + dy;

                if (CanPlayerMoveTo(destX, destY, dx, dy))
                {
                    SaveUndoState();
                    Player.TargetX = destX;
                    Player.TargetY = destY;
                    Player.MoveProgress = 0.0;
                    Player.IsMoving = true;
                    SoundPlayStep = true;
                }
            }
        }

        private bool CanPlayerMoveTo(int destX, int destY, int dx, int dy)
        {
            if (destX < 0 || destX >= Width || destY < 0 || destY >= Height) return false;

            // Tile checks
            var tile = Map[destX, destY];

            // Wall or Tree or Lava
            if (tile == EggerlandTileType.Wall || tile == EggerlandTileType.Tree || tile == EggerlandTileType.Lava)
                return false;

            // Closed Jewel Chest
            if (tile == EggerlandTileType.JewelChest && !IsChestOpen)
                return false;

            // Water (Walkable ONLY if there is an egg raft or block in water)
            if (tile == EggerlandTileType.Water)
            {
                bool hasBlockInWater = Blocks.Any(b => b.X == destX && b.Y == destY && b.IsInWater);
                bool hasEggRaft = Eggs.Any(e => e.X == destX && e.Y == destY && e.IsInWater && !e.IsBlastedOff);
                if (!hasBlockInWater && !hasEggRaft) return false;
            }

            // One-Way Arrow Restriction
            if (tile == EggerlandTileType.ArrowUp && dy > 0) return false;
            if (tile == EggerlandTileType.ArrowDown && dy < 0) return false;
            if (tile == EggerlandTileType.ArrowLeft && dx > 0) return false;
            if (tile == EggerlandTileType.ArrowRight && dx < 0) return false;

            // Check Pushable Emerald Block
            var block = Blocks.FirstOrDefault(b => b.X == destX && b.Y == destY && !b.IsInWater);
            if (block != null)
            {
                return TryPushBlock(block, dx, dy);
            }

            // Check Egg
            var egg = Eggs.FirstOrDefault(e => e.X == destX && e.Y == destY && !e.IsBlastedOff && !e.IsInWater);
            if (egg != null)
            {
                return TryPushEgg(egg, dx, dy);
            }

            // Check Sleeping Leeper or Solid Enemies
            var monster = Monsters.FirstOrDefault(m => m.X == destX && m.Y == destY);
            if (monster != null)
            {
                if (monster.IsSleeping) return false; // Sleeping Leeper is solid wall
                // Active monsters can be walked into (triggers death check)
            }

            return true;
        }

        private bool TryPushBlock(EggerlandBlock block, int dx, int dy)
        {
            int targetX = block.X + dx;
            int targetY = block.Y + dy;

            if (targetX < 0 || targetX >= Width || targetY < 0 || targetY >= Height) return false;

            var targetTile = Map[targetX, targetY];
            if (targetTile == EggerlandTileType.Wall || targetTile == EggerlandTileType.Tree ||
                targetTile == EggerlandTileType.HeartFrame || targetTile == EggerlandTileType.JewelChest ||
                targetTile == EggerlandTileType.ExitDoor) return false;

            // Cannot push block into another block or egg or monster
            if (Blocks.Any(b => b.X == targetX && b.Y == targetY)) return false;
            if (Eggs.Any(e => e.X == targetX && e.Y == targetY && !e.IsBlastedOff)) return false;
            if (Monsters.Any(m => m.X == targetX && m.Y == targetY)) return false;

            // Push block!
            block.X = targetX;
            block.Y = targetY;
            block.AnimX = targetX;
            block.AnimY = targetY;

            if (targetTile == EggerlandTileType.Water)
            {
                block.IsInWater = true;
            }

            SoundPlayPush = true;
            return true;
        }

        private bool TryPushEgg(EggerlandEgg egg, int dx, int dy)
        {
            int targetX = egg.X + dx;
            int targetY = egg.Y + dy;

            if (targetX < 0 || targetX >= Width || targetY < 0 || targetY >= Height) return false;

            var targetTile = Map[targetX, targetY];
            if (targetTile == EggerlandTileType.Wall || targetTile == EggerlandTileType.Tree ||
                targetTile == EggerlandTileType.HeartFrame || targetTile == EggerlandTileType.JewelChest) return false;

            if (Blocks.Any(b => b.X == targetX && b.Y == targetY)) return false;
            if (Eggs.Any(e => e.X == targetX && e.Y == targetY && !e.IsBlastedOff)) return false;
            if (Monsters.Any(m => m.X == targetX && m.Y == targetY)) return false;

            egg.X = targetX;
            egg.Y = targetY;
            egg.AnimX = targetX;
            egg.AnimY = targetY;

            if (targetTile == EggerlandTileType.Water)
            {
                egg.IsInWater = true;
                egg.WaterSinkingTimer = 10.0; // Act as raft
            }

            SoundPlayPush = true;
            return true;
        }

        private void OnPlayerEnteredTile(int x, int y)
        {
            var tile = Map[x, y];

            // Heart Frame Pickup
            if (tile == EggerlandTileType.HeartFrame)
            {
                Map[x, y] = EggerlandTileType.Floor;
                HeartsRemaining--;
                Score += 100;
                SoundPlayHeart = true;

                // Grant 2 Magic Shots for picking up heart frames on certain stages
                Player.Shots += 2;

                if (HeartsRemaining <= 0)
                {
                    IsChestOpen = true;
                    SoundPlayChest = true;

                    // Wake up Skulls and Gols!
                    foreach (var m in Monsters)
                    {
                        if (m.Type == MonsterType.Skull || m.Type == MonsterType.Gol)
                        {
                            m.IsAwake = true;
                        }
                    }
                }
            }
            // Jewel Chest Pickup
            else if (tile == EggerlandTileType.JewelChest && IsChestOpen && !IsJewelCollected)
            {
                IsJewelCollected = true;
                IsExitOpen = true;
                Score += 500;
                SoundPlayWin = true;

                // Open Exit Door tile
                Map[5, 0] = EggerlandTileType.Floor; // Open door
            }
            // Exit Door
            else if ((x == 5 && y == 0 && IsJewelCollected) || tile == EggerlandTileType.ExitDoor && IsJewelCollected)
            {
                Score += 1000;
                NextLevel();
            }
        }

        #endregion

        #region Shots & Projectiles

        private void ShootMagicShot()
        {
            Player.Shots--;
            SoundPlayShot = true;

            (int dx, int dy) = GetDirectionOffset(Player.Facing);
            Shots.Add(new EggerlandShot
            {
                X = Player.X + dx * 0.5,
                Y = Player.Y + dy * 0.5,
                Dir = Player.Facing,
                IsFromPlayer = true
            });
        }

        private void UpdateShots(double dt)
        {
            for (int i = Shots.Count - 1; i >= 0; i--)
            {
                var shot = Shots[i];
                (int dx, int dy) = GetDirectionOffset(shot.Dir);

                double speed = shot.IsFromPlayer ? 14.0 : 10.0;
                shot.X += dx * speed * dt;
                shot.Y += dy * speed * dt;

                int gridX = (int)Math.Round(shot.X);
                int gridY = (int)Math.Round(shot.Y);

                if (gridX < 0 || gridX >= Width || gridY < 0 || gridY >= Height)
                {
                    Shots.RemoveAt(i);
                    continue;
                }

                // Check wall/obstacle collision
                var tile = Map[gridX, gridY];
                if (tile == EggerlandTileType.Wall || tile == EggerlandTileType.Tree || Blocks.Any(b => b.X == gridX && b.Y == gridY && !b.IsInWater))
                {
                    Shots.RemoveAt(i);
                    continue;
                }

                if (shot.IsFromPlayer)
                {
                    // Check hitting Monster
                    var monster = Monsters.FirstOrDefault(m => m.X == gridX && m.Y == gridY);
                    if (monster != null)
                    {
                        // Transform monster into Egg!
                        Eggs.Add(new EggerlandEgg
                        {
                            OriginalMonsterType = monster.Type,
                            X = monster.X,
                            Y = monster.Y,
                            SpawnX = monster.SpawnX,
                            SpawnY = monster.SpawnY,
                            AnimX = monster.X,
                            AnimY = monster.Y,
                            HatchTimer = 12.0
                        });
                        Monsters.Remove(monster);
                        Shots.RemoveAt(i);
                        SoundPlayEgg = true;
                        continue;
                    }

                    // Check hitting existing Egg (Blasts off screen!)
                    var egg = Eggs.FirstOrDefault(e => e.X == gridX && e.Y == gridY && !e.IsBlastedOff);
                    if (egg != null)
                    {
                        egg.IsBlastedOff = true;
                        egg.RespawnTimer = 8.0;
                        Shots.RemoveAt(i);
                        SoundPlayEgg = true;
                        continue;
                    }
                }
            }
        }

        #endregion

        #region Egg Management

        private void UpdateEggs(double dt)
        {
            for (int i = Eggs.Count - 1; i >= 0; i--)
            {
                var egg = Eggs[i];

                if (egg.IsBlastedOff)
                {
                    egg.RespawnTimer -= dt;
                    if (egg.RespawnTimer <= 0)
                    {
                        // Respawn original monster at spawn coordinates if unoccupied
                        if (!Monsters.Any(m => m.X == egg.SpawnX && m.Y == egg.SpawnY) &&
                            !Eggs.Any(e => e.X == egg.SpawnX && e.Y == egg.SpawnY && !e.IsBlastedOff))
                        {
                            Monsters.Add(new EggerlandMonster
                            {
                                Type = egg.OriginalMonsterType,
                                X = egg.SpawnX,
                                Y = egg.SpawnY,
                                SpawnX = egg.SpawnX,
                                SpawnY = egg.SpawnY,
                                AnimX = egg.SpawnX,
                                AnimY = egg.SpawnY,
                                TargetX = egg.SpawnX,
                                TargetY = egg.SpawnY
                            });
                            Eggs.RemoveAt(i);
                        }
                    }
                    continue;
                }

                if (egg.IsInWater)
                {
                    egg.WaterSinkingTimer -= dt;
                    if (egg.WaterSinkingTimer <= 0)
                    {
                        Eggs.RemoveAt(i);
                    }
                    continue;
                }

                // Hatching Timer
                egg.HatchTimer -= dt;
                if (egg.HatchTimer <= 0)
                {
                    // Hatch back into monster
                    Monsters.Add(new EggerlandMonster
                    {
                        Type = egg.OriginalMonsterType,
                        X = egg.X,
                        Y = egg.Y,
                        SpawnX = egg.SpawnX,
                        SpawnY = egg.SpawnY,
                        AnimX = egg.X,
                        AnimY = egg.Y,
                        TargetX = egg.X,
                        TargetY = egg.Y
                    });
                    Eggs.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Monster AI & Line of Sight Hazards

        private void UpdateMonsters(double dt)
        {
            foreach (var m in Monsters)
            {
                if (m.IsSleeping) continue;

                m.MoveCooldown -= dt;
                if (m.MoveCooldown > 0) continue;

                switch (m.Type)
                {
                    case MonsterType.Alma:
                        // Chases Lolo
                        MoveChaserMonster(m, 0.4);
                        break;

                    case MonsterType.Leeper:
                        // Chases Lolo until close
                        int dist = Math.Abs(m.X - Player.X) + Math.Abs(m.Y - Player.Y);
                        if (dist <= 1)
                        {
                            m.IsSleeping = true; // Sleep on spot to form block!
                            SoundPlayEgg = true;
                        }
                        else
                        {
                            MoveChaserMonster(m, 0.35);
                        }
                        break;

                    case MonsterType.Skull:
                        if (m.IsAwake)
                        {
                            MoveChaserMonster(m, 0.25);
                        }
                        break;

                    case MonsterType.DonMedusa:
                        // Patrols back and forth on axis
                        MoveDonMedusa(m, 0.3);
                        break;
                }
            }
        }

        private void MoveChaserMonster(EggerlandMonster m, double cooldownSpeed)
        {
            int dx = 0;
            int dy = 0;

            if (Math.Abs(Player.X - m.X) > Math.Abs(Player.Y - m.Y))
            {
                dx = Math.Sign(Player.X - m.X);
            }
            else
            {
                dy = Math.Sign(Player.Y - m.Y);
            }

            int targetX = m.X + dx;
            int targetY = m.Y + dy;

            if (CanMonsterMoveTo(targetX, targetY))
            {
                m.X = targetX;
                m.Y = targetY;
                m.AnimX = targetX;
                m.AnimY = targetY;
                m.MoveCooldown = cooldownSpeed;
            }
        }

        private void MoveDonMedusa(EggerlandMonster m, double cooldownSpeed)
        {
            int dx = m.IsHorizontalMovement ? m.MoveDir : 0;
            int dy = m.IsHorizontalMovement ? 0 : m.MoveDir;

            int targetX = m.X + dx;
            int targetY = m.Y + dy;

            if (CanMonsterMoveTo(targetX, targetY))
            {
                m.X = targetX;
                m.Y = targetY;
                m.AnimX = targetX;
                m.AnimY = targetY;
                m.MoveCooldown = cooldownSpeed;
            }
            else
            {
                m.MoveDir *= -1; // Reverse direction
                m.MoveCooldown = cooldownSpeed;
            }
        }

        private bool CanMonsterMoveTo(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;

            var tile = Map[x, y];
            if (tile == EggerlandTileType.Wall || tile == EggerlandTileType.Tree || tile == EggerlandTileType.Water ||
                tile == EggerlandTileType.JewelChest || tile == EggerlandTileType.HeartFrame) return false;

            if (Blocks.Any(b => b.X == x && b.Y == y)) return false;
            if (Eggs.Any(e => e.X == x && e.Y == y && !e.IsBlastedOff)) return false;

            return true;
        }

        private void CheckLineOfSightHazards()
        {
            // Medusa and Don Medusa line of sight gaze check
            foreach (var m in Monsters)
            {
                if (m.Type == MonsterType.Medusa || m.Type == MonsterType.DonMedusa)
                {
                    if (IsPlayerInGazeLine(m.X, m.Y))
                    {
                        KillPlayer();
                        return;
                    }
                }
                else if (m.Type == MonsterType.Gol && m.IsAwake)
                {
                    // Check if Gol should fire fireball
                    if (IsPlayerInGazeLine(m.X, m.Y))
                    {
                        (int dx, int dy) = GetDirectionOffset(m.Facing);
                        // Check if Gol isn't already firing
                        if (!Shots.Any(s => !s.IsFromPlayer))
                        {
                            Shots.Add(new EggerlandShot
                            {
                                X = m.X + dx * 0.5,
                                Y = m.Y + dy * 0.5,
                                Dir = m.Facing,
                                IsFromPlayer = false
                            });
                            SoundPlayShot = true;
                        }
                    }
                }
            }
        }

        private bool IsPlayerInGazeLine(int startX, int startY)
        {
            if (Player.X != startX && Player.Y != startY) return false;

            int dx = Math.Sign(Player.X - startX);
            int dy = Math.Sign(Player.Y - startY);

            int cx = startX + dx;
            int cy = startY + dy;

            while (cx != Player.X || cy != Player.Y)
            {
                if (cx < 0 || cx >= Width || cy < 0 || cy >= Height) return false;

                var tile = Map[cx, cy];
                // Obstacles block gaze: Wall, Tree, HeartFrame, JewelChest, EmeraldBlock, Egg
                if (tile == EggerlandTileType.Wall || tile == EggerlandTileType.Tree ||
                    tile == EggerlandTileType.HeartFrame || tile == EggerlandTileType.JewelChest)
                    return false;

                if (Blocks.Any(b => b.X == cx && b.Y == cy && !b.IsInWater)) return false;
                if (Eggs.Any(e => e.X == cx && e.Y == cy && !e.IsBlastedOff && !e.IsInWater)) return false;

                cx += dx;
                cy += dy;
            }

            return true;
        }

        private void CheckPlayerCollisions()
        {
            // Monster contact
            foreach (var m in Monsters)
            {
                if (m.IsSleeping) continue;
                if (m.Type == MonsterType.Snakey) continue; // Snakey is harmless

                if (m.X == Player.X && m.Y == Player.Y)
                {
                    KillPlayer();
                    return;
                }
            }

            // Enemy Fireball contact
            foreach (var shot in Shots)
            {
                if (!shot.IsFromPlayer)
                {
                    int sx = (int)Math.Round(shot.X);
                    int sy = (int)Math.Round(shot.Y);
                    if (sx == Player.X && sy == Player.Y)
                    {
                        KillPlayer();
                        return;
                    }
                }
            }
        }

        private void KillPlayer()
        {
            if (IsDying) return;
            IsDying = true;
            DeathTimer = 1.0;
            SoundPlayDeath = true;
        }

        #endregion

        #region Helpers

        public static (int dx, int dy) GetDirectionOffset(EggerlandDirection dir)
        {
            return dir switch
            {
                EggerlandDirection.Up => (0, -1),
                EggerlandDirection.Down => (0, 1),
                EggerlandDirection.Left => (-1, 0),
                EggerlandDirection.Right => (1, 0),
                _ => (0, 0)
            };
        }

        #endregion
    }
}
