using System.CommandLine;
using System.Security.Cryptography;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var inputOption = new Option<string>(
            name: "--input",
            description: "Input file path",
            getDefaultValue: () => "input.txt");

        var partOption = new Option<int>(
            name: "--part",
            description: "Part 1 or 2",
            getDefaultValue: () => 1);

        var rootCommand = new RootCommand();
        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(partOption);

        rootCommand.SetHandler(Run, inputOption, partOption);

        await rootCommand.InvokeAsync(args);
    }

    public static async Task Run(string input, int part)
    {
        var tiles = await Parse(input);
        var task = part switch
        {
            1 => Part1(tiles),
            2 => Part2(),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(char[][] tiles)
    {
        var energisedTiles = FindEnergisedTiles(tiles);

        foreach (var point in energisedTiles)
        {
            tiles[point.Row][point.Col] = '#';
        }

        Console.WriteLine(energisedTiles.Count);

        return Task.CompletedTask;
    }

    private static Task Part2()
    {
        return Task.CompletedTask;
    }

    private static void PrintTiles(char[][] tiles)
    {
        foreach (var row in tiles)
        {
            Console.WriteLine(new string(row));
        }
    }

    private static HashSet<Point> FindEnergisedTiles(char[][] tiles)
    {
        var queue = new Queue<Beam>();
        var energised = new HashSet<Point>();
        var seen = new HashSet<Beam>();

        queue.Enqueue(new Beam(new Point(0, -1), Direction.Right));
        while (queue.Count > 0)
        {
            var beam = queue.Dequeue();

            if (seen.Contains(beam))
            {
                continue;
            }
            if (beam.Location.WithinBounds(tiles))
            {
                energised.Add(beam.Location);
                seen.Add(beam);
            }

            foreach (var newBeam in beam.Move(tiles))
            {
                queue.Enqueue(newBeam);
            }
        }

        return energised;
    }

    private static async Task<char[][]> Parse(string input)
    {
        return (await File.ReadAllLinesAsync(input))
            .Select(s => s.ToCharArray())
            .ToArray();
    }

    public record Point(int Row, int Col)
    {
        public Point Move(Direction direction)
        {
            return direction switch
            {
                Direction.Up => this with { Row = Row - 1 },
                Direction.Right => this with { Col = Col + 1 },
                Direction.Down => this with { Row = Row + 1 },
                Direction.Left => this with { Col = Col - 1 },
                _ => throw new ArgumentException("Invalid direction")
            };
        }

        public bool WithinBounds(char[][] tiles)
        {
            return Row >= 0 && Row < tiles.Length
                && Col >= 0 && Col < tiles[0].Length;
        }

    }

    public record Beam(Point Location, Direction Direction)
    {
        public IEnumerable<Beam> Move(char[][] tiles)
        {
            var newLocation = Location.Move(Direction);

            if (!newLocation.WithinBounds(tiles))
            {
                yield break;
            }

            var tile = tiles[newLocation.Row][newLocation.Col];
            if (tile == '.')
            {
                yield return this with { Location = newLocation };
                yield break;
            }

            if (tile is '/' or '\\')
            {
                yield return this with { Location = newLocation, Direction = Direction.Mirror(tile) };
                yield break;
            }

            if (tile is '|' or '-')
            {
                var splitDirections = Direction.Split(tile);

                foreach (var newDirection in splitDirections)
                {
                    yield return this with { Location = newLocation, Direction = newDirection };
                }
            }
        }
    }
    

}

public enum Direction
{
    Up, Right, Down, Left
}

public static class DirectionExtensions
{
    public static Direction Mirror(this Direction direction, char tile)
    {
        return tile switch
        {
            '/' => direction switch
            {
                Direction.Up => Direction.Right,
                Direction.Right => Direction.Up,
                Direction.Down => Direction.Left,
                Direction.Left => Direction.Down,
                _ => throw new Exception("Unexpected direction")
            },
            '\\' => direction switch
            {
                Direction.Up => Direction.Left,
                Direction.Right => Direction.Down,
                Direction.Down => Direction.Right,
                Direction.Left => Direction.Up,
                _ => throw new Exception("Unexpected direction")
            },
            _ => throw new Exception("Unexpected mirror")
        };
    }

    public static IEnumerable<Direction> Split(this Direction direction, char tile)
    {
        if (tile == '-')
        {
            switch (direction)
            {
                case Direction.Up:
                case Direction.Down:
                    yield return Direction.Left;
                    yield return Direction.Right;
                    yield break;
                case Direction.Left:
                    yield return Direction.Left;
                    yield break;
                case Direction.Right:
                    yield return Direction.Right;
                    yield break;
                default:
                    throw new Exception("Unexpected direction");                
            }
        }

        if (tile == '|')
        {
            switch (direction)
            {
                case Direction.Left:
                case Direction.Right:
                    yield return Direction.Up;
                    yield return Direction.Down;
                    yield break;
                case Direction.Up:
                    yield return Direction.Up;
                    yield break;
                case Direction.Down:
                    yield return Direction.Down;
                    yield break;
                default:
                    throw new Exception("Unexpected direction");
            }
        }

        throw new Exception("Unexpected tile");
    }
}
