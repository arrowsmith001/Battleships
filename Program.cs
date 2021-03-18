using System;
using System.Collections.Generic;

namespace Battleships
{
    public class CheatCodes
    {
        public static string REVEAL_GRID = "r";
        public static string GIVE_UP = "x";
    }

    class Program
    {
        static void Main(string[] args)
        {
            Battleships battleships = new Battleships(10, 10);
            battleships.Play();
        }
    }

    class Battleships
    {

        private BattleshipsGrid grid;

        public Battleships(int rows, int columns)
        {
            grid = new BattleshipsGrid(rows, columns);
        }

        public void Play()
        {
            char playAgainResponse;

            do
            {
                // Setup grid
                grid.SetUp();

                // Place ships
                grid.AddShip(new Battleship());
                grid.AddShip(new Destroyer());
                grid.AddShip(new Destroyer());

                while (grid.ShipsRemaining > 0)
                {
                    grid.PrintGridForPlayer();
                    Console.WriteLine("Enter grid reference to fire at: ");

                    string input = Console.ReadLine();

                    if (input == CheatCodes.REVEAL_GRID)
                    {
                        grid.PrintActualGrid();
                        continue;
                    }

                    if (input == CheatCodes.GIVE_UP)
                    {
                        break;
                    }

                    Point? chosenPoint = GridRef.GridRefToPoint(input);
                    if (chosenPoint == null)
                    {
                        Console.WriteLine("Please enter a valid point. It should be a letter directly followed by a number i.e. A1.");
                        continue;
                    }

                    Point point = (Point)chosenPoint;

                    if (!grid.IsPointValid(point))
                    {
                        Console.WriteLine("That point is outside the bounds of the grid");
                        continue;
                    }

                    string fireString = grid.Fire(point);
                    Console.WriteLine(fireString);
                }

                grid.PrintActualGrid();

                Console.WriteLine("Game Over");

                do
                {
                    Console.WriteLine();
                    Console.WriteLine("Play again? [y/n]: ");
                    playAgainResponse = char.ToUpper(Console.ReadKey().KeyChar);
                }
                while (playAgainResponse != 'Y' && playAgainResponse != 'N');


            } while (playAgainResponse == 'Y');

        }



    }

    class BattleshipsGrid
    {
        public BattleshipsGrid(int rows, int cols)
        {
            this.rows = rows;
            this.cols = cols;
        }

        private int rows;
        private int cols;
        public int Rows { get => rows; }
        public int Columns { get => cols; }
        public int ShipsRemaining { get => shipRegistry.Count - shipsSunk; }

        public void SetUp()
        {
            grid = new int[rows, cols];
            gridFiredAt = new bool[rows, cols];

            shipRegistry.Clear();
            shipsSunk = 0;
            currentBuildCode = 1;
        }

        private int[,] grid; // Grid of numbers indicating 0 (no ship) or 1+ (a ship code)
        private bool[,] gridFiredAt; // Bool grid indicating whether the player has fired at a spot or not

        private Dictionary<int, Ship> shipRegistry = new Dictionary<int, Ship>(); // Stores registered ships
        private int shipsSunk = 0; // Number of ships sunk
        private int currentBuildCode = 1; // This determines the symbol representing the various ships


        public Point GetRandomPoint()
        {
            Random random = new Random();
            int row = random.Next(0, Rows);
            int col = random.Next(0, Columns);

            return new Point(row, col);
        }
        public bool IsPointValid(Point point)
        {
            if (point.Row < 0 || point.Col < 0) return false;
            if (point.Row >= Rows || point.Col >= Columns) return false;
            return true;
        }

        // This method will keep trying to place the ship and wont give up. 
        // Therefore it assumes that the ship placement is possible (i.e. the ship isn't longer than the grid space, there is available space, etc)
        public void AddShip(Ship ship)
        {
            // Pick a random empty point to start building a ship
            Point startPoint;
            do { startPoint = GetRandomPoint(); }
            while (!IsPointAvailable(startPoint));

            // Pick a random orientation to start building (no orientation will get preference)
            RandomOrientation randomOrientation = new RandomOrientation();
            Orientation orientation = randomOrientation.PrimaryOrientation;

            // "Scan" in that direction to ensure ample space
            Point p1 = startPoint.Clone();
            Point p2 = startPoint.Clone();
            Scan(ref p1, ref p2, orientation);
            int scannedSize = (int) Point.StraightLineDistance(p1, p2);

            // Is the space too small for a ship?
            if (scannedSize < ship.Size())
            {
                // If so, try other orientation
                orientation = randomOrientation.SecondaryOrientation;

                // "Scan" in this direction to ensure ample space
                p1.Copy(startPoint);
                p2.Copy(startPoint);
                Scan(ref p1, ref p2, orientation);
                scannedSize = (int) Point.StraightLineDistance(p1, p2);

                // Is the space too small for a ship?
                if (scannedSize < ship.Size())
                {
                    // Try all over again with another random point
                    AddShip(ship);
                    return;
                }
            }

            // If we made it this far, we have a line the ship can fit along (somewhere between p1 and p2).
            // Now we determine (randomly) where we will offset the ship along this line.
            int wiggleRoom = scannedSize - ship.Size();

            Random random = new Random();
            int offset = random.Next(0, wiggleRoom);

            if (orientation == Orientation.horizontal){ 
                p1.Right(offset);
            }
            else{
                p1.Down(offset);
            }

            // Now we have the true start point, ship size and orientation. Time to build the ship.
            int code = GetBuildCode();
            Build(code, p1);
            int buildProgress = 1;

            while(buildProgress < ship.Size())
            {
                if (orientation == Orientation.horizontal) p1.Right(1);
                else p1.Down(1);

                Build(code, p1);
                buildProgress++;
            }

            // Lastly, we register this ship
            shipRegistry.Add(code, ship);
        }

        public string Fire(Point point)
        {
            if (gridFiredAt[point.Row, point.Col] == true) return "You've already fired here!";

            // Toggle firedAt to true
            gridFiredAt[point.Row, point.Col] = true;

            if (grid[point.Row, point.Col] == 0) return "Miss.";
            else
            {
                int shipCode = grid[point.Row, point.Col];
                Ship ship = shipRegistry[shipCode];

                ship.Hit();

                if (ship.IsDestroyed())
                {
                    shipsSunk++;
                    return "Hit! You sunk my " + ship.Name() + ".";
                }
                else return "Hit!";
            }
        }

        public void PrintGridForPlayer()
        {
            PrintGrid(PRINTCODE_PLAYER_VIEW);
        }

        public void PrintActualGrid()
        {
            PrintGrid(PRINTCODE_ACTUAL_GRID);
        }



        private void Scan(ref Point p1, ref Point p2, Orientation orientation)
        {
            if (orientation == Orientation.horizontal)
            {
                do { p1.Left(1); } while (IsPointAvailable(p1)); // Edge leftwards
                p1.Right(1); // Undo final step

                do { p2.Right(1); } while (IsPointAvailable(p2)); // Edge rightwards
                p2.Left(1); // Undo final step
            }
            else
            {
                do { p1.Up(1); } while (IsPointAvailable(p1)); // Edge upwards
                p1.Down(1); // Undo final step

                do { p2.Down(1); } while (IsPointAvailable(p2)); // Edge downwards
                p2.Up(1); // Undo final step
            }
        }
        private int GetBuildCode() => currentBuildCode++;
        private void Build(int code, Point p)
        {
            grid[p.Row, p.Col] = code;
        }
        private bool IsPointAvailable(Point point)
        {
            if (!IsPointValid(point)) return false;
            return grid[point.Row, point.Col] == 0;
        }

        static int PRINTCODE_ACTUAL_GRID = 0;
        static int PRINTCODE_PLAYER_VIEW = 1;

        private void PrintGrid(int printCode)
        {
            string leftIndent = "    ";

            Console.WriteLine();
            Console.Write(leftIndent + " ");
            for (int c = 0; c < Columns; c++)
            {
                Console.Write((c + 1) + " ");
            }
            Console.WriteLine();
            Console.WriteLine();

            for (int i = 0; i < Rows; i++)
            {
                Console.Write(MyStrings.alphabet[i] + leftIndent);

                for (int j = 0; j < Columns; j++)
                {
                    switch(printCode)
                    {
                        case 0:

                            Console.Write(grid[i, j]);

                            break;
                        case 1:

                            bool firedAt = gridFiredAt[i, j];
                            int shipCode = grid[i, j];

                            if (!firedAt) Console.Write("?");
                            else if (shipCode == 0) Console.Write("-");
                            else if (!shipRegistry[shipCode].IsDestroyed()) Console.Write("X");
                            else Console.Write(shipCode);

                            break;
                    }
                    
                    Console.Write(" ");
                }

                Console.WriteLine();
            }

            Console.WriteLine();
        }

    }

    struct Point
    {
        public Point(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        private int row;
        private int col;

        public int Row { get => row; private set { row = value; } }
        public int Col { get => col; private set { col = value; } }

        public void Left(int by) { col -= by; }
        public void Right(int by) { col += by; }
        public void Up(int by) { row -= by; }
        public void Down(int by) { row += by; }

        public void Copy(Point toCopy)
        {
            this.row = toCopy.row;
            this.col = toCopy.col;
        }
        public Point Clone() => new Point(row, col);

        // Assumes points share a row or column. Returns null otherwise.
        public static int? StraightLineDistance(Point p1, Point p2)
        {
            if (p1.row == p2.row) return Math.Abs(p1.col - p2.col);
            if (p1.col == p2.col) return Math.Abs(p1.row - p2.row);
            return null;
        }

        public override string ToString()
        {
            return "[" + row + "," + col + "]";
        }

    }

    class GridRef
    {
        public static Point? GridRefToPoint(string entry)
        {
            char letterString = entry[0];
            int letterIndex = MyStrings.alphabet.IndexOf(char.ToUpper(letterString));

            if (letterIndex == -1) return null;

            int num;

            string numString = entry.Substring(1);
            try
            {
                num = int.Parse(numString);
            }
            catch
            {
                return null;
            }

            return new Point(letterIndex, num - 1);
        }

    }
     
    enum Orientation { horizontal, vertical }

    abstract class RandomEnum
    {
        public RandomEnum() { Randomize(); }
        public abstract void Randomize();
    }

    class RandomOrientation : RandomEnum
    {
        public override void Randomize()
        {
            Random random = new Random();
            int random01 = random.Next(0, 2);
            if (random01 == 0)
            {
                orientation1 = Orientation.horizontal;
                orientation2 = Orientation.vertical;
            }
            else
            {
                orientation2 = Orientation.horizontal;
                orientation1 = Orientation.vertical;
            }
        }

        Orientation orientation1;
        Orientation orientation2;

        public Orientation PrimaryOrientation { get => orientation1; }
        public Orientation SecondaryOrientation { get => orientation2; }
    }


    abstract class Ship
    {
        private int hits = 0;

        public abstract string Name();
        public abstract int Size();
        public void Hit() { hits++; }
        public bool IsDestroyed() => Size() <= hits;
    }

    class Battleship : Ship
    {
        public override string Name() => "Battleship";

        public override int Size() => 5;
    }
    class Destroyer : Ship
    {
        public override string Name() => "Destroyer";

        public override int Size() => 4;
    }

    class MyStrings
    {
        public static string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    }
    
}
