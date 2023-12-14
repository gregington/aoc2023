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
        var platform = await Parse(input);
        var task = part switch
        {
            1 => Part1(platform),
            2 => Part2(platform),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(char[][] platform)
    {
        var tilted = TiltNorth(platform);
        var load = CalculateLoad(tilted);

        Console.WriteLine(load);

        return Task.CompletedTask;
    }

    private static Task Part2(char[][] platform)
    {
        var cycles = 1_000_000_000;

        var (loop, startOffset) = FindLoop(platform);

        var loadAtCycles = loop[(cycles - startOffset) % loop.Count];
        Console.WriteLine(loadAtCycles);

        return Task.CompletedTask;
    }

    private static (List<int> Loop, int StartOffset) FindLoop(char[][] platform)
    {
        var platforms = new Dictionary<string, int>();

        var loads = new List<int>();


        var sequence = 0;
        var stringified = Stringify(platform);

        while (true)
        {
            if (platforms.TryGetValue(stringified, out var startLoop))
            {
                var loop = loads.Skip(startLoop).ToList();
                return (loop, startLoop);
            }

            loads.Add(CalculateLoad(platform));
            platforms.Add(stringified, sequence++);

            platform = Cycle(platform);
            stringified = Stringify(platform);
        }
    }

    private static string Stringify(char[][] platform)
    {
        return string.Join("", platform.SelectMany(x => x));
    }

    private static bool Equal(char[][] a, char[][] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i].Length != b[i].Length)
            {
                return false;
            }
            for (var j = 0; j < a[i].Length; j++)
            {
                if (a[i][j] != b[i][j])
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static char[][] Cycle(char[][] platform)
    {
        for (var i = 0; i < 4; i++)
        {
            platform = TiltNorth(platform);
            platform = RotateClockwise(platform);
        }
        return platform;
    }

    public static char[][] TiltNorth(char[][] input)
    {
        var copy = Copy(input);

        int moves;

        do
        {
            moves = 0;
            for (var row = 1; row < copy.Length; row++)
            {
                for (var col = 0; col < copy[row].Length; col++)
                {
                    var c = copy[row][col];
                    if (c is '.' or '#')
                    {
                        continue;
                    }
                    if (c != 'O')
                    {
                        throw new Exception($"Unexpected character {c} at {row},{col}");
                    }

                    var above = copy[row - 1][col];
                    if (above == '.')
                    {
                        copy[row - 1][col] = 'O';
                        copy[row][col] = '.';
                        moves++;
                    }
                }
            }
        } while (moves > 0);

        return copy;
    }

    private static int CalculateLoad(char[][] platform)
    {
        var score = 0;
        for (var row = 0; row < platform.Length; row++)
        {
            for (var col = 0; col < platform[row].Length; col++)
            {
                if (platform[row][col] == 'O')
                {
                    score += platform.Length - row;
                }
            }
        }
        return score;
    }

    private static char[][] RotateClockwise(char[][] input)
    {
        var result = new char[input[0].Length][];
        for (var i = 0; i < input[0].Length; i++)
        {
            result[i] = new char[input.Length];
            for (var j = 0; j < input.Length; j++)
            {
                result[i][j] = input[input.Length - j - 1][i];
            }
        }

        return result;
    }

    private static char[][] Copy(char[][] input)
    {
        var copy = new char[input.Length][];
        for (var i = 0; i < input.Length; i++)
        {
            copy[i] = new char[input[i].Length];
            Array.Copy(input[i], copy[i], input[i].Length);
        }
        return copy;
    }

    private static void PrintPlatform(char[][] input)
    {
        foreach (var line in input)
        {
            Console.WriteLine(line);
        }
    }

    private static async Task<char[][]> Parse(string input)
    {
        return await File.ReadLinesAsync(input)
            .Select(x => x.ToCharArray())
            .ToArrayAsync();
    }
}