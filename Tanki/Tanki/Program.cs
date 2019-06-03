using System;
using System.Threading;

namespace Tanki
{
    internal class Program
    {
        private static bool _game = true;
        private static KeysInfo _keys;
        private static BulletsArray _bullets;

        //field initialization; 0 - Wall, ' ' - Empty, 2 - player, v,^,<,> - enemy
        private static readonly string[][] Levels =
        {
            new[]
            {
                "000000000000000",
                "0  v       v  0",
                "0 0 0 0 0 0 0 0",
                "0 0 0 0 0 0 0 0",
                "0 0 0 0 0 0 0 0",
                "0 0 0 000 0 0 0",
                "0 0 0     0 0 0",
                "0     0 0     0",
                "00 00     00 00",
                "0     0 0     0",
                "0 0 0 000 0 0 0",
                "0 0 0 0 0 0 0 0",
                "0 0 0 0 0 0 0 0",
                "0    2000     0",
                "000000000000000"
            }
        };


        public static void Main(string[] args)
        {
            Console.CursorVisible = false;
            
            char key;
            do
            {
                Cell[,] field = new Cell[0,0];
                if (_game)
                {
                    Thread keyThread = new Thread(KeyInput);
                    keyThread.Priority = ThreadPriority.Normal;

                    keyThread.Start();
                    
                    field = InitField(Levels[0]);
                    DrawField(field);
                }

                while (_game)
                {
                    UpdateField(field);
                    DrawField(field);

                    Thread.Sleep(200);
                }

                ConsoleMessage("Play again? y/n", ydiff: 2);
                key = Console.ReadKey(true).KeyChar;
                if (key == 'y')
                {
                    _game = true;
                }

            } while (key != 'n');
        }

        enum Types
        {
            Wall,
            Empty,
            Player,
            Enemy,
            DestroyedObject,
            Boss
        }

        enum Rotates
        {
            Left,
            Up,
            Right,
            Down
        }

        struct KeysInfo
        {
            public bool UpKey;
            public bool DownKey;
            public bool LeftKey;
            public bool RightKey;
            public bool FireKey;
        }

        struct Bullet
        {
            private int x;
            private int y;
            private Rotates direction;
            public bool IsActive;
            private bool isEnemys;

            public Bullet(bool active)
            {
                x = 0;
                y = 0;
                direction = Rotates.Up;
                IsActive = false;
                isEnemys = true;
            }

            public Bullet(int x, int y, Rotates direction, bool isEnemys)
            {
                this.x = x;
                this.y = y;
                this.direction = direction;
                IsActive = true;
                this.isEnemys = isEnemys;
            }

            public void Draw(int consoleY, int consoleX, Cell [,]field)
            {
                if (field[x, y].Type != Types.Enemy)
                {
                    Console.SetCursorPosition(consoleY + y, consoleX + x);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.Write('.');
                }
            }

            public void Shot(Cell[,]field, int idx)
            {
                int newI = x;
                int newJ = y;
                switch (direction)
                {
                    case Rotates.Up:
                        newI -= 1;
                        break;
                    case Rotates.Right:
                        newJ += 1;
                        break;
                    case Rotates.Down:
                        newI += 1;
                        break;
                    case Rotates.Left:
                        newJ -= 1;
                        break;
                }
                if (newI >= 0 && newI < field.GetLength(0) && newJ >= 0 && newJ < field.GetLength(1))
                {
                    if (field[newI, newJ].Type == Types.Empty 
                        || (field[newI, newJ].Type == Types.Enemy && _bullets.Bullets[idx].isEnemys)
                        || (field[newI, newJ].Type == Types.Boss && _bullets.Bullets[idx].isEnemys))
                    {
                        _bullets.Bullets[idx].x = newI;
                        _bullets.Bullets[idx].y = newJ;
                    }
                    else if (field[newI, newJ].Type == Types.Wall)
                    {
                        _bullets.DeleteBullet(idx);
                        field[newI, newJ].Type = Types.DestroyedObject;
                    }
                    else if (field[newI, newJ].Type == Types.Player 
                             || field[newI, newJ].Type == Types.Enemy
                             || field[newI, newJ].Type == Types.Boss)
                    {
                        _bullets.DeleteBullet(idx);
                        field[newI, newJ].Hit(field);
                    }
                }
                else
                {
                    _bullets.DeleteBullet(idx);
                }
            }
        }

        struct BulletsArray
        {
            public Bullet[] Bullets;

            public BulletsArray(int size = 0)
            {
                Bullets = new Bullet[size];
            }

            public void AddBullet(Bullet bullet)
            {
                Expand();
                Bullets[Bullets.Length - 1] = bullet;
            }

            public void DeleteBullet(int idx)
            {
                for (int i = idx; i < Bullets.Length - 1; i++)
                    Bullets[i] = Bullets[i + 1];
                Shrink();
            }

            private void Expand()
            {
                Bullet[] copy = new Bullet[Bullets.Length];
                for (int i = 0; i < Bullets.Length; i++)
                    copy[i] = Bullets[i];
                Bullets = new Bullet[copy.Length + 1];
                for (int i = 0; i < copy.Length; i++)
                    Bullets[i] = copy[i];
            }

            private void Shrink()
            {
                Bullet[] copy = new Bullet[Bullets.Length - 1];
                for (int i = 0; i < Bullets.Length - 1; i++)
                    copy[i] = Bullets[i];
                Bullets = new Bullet[copy.Length];
                for (int i = 0; i < copy.Length; i++)
                    Bullets[i] = copy[i];
            }
        }

        struct Cell
        {
            public Types Type;
            public Rotates Rotate;
            public ConsoleColor FColor;
            public ConsoleColor BColor;
            public bool MadeStep;
            public int Health;

            public static int EnemyCount = 0;

            public Cell(Types type, Rotates rotate)
            {
                switch (type)
                {
                    case Types.Player:
                        FColor = ConsoleColor.Green;
                        BColor = ConsoleColor.Black;
                        Health = 5;
                        break;
                    case Types.Enemy:
                        FColor = ConsoleColor.Red;
                        BColor = ConsoleColor.Black;
                        Health = 1;
                        break;
                    case Types.Boss:
                        FColor = ConsoleColor.Black;
                        BColor = ConsoleColor.Red;
                        Health = 10;
                        break;
                    default:
                        FColor = ConsoleColor.White;
                        BColor = ConsoleColor.Black;
                        Health = -1;
                        break;
                }
                this.Type = type;
                this.Rotate = rotate;
                MadeStep = false;
            }

            public void TurnRight()
            {
                Rotate = (Rotates) (((int) Rotate + 1) % 4);
            }

            public void TurnLeft()
            {
                Rotate = (Rotates) ((int) Rotate - 1 >= 0 ? (int) Rotate - 1 : 3);
            }

            public void Hit(Cell [,]field)
            {
                Health--;
                if (Type == Types.Boss)
                {
                    ConsoleMessage("Boss health: " + Health, ConsoleColor.Yellow, ConsoleColor.Black);
                }
                if (Health < 1)
                {
                    if (Type == Types.Boss)
                    {
                        Winning();
                    }
                    else if (Type == Types.Enemy)
                    {
                        Cell.EnemyCount--;
                        if (EnemyCount < 1)
                            BossAppear(field);
                    }
                    else if (Type == Types.Player)
                    {
                        Loosing();
                    }
                    Type = Types.DestroyedObject;
                }
            }

            public void Step(Cell[,]field, int i, int j, bool backward = false)
            {
                int coordChange = 1;
                if (backward)
                    coordChange = -1;
                int newI = i, newJ = j;
                switch (Rotate)
                {
                    case Rotates.Up:
                        newI -= coordChange;
                        break;
                    case Rotates.Right:
                        newJ += coordChange;
                        break;
                    case Rotates.Down:
                        newI += coordChange;
                        break;
                    case Rotates.Left:
                        newJ -= coordChange;
                        break;
                }
                if (newI >= 0 && newI < field.GetLength(0) && newJ >= 0 && newJ < field.GetLength(1)
                    && field[newI, newJ].Type == Types.Empty)
                {
                    Cell temp = field[newI, newJ];
                    field[newI, newJ] = field[i, j];
                    field[newI, newJ].MadeStep = true;
                    field[i, j] = temp;
                }
            }

            public void EnemyBehavior(Cell[,]field, int i, int j)
            {
                Random rnd = new Random();

                int coin = Type == Types.Boss ? 0 : rnd.Next(0, 2);
                if (coin == 0)
                {
                    coin = rnd.Next(0, 2);
                    if (coin == 0)
                        field[i, j].Step(field, i, j);
                    else
                    {
                        coin = rnd.Next(0, 2);
                        if (coin == 0)
                            field[i, j].TurnLeft();
                        else
                        {
                            field[i, j].TurnRight();
                        }
                    }

                    int makeShot = Type == Types.Boss ? rnd.Next(0, 4) : rnd.Next(0, 7);

                    if (makeShot == 0)
                        field[i, j].MakeBullet(i, j, true);
                }
            }

            public void MakeBullet(int i, int j, bool isEnemy)
            {
                _bullets.AddBullet(new Bullet(i, j, Rotate, isEnemy));
            }

            public void MakeEmpty()
            {
                Type = Types.Empty;
                Rotate = Rotates.Up;
                FColor = ConsoleColor.White;
                BColor = ConsoleColor.Black;
            }
        }

        static Cell[,] InitField(string[]fieldMap)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            Cell[,] field = new Cell[fieldMap.Length, fieldMap[0].Length];

            for (int i = 0; i < fieldMap.Length; i++)
            {
                for (int j = 0; j < fieldMap[0].Length; j++)
                {
                    Rotates rotate;
                    Types type;
                    switch (fieldMap[i][j])
                    {
                        case 'v':
                            type = Types.Enemy;
                            rotate = Rotates.Down;
                            break;
                        case '^':
                            type = Types.Enemy;
                            rotate = Rotates.Up;
                            break;
                        case '<':
                            type = Types.Enemy;
                            rotate = Rotates.Left;
                            break;
                        case '>':
                            type = Types.Enemy;
                            rotate = Rotates.Right;
                            break;
                        case ' ':
                            type = Types.Empty;
                            rotate = Rotates.Up;
                            break;
                        default:
                            type = (Types) (Convert.ToInt32(fieldMap[i][j]) - Convert.ToInt32('0'));
                            rotate = Rotates.Up;
                            break;
                    }
                    if (type == Types.Enemy)
                        Cell.EnemyCount++;
                    field[i, j] = new Cell(type, rotate);
                }
            }
            _bullets = new BulletsArray(10);
            return field;
        }

        static void DrawField(Cell[,] field)
        {
            int topStartDraw = Console.WindowHeight / 2 - field.GetLength(0) / 2;
            int leftStartDraw = Console.WindowWidth / 2 - field.GetLength(1) / 2;

            int playerLives = 0;
            for (int i = 0; i < field.GetLength(0); i++)
            {
                for (int j = 0; j < field.GetLength(1); j++)
                {
                    Console.SetCursorPosition(leftStartDraw + j, topStartDraw + i);
                    Console.ForegroundColor = field[i, j].FColor;
                    Console.BackgroundColor = field[i, j].BColor;
                    char writeSumbol = '@';
                    switch (field[i, j].Type)
                    {
                        case Types.Wall:
                            writeSumbol = '#';
                            break;
                        case Types.Empty:
                            writeSumbol = ' ';
                            break;
                        case Types.DestroyedObject:
                            writeSumbol = 'o';
                            break;
                        case Types.Player:
                            playerLives = field[i, j].Health;
                            goto case Types.Enemy;
                        case Types.Enemy:
                            switch (field[i, j].Rotate)
                            {
                                case Rotates.Left:
                                    writeSumbol = '<';
                                    break;
                                case Rotates.Up:
                                    writeSumbol = '^';
                                    break;
                                case Rotates.Right:
                                    writeSumbol = '>';
                                    break;
                                case Rotates.Down:
                                    writeSumbol = 'v';
                                    break;
                            }
                            field[i, j].MadeStep = false;
                            break;
                        case Types.Boss:
                            switch (field[i, j].Rotate)
                            {
                                case Rotates.Left:
                                    writeSumbol = '<';
                                    break;
                                case Rotates.Up:
                                    writeSumbol = '^';
                                    break;
                                case Rotates.Right:
                                    writeSumbol = '>';
                                    break;
                                case Rotates.Down:
                                    writeSumbol = 'v';
                                    break;
                            }
                            field[i, j].MadeStep = false;
                            break;
                    }
                    Console.Write(writeSumbol);
                }
            }
            DrawLives(leftStartDraw, topStartDraw - 5, playerLives);
            for (int i = 0; i < _bullets.Bullets.Length; i++)
            {
                if (_bullets.Bullets[i].IsActive)
                    _bullets.Bullets[i].Draw(leftStartDraw, topStartDraw, field);
            }
        }

        static void DrawLives(int left, int top, int amount)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.SetCursorPosition(left, top);
            Console.Write("         ");
            Console.SetCursorPosition(left, top);
            Console.ForegroundColor = ConsoleColor.Red;
            for(int i = 0; i < amount; i++)
                Console.Write("\u0003 ");
        }

        static void ConsoleMessage(string message, ConsoleColor fColor = ConsoleColor.Black, ConsoleColor bColor = ConsoleColor.White, int ydiff = 0)
        {
            int leftStartDraw = Console.WindowWidth / 2 - message.Length / 2;
            Console.SetCursorPosition(leftStartDraw - 20, 5 + ydiff);
            Console.ForegroundColor = fColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("                                             ");
            Console.SetCursorPosition(leftStartDraw, 5 + ydiff);
            Console.BackgroundColor = bColor;
            Console.Write(message);
        }

        static void UpdateField(Cell[,] field)
        {
            for (int i = 0; i < field.GetLength(0); i++)
            {
                for (int j = 0; j < field.GetLength(1); j++)
                {
                    switch (field[i, j].Type)
                    {
                        case Types.Player:
                            if (_keys.FireKey)
                            {
                                field[i, j].MakeBullet(i, j, false);
                                _keys.FireKey = false;
                            }
                            if (_keys.LeftKey)
                            {
                                field[i, j].TurnLeft();
                                _keys.LeftKey = false;
                            }
                            if (_keys.RightKey)
                            {
                                field[i, j].TurnRight();
                                _keys.RightKey = false;
                            }
                            if (_keys.UpKey)
                            {
                                field[i, j].Step(field, i, j);
                                _keys.UpKey = false;
                            }
                            if (_keys.DownKey)
                            {
                                field[i, j].Step(field, i, j, true);
                                _keys.DownKey = false;
                            }
                            break;
                        case Types.DestroyedObject:
                            field[i, j].MakeEmpty();
                            break;
                        case Types.Enemy:
                        case Types.Boss:
                            if(!field[i, j].MadeStep)
                                field[i, j].EnemyBehavior(field, i, j);
                            break;
                    }
                }
            }
            for (int i = 0; i < _bullets.Bullets.Length; i++)
            {
                if (_bullets.Bullets[i].IsActive)
                {
                    _bullets.Bullets[i].Shot(field, i);
                }
            }
        }

        static void BossAppear(Cell [,]field)
        {
            Random rnd = new Random();
            int randI, randJ;
            do
            {
                randI = rnd.Next(0, field.GetLength(0));
                randJ = rnd.Next(0, field.GetLength(1));
            } while (field[randI, randJ].Type != Types.Empty && field[randI, randJ].Type != Types.DestroyedObject);

            field[randI, randJ] = new Cell(Types.Boss, Rotates.Down);
            ConsoleMessage("Boss Appears!!");
        }

        static void Winning()
        {
            ConsoleMessage("You win!");
            _game = false;
        }

        static void Loosing()
        {
            ConsoleMessage("You loose!");
            _game = false;
        }

        static void KeyInput()
        {
            while (_game)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                _keys.UpKey = false;
                _keys.DownKey = false;
                _keys.LeftKey = false;
                _keys.RightKey = false;

                switch (key.KeyChar)
                {
                    case 'W':
                    case 'w':
                        _keys.UpKey = true;
                        break;
                    case 'A':
                    case 'a':
                        _keys.LeftKey = true;
                        break;
                    case 'S':
                    case 's':
                        _keys.DownKey = true;
                        break;
                    case 'D':
                    case 'd':
                        _keys.RightKey = true;
                        break;
                    case 'F':
                    case 'f':
                        _keys.FireKey = true;
                        break;
                }
            }
        }
    }
}