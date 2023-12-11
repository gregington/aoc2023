using System.CommandLine;

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
        var expandedMap = Expand(map);
        var galaxies = FindGalaxies(expandedMap);
        var pairs = MakePairs(galaxies);
        var distances = pairs.Select(d => ManhattanDistance(d.First, d.Second));

        Console.WriteLine(distances.Sum());

        return Task.CompletedTask;
    }

    private static Task Part2(char[][] map)
    {
        var expansionFactor = 1_000_000;
        var emptyRows = FindEmptyRows(map).Order().ToList();
        var emptyCols = FindEmptyCols(map).Order().ToList();
        
        var galaxies = FindGalaxies(map);
        var pairs = MakePairs(galaxies);

        var distances = pairs.Select(d => ExpandedManhattanDistance(d.First, d.Second, expansionFactor, emptyRows, emptyCols));

        var distanceSum = distances.Sum();

        Console.WriteLine(distanceSum);

        return Task.CompletedTask;
    }

    private static long ExpandedManhattanDistance(Point first, Point second, long expansionFactor, List<int> emptyRows, List<int> emptyCols)
    {
        return ExpandedLinearDistance(first.Row, second.Row, expansionFactor, emptyRows)
            + ExpandedLinearDistance(first.Col, second.Col, expansionFactor, emptyCols);
    }

    private static long ExpandedLinearDistance(int first, int second, long expansionFactor, List<int> emptyPositions)
    {
        var minPos = Math.Min(first, second);
        var maxPos = Math.Max(first, second);

        var crossedEmptyPositions = emptyPositions.Where(x => x > minPos && x < maxPos);
        var crossedPositionsCount = crossedEmptyPositions.Count();

        var baseDistance = maxPos - minPos - crossedPositionsCount;
        var expandedDistance = crossedPositionsCount * expansionFactor;

        return baseDistance + expandedDistance;
    }

    private static int ManhattanDistance(Point a, Point b) => Math.Abs(a.Row - b.Row) + Math.Abs(a.Col - b.Col);

    private static IEnumerable<Pair> MakePairs(IEnumerable<Point> galaxies) => 
        galaxies.SelectMany(a => galaxies.Where(b => a != b).Select(b => new Pair(a, b)))
            .Distinct();

    private static IEnumerable<Point> FindGalaxies(char[][] map)
    {
        for (var row = 0; row < map.Length; row++)
        {
            for (var col = 0; col < map[row].Length; col++) {
                if (map[row][col] == '#') {
                    yield return new Point(row, col);
                }
            }
        }
    }

    private static char[][] Expand(char[][] map)
    {
        var emptyRows = FindEmptyRows(map);
        var emptyCols = FindEmptyCols(map);

        // Convert to lists to make inserts easier
        var mapAsLists = map.Select(row => row.ToList()).ToList();

        // Insert empty in reverse order, that doesn't stuff up numbering
        foreach (var row in emptyRows.Order().Reverse())
        {
            mapAsLists.Insert(row, Enumerable.Range(0, mapAsLists[0].Count).Select(_ => '.').ToList());
        }

        foreach (var col in emptyCols.Order().Reverse())
        {
            foreach (var row in mapAsLists)
            {
                row.Insert(col, '.');
            }
        }

        return mapAsLists.Select(x => x.ToArray()).ToArray();
    }

    private static IEnumerable<int> FindEmptyRows(char[][] map)
    {
        for (var row = 0; row < map.Length; row++)
        {
            if (map[row].All(c => c == '.'))
            {
                yield return row;
            }
        }
    }

    private static IEnumerable<int> FindEmptyCols(char[][] map)
    {
        for (var col = 0; col < map[0].Length; col++)
        {
            var empty = true;
            for (var row = 0; row < map.Length; row++)
            {
                if (map[row][col] != '.')
                {
                    empty = false;
                    break;
                }
            }
            if (empty)
            {
                yield return col;
            }
        }
    }

    private static void PrintMap(char[][] map)
    {
        foreach (var row in map)
        {
            var rowStr = new string(row);
            Console.WriteLine(rowStr);
        }
    }

    private static async Task<char[][]>Parse(string input)
    {
        return await File.ReadLinesAsync(input)
            .Select(line => line.ToCharArray())
            .ToArrayAsync();
    }
}

public record Point(int Row, int Col) : IComparable<Point>
{
    public int CompareTo(Point other)
    {
        var diff = Row - other.Row;
        if (diff != 0)
        {
            return diff;
        }
        return Col - other.Col;
    }

    public override string ToString() => $"({Row}, {Col})";
}

public class Pair
{
    public Pair(Point first, Point second)
    {
        var points = new [] { first, second }.Order().ToArray();
        First = points[0];
        Second = points[1];
    }
    public Point First { get; init; }

    public Point Second { get; init; }

    public override string ToString() => $"[{First} {Second}]";

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (Pair) obj;        
        return First.Equals(other.First) && Second.Equals(other.Second);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(First);
        hash.Add(Second);
        return hash.ToHashCode();
    }
}
