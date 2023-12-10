using System.Collections.Frozen;
using System.Collections.Immutable;
using System.CommandLine;
using System.Reflection;

public partial class Program
{
    private static FrozenDictionary<Direction, FrozenSet<char>> DirectionSymbols =
        new Dictionary<Direction, FrozenSet<char>>
        {
            [Direction.Up] = "|F7".ToFrozenSet(),
            [Direction.Down] = "|JL".ToFrozenSet(),
            [Direction.Left] = "-LF".ToFrozenSet(),
            [Direction.Right] = "-J7".ToFrozenSet()
        }.ToFrozenDictionary();

    // Only use bottom half corners, as we can move though parallel pipes
    private static FrozenSet<char> VerticalPipes = "|LJ".ToFrozenSet();

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

    private static async Task<char[][]>Parse(string input)
    {
        return await File.ReadLinesAsync(input)
            .Select(line => line.ToCharArray())
            .ToArrayAsync();
    }

    private static Task Part1(char[][] map)
    {
        var visited = new HashSet<Point>();
        var start = FindStart(map);
        var point = start;
        var startSymbol = InferSymbol(map, point);
        map[point.Row][point.Col] = startSymbol;

        while (point != null)
        {
            visited.Add(point);
            point = FindAdjacent(map, point)
                .Where(p => !visited.Contains(p))
                .FirstOrDefault();
        }

        var filteredMap = FilterMap(map, visited);
        PrintMap(filteredMap);
        Console.WriteLine();

        Console.WriteLine(visited.Count / 2);
        return Task.CompletedTask;
    }

    private static Task Part2(char[][] map)
    {
        var visited = new HashSet<Point>();
        var start = FindStart(map);
        var point = start;
        var startSymbol = InferSymbol(map, point);
        map[point.Row][point.Col] = startSymbol;

        while (point != null)
        {
            visited.Add(point);
            point = FindAdjacent(map, point)
                .Where(p => !visited.Contains(p))
                .FirstOrDefault();
        }


        var filteredMap = FilterMap(map, visited);
        PrintMap(filteredMap);
        var nonLoopPoints = FindNonLoopPoints(filteredMap, visited);
        var insidePoints = FindInsidePoints(filteredMap, nonLoopPoints);

        foreach (var insidePoint in insidePoints)
        {
            filteredMap[insidePoint.Row][insidePoint.Col] = 'I';
        }

        Console.WriteLine("--------");

        PrintMap(filteredMap);

        Console.WriteLine(insidePoints.Count());
        return Task.CompletedTask;
    }

    private static IEnumerable<Point> FindInsidePoints(char[][] map, IEnumerable<Point> points)
    {
        foreach (var point in points)
        {
            var row = map[point.Row];
            var rightIntersectionCount = 0;
            for (var col = point.Col; col < row.Length; col++)
            {
                if (VerticalPipes.Contains(row[col]))
                {
                    rightIntersectionCount++;
                }
            }

            if (rightIntersectionCount % 2 == 1)
            {
                yield return point;
            }
        }
    }

    private static IEnumerable<Point> FindNonLoopPoints(char[][] map, IEnumerable<Point> loop)
    {
        for (var row = 0; row < map.Length; row++)
        {
            for (var col = 0; col < map[0].Length; col++)
            {
                var point = new Point(row, col);
                if (!loop.Contains(point))
                {
                    yield return point;
                }
            }
        }
    }

    private static IEnumerable<Point> FindAdjacent(char[][] map, Point point)
    {
        var here = map[point.Row][point.Col];

        // Up
        if (DirectionSymbols[Direction.Down].Contains(here)
            && point.Row > 0 && DirectionSymbols[Direction.Up].Contains(map[point.Row - 1][point.Col]))
        {
            yield return point with { Row = point.Row - 1};
        }
        
        // Down
        if (DirectionSymbols[Direction.Up].Contains(here)
            && point.Row < map.Length - 1 && DirectionSymbols[Direction.Down].Contains(map[point.Row + 1][point.Col]))
        {
            yield return point with { Row = point.Row + 1};
        }

        // Left
        if (DirectionSymbols[Direction.Right].Contains(here)
            && point.Col > 0 && DirectionSymbols[Direction.Left].Contains(map[point.Row][point.Col - 1]))
        {
            yield return point with { Col = point.Col - 1 };
        }

        // Right
        if (DirectionSymbols[Direction.Left].Contains(here)
            && point.Col < map[point.Row].Length - 1 && DirectionSymbols[Direction.Right].Contains(map[point.Row][point.Col + 1]))
        {
            yield return point with { Col = point.Col + 1};
        }
    }

    private static char InferSymbol(char[][] map, Point point)
    {
        var up = point.Row > 0 ? map[point.Row - 1][point.Col] as char? : null;

        var down = point.Row < map.Length - 1 ? map[point.Row + 1][point.Col] as char? : null;

        var left = point.Col > 0 ? map[point.Row][point.Col - 1] as char? : null;

        var right = point.Col < map[point.Row].Length - 1 ? map[point.Row][point.Col + 1] as char? : null;

        if (up != null && DirectionSymbols[Direction.Up].Contains(up.Value)
            && left != null && DirectionSymbols[Direction.Left].Contains(left.Value))
        {
            return 'J';
        }

        if (up != null && DirectionSymbols[Direction.Up].Contains(up.Value)
            && right != null && DirectionSymbols[Direction.Right].Contains(right.Value))
        {
            return 'L';
        }

        if (down != null && DirectionSymbols[Direction.Down].Contains(down.Value) 
            && left != null && DirectionSymbols[Direction.Left].Contains(left.Value))
        {
            return '7';
        }

        if (down != null && DirectionSymbols[Direction.Down].Contains(down.Value)
            && right != null && DirectionSymbols[Direction.Right].Contains(right.Value))
        {
            return 'F';
        }

        if (left != null && DirectionSymbols[Direction.Left].Contains(left.Value)
            && right != null && DirectionSymbols[Direction.Right].Contains(right.Value))
        {
            return '-';
        }

        if (up != null && DirectionSymbols[Direction.Up].Contains(up.Value)
            && down != null && DirectionSymbols[Direction.Down].Contains(down.Value))
        {
            return '|';
        }

        throw new Exception("Could not infer symbol");
    }

    private static char[][] FilterMap(char[][] map, IReadOnlySet<Point> points)
    {
        var newMap = Enumerable.Range(0, map.Length)
            .Select(_ => Enumerable.Range(0, map[0].Length).Select(_ => '.').ToArray())
            .ToArray();

        foreach (var point in points)
        {
            var (row, col) = point;
            newMap[row][col] = map[row][col];
        }
        return newMap;
    }

    private static void PrintMap(char[][] map)
    {
        for (var row = 0; row < map.Length; row++)
        {
            for (var col = 0; col < map[row].Length; col++)
            {
                Console.Write(map[row][col]);
            }
            Console.WriteLine();
        }
    }

    private static Point FindStart(char[][] map)
    {
        for (var r = 0; r < map.Length; r++)
        {
            for (var c = 0; c < map[r].Length; c++)
            {
                if (map[r][c] == 'S')
                {
                    return new Point(r, c);
                }
            }
        }

        throw new Exception("Start not found");
    }
}

public record Point(int Row, int Col);

public enum Direction
{
    Up,
    Down,
    Left,
    Right
}
