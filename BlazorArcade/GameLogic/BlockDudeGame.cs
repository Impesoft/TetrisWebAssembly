using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorArcade.GameLogic
{
    public class BDPlayer
    {
        public int GridX { get; set; }
        public int GridY { get; set; }
        public double RenderX { get; set; }
        public double RenderY { get; set; }
        
        public bool FacingLeft { get; set; }
        public bool IsCarryingBlock { get; set; }

        public bool IsMoving { get; set; }
        public int TargetGridX { get; set; }
        public int TargetGridY { get; set; }
    }

    public class BDBlock
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int GridX { get; set; }
        public int GridY { get; set; }
        public double RenderX { get; set; }
        public double RenderY { get; set; }
        
        public bool IsFalling { get; set; }
        public int TargetGridY { get; set; }
    }

    public class BlockDudeGame
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        
        public int[,] Map { get; private set; } // 0: Empty, 1: Wall, 3: Exit
        public BDPlayer Player { get; private set; }
        public List<BDBlock> Blocks { get; private set; }

        public int CurrentLevel { get; private set; } = 0;
        public bool LevelComplete { get; private set; }
        public bool GameComplete { get; private set; }

        public double MoveSpeed { get; set; } = 8.0; // Grid cells per second
        public double FallSpeed { get; set; } = 15.0;

        private readonly string[] _levelData = new string[]
        {
            // Level 1
            "11111111111111111111," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "13000000000000000001," +
            "11100000000020000001," +
            "11100004000220000001," +
            "11100011111111111111," +
            "11111111111111111111," +
            "11111111111111111111",

            // Level 2
            "11111111111111111111," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000030000001," +
            "10000000000111000001," +
            "10000020000001000001," +
            "10040111002201000001," +
            "11111111111111111111," +
            "11111111111111111111," +
            "11111111111111111111",

            // Level 3
            "11111111111111111111," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000000001," +
            "10000000000000003001," +
            "10000000000000011111," +
            "10000000000000010001," +
            "10000000000000010001," +
            "10420202020202010001," +
            "11111111111111111111," +
            "11111111111111111111," +
            "11111111111111111111"
        };

        public void LoadLevel(int levelIndex)
        {
            if (levelIndex >= _levelData.Length)
            {
                GameComplete = true;
                return;
            }

            CurrentLevel = levelIndex;
            LevelComplete = false;
            GameComplete = false;
            Blocks = new List<BDBlock>();
            Player = new BDPlayer();

            string rawData = _levelData[levelIndex].Replace(",", "");
            Width = 20;
            Height = 14;
            Map = new int[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = rawData[y * Width + x];
                    if (c == '1') Map[x, y] = 1;
                    else if (c == '3') Map[x, y] = 3;
                    else
                    {
                        Map[x, y] = 0;
                        if (c == '2')
                        {
                            Blocks.Add(new BDBlock
                            {
                                GridX = x,
                                GridY = y,
                                RenderX = x,
                                RenderY = y
                            });
                        }
                        else if (c == '4')
                        {
                            Player.GridX = x;
                            Player.GridY = y;
                            Player.RenderX = x;
                            Player.RenderY = y;
                        }
                    }
                }
            }
        }

        public void ResetLevel()
        {
            LoadLevel(CurrentLevel);
        }

        public void MoveLeft()
        {
            if (Player.IsMoving || LevelComplete || GameComplete || IsAnyBlockFalling()) return;
            Player.FacingLeft = true;
            TryMove(-1);
        }

        public void MoveRight()
        {
            if (Player.IsMoving || LevelComplete || GameComplete || IsAnyBlockFalling()) return;
            Player.FacingLeft = false;
            TryMove(1);
        }

        private void TryMove(int dx)
        {
            int nx = Player.GridX + dx;
            int ny = Player.GridY;
            
            bool isSolidTarget = IsSolid(nx, ny);
            
            if (isSolidTarget)
            {
                // Try step up
                bool canStepUp = true;
                
                // Check if step up space is free
                if (IsSolid(nx, ny - 1) || IsSolid(Player.GridX, Player.GridY - 1))
                {
                    canStepUp = false;
                }
                
                // If carrying a block, check the space above that too (y - 2)
                if (Player.IsCarryingBlock)
                {
                    if (IsSolid(nx, ny - 2) || IsSolid(Player.GridX, Player.GridY - 2))
                    {
                        canStepUp = false;
                    }
                }

                if (canStepUp)
                {
                    // Can step up
                    Player.TargetGridX = nx;
                    Player.TargetGridY = ny - 1;
                    Player.IsMoving = true;
                }
            }
            else
            {
                // Move horizontally
                bool canMove = true;
                if (Player.IsCarryingBlock)
                {
                    // Space for the carried block must be free
                    if (IsSolid(nx, ny - 1))
                    {
                        canMove = false;
                    }
                }

                if (canMove)
                {
                    Player.TargetGridX = nx;
                    Player.TargetGridY = ny;
                    Player.IsMoving = true;
                }
            }
        }

        public void Action()
        {
            if (Player.IsMoving || LevelComplete || GameComplete || IsAnyBlockFalling()) return;

            int dx = Player.FacingLeft ? -1 : 1;

            if (Player.IsCarryingBlock)
            {
                // Drop block
                int nx = Player.GridX + dx;
                // Can we drop it? The space immediately in front and above the block in front must be free.
                if (!IsSolid(nx, Player.GridY - 1))
                {
                    // Find lowest empty space
                    int dropY = Player.GridY - 1;
                    while (dropY < Height - 1 && !IsSolid(nx, dropY + 1))
                    {
                        dropY++;
                    }

                    var newBlock = new BDBlock
                    {
                        GridX = nx,
                        GridY = Player.GridY - 1, // Drops from player's head height
                        RenderX = nx,
                        RenderY = Player.GridY - 1,
                        IsFalling = true,
                        TargetGridY = dropY
                    };
                    Blocks.Add(newBlock);
                    Player.IsCarryingBlock = false;
                }
            }
            else
            {
                // Pick up block
                int nx = Player.GridX + dx;
                int ny = Player.GridY;

                // Make sure there is space above the player to hold the block
                if (IsSolid(Player.GridX, Player.GridY - 1)) return;
                
                // Make sure there is space above the block to pull it up
                if (IsSolid(nx, ny - 1)) return;

                var blockToPick = GetBlockAt(nx, ny);
                if (blockToPick != null)
                {
                    Blocks.Remove(blockToPick);
                    Player.IsCarryingBlock = true;
                }
            }
        }

        public void Update(double dt)
        {
            if (GameComplete) return;

            // Handle Block Falling
            bool blocksWereFalling = false;
            foreach (var block in Blocks)
            {
                if (block.IsFalling)
                {
                    blocksWereFalling = true;
                    block.RenderY += FallSpeed * dt;
                    if (block.RenderY >= block.TargetGridY)
                    {
                        block.RenderY = block.TargetGridY;
                        block.GridY = block.TargetGridY;
                        block.IsFalling = false;
                    }
                }
                else
                {
                    // Check if should fall
                    if (block.GridY < Height - 1 && !IsSolid(block.GridX, block.GridY + 1, block))
                    {
                        block.IsFalling = true;
                        block.TargetGridY = block.GridY;
                        while (block.TargetGridY < Height - 1 && !IsSolid(block.GridX, block.TargetGridY + 1, block))
                        {
                            block.TargetGridY++;
                        }
                    }
                }
            }

            if (blocksWereFalling) return; // Wait for blocks to finish falling

            // Handle Player Falling
            if (!Player.IsMoving && Player.GridY < Height - 1 && !IsSolid(Player.GridX, Player.GridY + 1))
            {
                Player.TargetGridX = Player.GridX;
                Player.TargetGridY = Player.GridY;
                while (Player.TargetGridY < Height - 1 && !IsSolid(Player.GridX, Player.TargetGridY + 1))
                {
                    Player.TargetGridY++;
                }
                Player.IsMoving = true;
                // Fall faster
            }

            // Handle Player Movement Interpolation
            if (Player.IsMoving)
            {
                double speed = (Player.TargetGridX == Player.GridX) ? FallSpeed : MoveSpeed; // fall vs move
                
                // Move X
                if (Player.RenderX < Player.TargetGridX)
                {
                    Player.RenderX += speed * dt;
                    if (Player.RenderX >= Player.TargetGridX) Player.RenderX = Player.TargetGridX;
                }
                else if (Player.RenderX > Player.TargetGridX)
                {
                    Player.RenderX -= speed * dt;
                    if (Player.RenderX <= Player.TargetGridX) Player.RenderX = Player.TargetGridX;
                }

                // Move Y
                if (Player.RenderY < Player.TargetGridY)
                {
                    Player.RenderY += speed * dt;
                    if (Player.RenderY >= Player.TargetGridY) Player.RenderY = Player.TargetGridY;
                }
                else if (Player.RenderY > Player.TargetGridY)
                {
                    Player.RenderY -= speed * dt;
                    if (Player.RenderY <= Player.TargetGridY) Player.RenderY = Player.TargetGridY;
                }

                if (Math.Abs(Player.RenderX - Player.TargetGridX) < 0.01 && Math.Abs(Player.RenderY - Player.TargetGridY) < 0.01)
                {
                    Player.GridX = Player.TargetGridX;
                    Player.GridY = Player.TargetGridY;
                    Player.RenderX = Player.GridX;
                    Player.RenderY = Player.GridY;
                    Player.IsMoving = false;
                    
                    CheckLevelComplete();
                }
            }
        }

        private void CheckLevelComplete()
        {
            if (Map[Player.GridX, Player.GridY] == 3) // Door
            {
                LevelComplete = true;
            }
        }

        public void NextLevel()
        {
            if (LevelComplete)
            {
                LoadLevel(CurrentLevel + 1);
            }
        }

        public bool IsSolid(int x, int y, BDBlock ignoreBlock = null)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return true;
            if (Map[x, y] == 1) return true;
            var block = GetBlockAt(x, y);
            if (block != null && block != ignoreBlock) return true;
            return false;
        }

        private BDBlock GetBlockAt(int x, int y)
        {
            return Blocks.FirstOrDefault(b => b.GridX == x && b.GridY == y && !b.IsFalling);
        }

        private bool IsAnyBlockFalling()
        {
            return Blocks.Any(b => b.IsFalling);
        }
    }
}
