namespace BlazorArcade.GameLogic
{
    public enum Direction { None, Up, Down, Left, Right }
    public enum TileType { Wall, Empty, Dot, PowerPellet, GhostGate }
    public enum GhostState { Normal, Frightened, Eaten }

    public class Entity
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Direction CurrentDirection { get; set; }
        public Direction NextDirection { get; set; }
    }

    public class Ghost : Entity
    {
        public string Color { get; set; } = "red";
        public GhostState State { get; set; } = GhostState.Normal;
        public int FrightenedTimer { get; set; } = 0;
        public int StartX { get; set; }
        public int StartY { get; set; }
        public int GhostDelay { get; set; } = 0;
        
        public Ghost(int x, int y, string color, int delay)
        {
            X = x; StartX = x;
            Y = y; StartY = y;
            Color = color;
            GhostDelay = delay;
            CurrentDirection = Direction.Up;
        }

        public void Reset()
        {
            X = StartX;
            Y = StartY;
            State = GhostState.Normal;
            CurrentDirection = Direction.Up;
        }
    }

    public class PacmanGame
    {
        public int Width { get; private set; } = 21;
        public int Height { get; private set; } = 22;
        public TileType[,] Map { get; private set; }
        public Entity Player { get; private set; }
        public List<Ghost> Ghosts { get; private set; }
        
        public int Score { get; private set; }
        public int Lives { get; private set; } = 3;
        public bool IsGameOver { get; private set; }
        public bool IsGameWon { get; private set; }
        
        public bool JustAteDot { get; set; }
        public bool JustAteGhost { get; set; }
        public bool JustDied { get; set; }
        
        private int _totalDots;
        private int _dotsEaten;
        private Random _rnd = new Random();
        private int _ghostTickCounter = 0;

        // 21x22 map
        private readonly string[] _levelDesign = new string[]
        {
            "WWWWWWWWWWWWWWWWWWWWW",
            "W.........W.........W",
            "W.WWW.WWW.W.WWW.WWW.W",
            "WOWWW.WWW.W.WWW.WWWOW",
            "W...................W",
            "W.WWW.W.WWWWW.W.WWW.W",
            "W.....W...W...W.....W",
            "WWWWW.WWW E WWW.WWWWW",
            "EEEEE.W EEEEE W.EEEEE",
            "WWWWW.W WWGWW W.WWWWW",
            "E    .E WEEEW E.    E",
            "WWWWW.W WWWWW W.WWWWW",
            "EEEEE.W EEEEE W.EEEEE",
            "WWWWW.W WWWWW W.WWWWW",
            "W.........W.........W",
            "W.WWW.WWW.W.WWW.WWW.W",
            "WO..W.....P.....W..OW",
            "WWW.W.W.WWWWW.W.W.WWW",
            "W.....W...W...W.....W",
            "W.WWWWWWW.W.WWWWWWW.W",
            "W...................W",
            "WWWWWWWWWWWWWWWWWWWWW"
        };

        public PacmanGame()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            Map = new TileType[Width, Height];
            Player = new Entity { X = 1, Y = 1, CurrentDirection = Direction.None, NextDirection = Direction.None };
            Ghosts = new List<Ghost>();
            Score = 0;
            Lives = 3;
            IsGameOver = false;
            IsGameWon = false;
            _totalDots = 0;
            _dotsEaten = 0;
            _ghostTickCounter = 0;
            JustAteDot = false;
            JustAteGhost = false;
            JustDied = false;

            LoadMap();
        }

        private void LoadMap()
        {
            Ghosts.Clear();
            _totalDots = 0;
            _dotsEaten = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = _levelDesign[y][x];
                    switch (c)
                    {
                        case 'W': Map[x, y] = TileType.Wall; break;
                        case '.': Map[x, y] = TileType.Dot; _totalDots++; break;
                        case 'O': Map[x, y] = TileType.PowerPellet; _totalDots++; break;
                        case 'G': Map[x, y] = TileType.GhostGate; break;
                        case 'E': Map[x, y] = TileType.Empty; break;
                        case 'P': 
                            Map[x, y] = TileType.Empty; 
                            Player.X = x; Player.Y = y; 
                            break;
                        case ' ': Map[x, y] = TileType.Empty; break;
                    }
                }
            }

            // Add ghosts at the center box (around 10,10)
            Ghosts.Add(new Ghost(10, 10, "red", 0));
            Ghosts.Add(new Ghost(9, 10, "pink", 10));
            Ghosts.Add(new Ghost(11, 10, "cyan", 20));
            Ghosts.Add(new Ghost(10, 9, "orange", 30)); // slightly out
        }

        public void Update()
        {
            if (IsGameOver || IsGameWon) return;

            MovePlayer();
            MoveGhosts();
            CheckCollisions();
            CheckWinCondition();
        }

        public void SetNextDirection(Direction dir)
        {
            Player.NextDirection = dir;
        }

        private void MovePlayer()
        {
            JustAteDot = false;
            
            // Try to move in next direction
            if (Player.NextDirection != Direction.None && CanMove(Player.X, Player.Y, Player.NextDirection, false))
            {
                Player.CurrentDirection = Player.NextDirection;
                Player.NextDirection = Direction.None;
            }

            // Move in current direction if possible
            if (Player.CurrentDirection != Direction.None && CanMove(Player.X, Player.Y, Player.CurrentDirection, false))
            {
                var nextPos = GetNextPosition(Player.X, Player.Y, Player.CurrentDirection);
                Player.X = nextPos.X;
                Player.Y = nextPos.Y;
                
                HandleWrapAround(Player);

                // Eat dot or power pellet
                if (Map[Player.X, Player.Y] == TileType.Dot)
                {
                    Map[Player.X, Player.Y] = TileType.Empty;
                    Score += 10;
                    _dotsEaten++;
                    JustAteDot = true;
                }
                else if (Map[Player.X, Player.Y] == TileType.PowerPellet)
                {
                    Map[Player.X, Player.Y] = TileType.Empty;
                    Score += 50;
                    _dotsEaten++;
                    FrightenGhosts();
                    JustAteDot = true;
                }
            }
        }

        private void MoveGhosts()
        {
            _ghostTickCounter++;
            if (_ghostTickCounter % 3 == 0) return; // Ghosts move 66% as fast as Pacman
            
            foreach (var ghost in Ghosts)
            {
                if (ghost.GhostDelay > 0)
                {
                    ghost.GhostDelay--;
                    continue;
                }

                if (ghost.State == GhostState.Frightened)
                {
                    ghost.FrightenedTimer--;
                    if (ghost.FrightenedTimer <= 0)
                    {
                        ghost.State = GhostState.Normal;
                    }
                }

                if (ghost.State == GhostState.Eaten)
                {
                    // Move towards start (simple tele for now, or pathfind)
                    // Let's just teleport them back to start for simplicity in this version
                    ghost.Reset();
                    ghost.GhostDelay = 20;
                    continue;
                }

                // AI: simple wander or chase
                List<Direction> possibleMoves = new List<Direction> { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
                // Don't reverse direction unless hitting a wall
                Direction reverse = GetReverseDirection(ghost.CurrentDirection);
                
                var validMoves = possibleMoves.Where(d => d != reverse && CanMove(ghost.X, ghost.Y, d, true)).ToList();
                
                if (validMoves.Count == 0)
                {
                    // Must reverse
                    if (CanMove(ghost.X, ghost.Y, reverse, true))
                        ghost.CurrentDirection = reverse;
                    else
                        ghost.CurrentDirection = Direction.None; // stuck
                }
                else
                {
                    // Choose best move
                    if (ghost.State == GhostState.Frightened)
                    {
                        // Random valid move
                        ghost.CurrentDirection = validMoves[_rnd.Next(validMoves.Count)];
                    }
                    else
                    {
                        // Chase player: pick valid move that minimizes distance to player
                        // Simple euclidean distance
                        Direction bestMove = validMoves[0];
                        double minDistance = double.MaxValue;
                        foreach (var move in validMoves)
                        {
                            var pos = GetNextPosition(ghost.X, ghost.Y, move);
                            double dist = Math.Sqrt(Math.Pow(pos.X - Player.X, 2) + Math.Pow(pos.Y - Player.Y, 2));
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                bestMove = move;
                            }
                        }
                        ghost.CurrentDirection = bestMove;
                    }
                }

                if (ghost.CurrentDirection != Direction.None)
                {
                    var nextPos = GetNextPosition(ghost.X, ghost.Y, ghost.CurrentDirection);
                    ghost.X = nextPos.X;
                    ghost.Y = nextPos.Y;
                    HandleWrapAround(ghost);
                }
            }
        }

        private void CheckCollisions()
        {
            JustDied = false;
            JustAteGhost = false;
            foreach (var ghost in Ghosts)
            {
                if (ghost.X == Player.X && ghost.Y == Player.Y)
                {
                    if (ghost.State == GhostState.Normal)
                    {
                        // Player dies
                        JustDied = true;
                        Lives--;
                        if (Lives <= 0)
                        {
                            IsGameOver = true;
                        }
                        else
                        {
                            ResetPositions();
                        }
                        return; // stop checking this frame
                    }
                    else if (ghost.State == GhostState.Frightened)
                    {
                        // Eat ghost
                        JustAteGhost = true;
                        Score += 200;
                        ghost.State = GhostState.Eaten;
                    }
                }
            }
        }

        private void CheckWinCondition()
        {
            if (_dotsEaten >= _totalDots && _totalDots > 0)
            {
                IsGameWon = true;
            }
        }

        private void FrightenGhosts()
        {
            foreach (var ghost in Ghosts)
            {
                if (ghost.State == GhostState.Normal || ghost.State == GhostState.Frightened)
                {
                    ghost.State = GhostState.Frightened;
                    ghost.FrightenedTimer = 40; // Approx 6-8 seconds at 150-200ms per tick
                    ghost.CurrentDirection = GetReverseDirection(ghost.CurrentDirection); // reverse direction when frightened
                }
            }
        }

        private void ResetPositions()
        {
            Player.X = 10; // Approx start position
            Player.Y = 16;
            Player.CurrentDirection = Direction.None;
            Player.NextDirection = Direction.None;

            foreach (var ghost in Ghosts)
            {
                ghost.Reset();
            }
        }

        private bool CanMove(int x, int y, Direction dir, bool isGhost)
        {
            var nextPos = GetNextPosition(x, y, dir);
            
            // Allow going off-screen for wrap around
            if (nextPos.X < 0 || nextPos.X >= Width) return true;
            if (nextPos.Y < 0 || nextPos.Y >= Height) return false; // Usually only horizontal wrap

            TileType tile = Map[nextPos.X, nextPos.Y];
            if (tile == TileType.Wall) return false;
            if (tile == TileType.GhostGate && !isGhost) return false;
            
            return true;
        }

        private void HandleWrapAround(Entity entity)
        {
            if (entity.X < 0) entity.X = Width - 1;
            else if (entity.X >= Width) entity.X = 0;
        }

        private (int X, int Y) GetNextPosition(int x, int y, Direction dir)
        {
            return dir switch
            {
                Direction.Up => (x, y - 1),
                Direction.Down => (x, y + 1),
                Direction.Left => (x - 1, y),
                Direction.Right => (x + 1, y),
                _ => (x, y)
            };
        }

        private Direction GetReverseDirection(Direction dir)
        {
            return dir switch
            {
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                _ => Direction.None
            };
        }
    }
}
