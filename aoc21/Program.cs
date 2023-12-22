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
        var (map, start) = await Parse(input);
        var task = part switch
        {
            1 => Part1(map, start),
            2 => Part2(map, start),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(char[][] map, Position start)
    {
        IEnumerable<Position> positions = new [] { start };

        for (var i = 0; i < 64; i++)
        {
            positions = positions.SelectMany(p => Step(map, p)).Distinct();
        }
        Console.WriteLine(positions.Count());

        return Task.CompletedTask;
    }

    private static Task Part2(char[][] map, Position start)
    {
        IEnumerable<Position> positions = new [] { start };

        var totalSteps = 26501365;
        var fullWidth = map[0].Length;
        var halfWidth = fullWidth / 2;

        var gridTraverals = (totalSteps - halfWidth) / fullWidth;
        Console.WriteLine(gridTraverals);

        // It takes half width steps to get to a new square, then
        // increments of full width after that. Calculate the first three
        // values of the sequence to get the formula

        var firstTerms = Enumerable.Range(0, 3)
            .Select(i => StepInfinite(map, start, i * fullWidth + halfWidth).Count())
            .ToArray();

        var (a, b, c) = QuadraticCoefficients(firstTerms);
        Console.WriteLine($"{a}, {b}, {c}");

        var numPositions = (a * gridTraverals * gridTraverals) + b * gridTraverals + c;
        Console.WriteLine(numPositions);

        return Task.CompletedTask;
    }

    private static (long A, long B, long C) QuadraticCoefficients(int[] sequence)
    {
        var a = (sequence[2] - (2 * sequence[1]) + sequence[0]) / 2;
        var b = sequence[1] - sequence[0] - a;
        var c = sequence[0];

        return (a, b, c);
    }

    private static IEnumerable<Position> StepInfinite(char[][] map, Position start, int numSteps)
    {
        IEnumerable<Position> positions = new [] { start };

        for (var i = 0; i < numSteps; i++)
        {
            positions = positions.SelectMany(p => StepInfinite(map, p)).Distinct().ToList();
        }
        return positions;
    }

    private static IEnumerable<Position> StepInfinite(char[][] map, Position position)
    {
        return CandidateSteps(position)
            .Where(p => map[Mod(p.Row, map.Length)][Mod(p.Col, map[0].Length)] == '.');
    }

    private static IEnumerable<Position> Step(char[][] map, Position position)
    {
        return CandidateSteps(position)
            .Where(p => InBounds(map, p))
            .Where(p => map[p.Row][p.Col] == '.');
    }

    private static bool InBounds(char[][] map, Position p) =>
        p.Row >= 0 && p.Row < map.Length && p.Col >= 0 && p.Col < map[0].Length;


    private static IEnumerable<Position> CandidateSteps(Position p) {
        yield return p with { Row = p.Row - 1};
        yield return p with { Row = p.Row + 1};
        yield return p with { Col = p.Col - 1};
        yield return p with { Col = p.Col + 1};        
    }

    private static void Print(char[][] map, IEnumerable<Position> positions)
    {
        var copy = CopyMap(map);
        foreach (var (row, col) in positions)
        {
            copy[row][col] = 'O';
        }

        foreach (var row in copy)
        {
            Console.WriteLine(new string(row));
        }
    }

    private static void PrintInfinite(char[][] map, IEnumerable<Position> positions)
    {
        var positionSet = positions.ToHashSet();

        var minRow = Math.Min(0, positions.Select(p => p.Row).Min());
        var maxRow = Math.Max(map.Length - 1, positions.Select(p => p.Row).Max());
        var minCol = Math.Min(0, positions.Select(p => p.Col).Min());
        var maxCol = Math.Max(map[0].Length - 1, positions.Select(p => p.Col).Max());

        for (var row = minRow; row < maxRow + 1; row++)
        {
            for (var col = minCol; col < maxCol + 1; col++)
            {
                if (positionSet.Contains(new Position(row, col)))
                {
                    Console.Write('O');
                }
                else
                {
                    Console.Write(map[Mod(row, map.Length)][Mod(col, map[0].Length)]);
                }
            }
            Console.WriteLine();
        }
    }

    private static int Mod(int x, int m) {
        return (x % m + m) % m;
    }

    private static char[][] CopyMap(char[][] map)
    {
        return map.Select(row => 
        {
            var newRow = new char[row.Length];
            Array.Copy(row, newRow, row.Length);
            return newRow;
        }).ToArray();
    }

    private static async Task<(char[][] Map, Position start)> Parse(string input)
    {
        var map = await File.ReadLinesAsync(input)
            .Select(line => line.ToArray())
            .ToArrayAsync();

        Position? start = null;
        for (var row = 0; row < map.Length; row++)
        {
            for (var col = 0; col < map[row].Length; col++)
            {
                var c = map[row][col];
                if (c == 'S')
                {
                    start = new Position(row, col);
                    map[row][col] = '.';
                    goto exit;
                }
            }
        }
        exit:
        return (map, start!);
    }
}

public record Position(int Row, int Col);

