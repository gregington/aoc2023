using System.Collections.Immutable;
using System.CommandLine;
using System.Runtime.Versioning;

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
        var map = await Parse(input);
        var task = part switch
        {
            1 => Part1(map),
            2 => Part2(map),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(char[][] map)
    {
        var maxSteps = FindMaxSteps(map);
        Console.WriteLine(maxSteps);

        return Task.CompletedTask;
    }

    private static Task Part2(char[][] map)
    {
        return Task.CompletedTask;
    }

    private static int FindMaxSteps(char[][] map)
    {
        var start = FindStart(map);
        var end = FindEnd(map);

        return FindMaxSteps(map, end, start, []);
    }

    private static int FindMaxSteps(char[][] map, Position goal, Position position, ImmutableHashSet<Position> visited)
    {
        if (position == goal)
        {
            return 0;
        }

        visited = visited.Add(position);

        var max = int.MinValue;

        foreach (var newPosition in NewPositions(map, position, visited))
        {
            var steps = FindMaxSteps(map, goal, newPosition, visited) + 1;
            max = Math.Max(max, steps);
        }
        return max;
    }

    private static IEnumerable<Position> NewPositions(char[][] map, Position position, IReadOnlySet<Position> visited)
    {
        var c = map[position.Row][position.Col];
        var candidates = new List<Position>();
        if (c is '>' or '<' or '^' or 'v')
        {
            candidates.Add(position.Move((Direction) c));
        }
        else 
        {
            candidates.AddRange(Enum.GetValues<Direction>().Select(d => position.Move(d)));
        }

        return candidates
            .Where(p => InBounds(map, p))
            .Where(p => map[p.Row][p.Col] != '#')
            .Where(p => !visited.Contains(p));
    }

    private static void PrintMap(char[][] map)
    {
        foreach (var row in map)
        {
            Console.WriteLine(new string(row));
        }
    }

    private static bool InBounds(char[][] map, Position p) =>
        p.Row >= 0 && p.Row < map.Length && p.Col >= 0 && p.Col < map[0].Length;

    private static Position FindStart(char[][] map)
    {
        var charPos = FindSinglePath(map[0]);
        return new Position(0, charPos);
    }

    private static Position FindEnd(char[][] map)
    {
        var charPos = FindSinglePath(map[^1]);
        return new Position(map.Length - 1, charPos);
    }

    private static int FindSinglePath(char[] row) {
        var pos = -1;
        for (var i = 0; i < row.Length; i++)
        {
            if (row[i] == '.')
            {
                if (pos != -1)
                {
                    throw new Exception("Found multiple '.'");
                }
                pos = i;
            }
        }
        return pos;
    }

    private static async Task<char[][]> Parse(string filename)
    {
        return await File.ReadLinesAsync(filename)
            .Select(line => line.ToArray())
            .ToArrayAsync();
    }
}

public record State(Position Position, ImmutableHashSet<Position> Visited);

public record Position(int Row, int Col)
{
    public Position Move(Direction direction)
    {
        return direction switch
        {
            Direction.Up => this with { Row = Row - 1 },
            Direction.Right => this with { Col = Col + 1 },
            Direction.Down => this with { Row = Row + 1 },
            Direction.Left => this with { Col = Col - 1 },
            _ => throw new Exception("Unknown direction")
        };
    }
}

public enum Direction
{
    Up = '^',
    Right = '>',
    Down = 'v',
    Left = '<',
}
