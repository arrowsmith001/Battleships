using System;

namespace Battleships
{
    class Program
    {
        static void Main(string[] args)
        {
            Battleships battleships = new Battleships(10, 10);
            battleships.Play();

            while (Console.ReadLine() == "y")
            {
                battleships.Play();
            }

        }
    }

    class Battleships
    {

        private int rows;
        private int columns;

        private BattleshipsGrid grid;

        public Battleships(int rows, int columns)
        {
            this.rows = rows;
            this.columns = columns;
        }

        public void Play()
        {
            // Set up grid
            InitializeGrid();

            // Place ships
            AddShipToGrid(new Battleship());
            AddShipToGrid(new Destroyer());
            AddShipToGrid(new Destroyer());

            grid.PrintGrid();
        }

        private void InitializeGrid() { grid = new BattleshipsGrid(rows, columns); }

        private void AddShipToGrid(Ship ship)
        {

            grid.AddShip(ship);


        }

    }

    class BattleshipsGrid
    {
        public BattleshipsGrid(int rows, int columns)
        {
            grid = new int[rows, columns];
        }

        private int[,] grid;

        // This determines the symbol representing the various ships
        private int highestBuildCode = 1; 


        public int Rows { get => grid.GetLength(0); }
        public int Columns { get => grid.GetLength(1); }

        public Point GetRandomPoint()
        {
            Random random = new Random();
            int row = random.Next(0, Rows);
            int col = random.Next(0, Columns);

            return new Point(row, col);
        }

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
            Point p1 = startPoint.Copy();
            Point p2 = startPoint.Copy();
            Scan(ref p1, ref p2, orientation);
            int scannedSize = (int) Point.StraightLineDistance(p1, p2);

            // Can we fit the ship here?
            if (scannedSize < ship.Size())
            {
                // If not, try other orientation
                orientation = randomOrientation.SecondaryOrientation;

                // "Scan" in this direction to ensure ample space
                p1 = startPoint.Copy();
                p2 = startPoint.Copy();
                Scan(ref p1, ref p2, orientation);
                scannedSize = (int) Point.StraightLineDistance(p1, p2);

                if (scannedSize < ship.Size())
                {
                    // Try all over again with another random point
                    AddShip(ship);
                }
            }

            // If we made it this far, we have a line the ship can fit along (somewhere between p1 and p2).
            // Now we determine (randomly) where we will offset the ship along this line.
            int wiggleRoom = scannedSize - ship.Size();

            Random random = new Random();
            int offset = random.Next(0, wiggleRoom);

            if (orientation == Orientation.horizontal){ 
                p1 = p1.Right(offset);
            }
            else{
                p1 = p1.Down(offset);
            }

            // Now we have the true start point, ship size and orientation. Time to build the ship.
            int code = GetBuildCode();
            Build(code, p1);
            int buildProgress = 1;

            while(buildProgress < ship.Size())
            {
                if (orientation == Orientation.horizontal) p1 = p1.Right(1);
                else p1 = p1.Down(1);

                Build(code, p1);
                buildProgress++;
            }
            
        }

        private void Scan(ref Point p1, ref Point p2, Orientation orientation)
        {
            if (orientation == Orientation.horizontal)
            {
                do { p1 = p1.Left(1); } while (IsPointAvailable(p1)); // Edge leftwards
                p1 = p1.Right(1); // Undo final step

                do { p2 = p2.Right(1); } while (IsPointAvailable(p2)); // Edge rightwards
                p2 = p2.Left(1); // Undo final step
            }
            else
            {
                do { p1 = p1.Up(1); } while (IsPointAvailable(p1)); // Edge upwards
                p1 = p1.Down(1); // Undo final step

                do { p2 = p2.Down(1); } while (IsPointAvailable(p2)); // Edge downwards
                p2 = p2.Up(1); // Undo final step
            }
        }

        private int GetBuildCode() => highestBuildCode++;
        private void Build(int code, Point p)
        {
            grid[p.row, p.col] = code;
        }

        private bool IsPointAvailable(Point point)
        {
            if (!IsPointValid(point)) return false;
            return grid[point.row, point.col] == 0;
        }

        private bool IsPointValid(Point point)
        {
            if(point.row < 0 || point.col < 0) return false; 
            if (point.row >= Rows || point.col >= Columns) return false;
            return true;
        }

        public void PrintGrid()
        {
            Console.Write("\n");

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    Console.Write(grid[i, j]);
                    Console.Write(" ");
                }

                Console.Write("\n");
            }

            Console.Write("\n");
        }
    }

    struct Point
    {
        public Point(int row, int col)
        {
            this.row = row;
            this.col = col;
        }

        public readonly int row;
        public readonly int col;

        public Point Left(int by) => new Point(row, col - by);
        public Point Right(int by) => new Point(row, col + by);
        public Point Up(int by) => new Point(row - by, col);
        public Point Down(int by) => new Point(row + by, col);

        public Point Copy() => new Point(row, col);

        // Assumes points share a row or column. Returns null otherwise.
        public static int? StraightLineDistance(Point p1, Point p2)
        {
            if (p1.row == p2.row) return Math.Abs(p1.col - p2.col);
            if (p1.col == p2.col) return Math.Abs(p1.row - p2.row);
            return null;
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
        public abstract int Size();
        public abstract string Name();
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

}
