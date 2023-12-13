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
        var patterns = Parse(input);
        var task = part switch
        {
            1 => Part1(patterns),
            2 => Part2(patterns),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IAsyncEnumerable<string[]> patterns)
    {
        var result = patterns.Select(Score).ToEnumerable().Sum();
        Console.WriteLine(result);

        return Task.CompletedTask;
    }

    private static Task Part2(IAsyncEnumerable<string[]> patterns)
    {
        return Task.CompletedTask;
    }

    private static int Score(string[] pattern)
    {
        // Horizontal reflection
        var reflectionIndex = FindReflectionIndex(pattern);
        if (reflectionIndex != -1) {
            return reflectionIndex * 100;
        }

        // Vertical reflection
        var transposed = Transpose(pattern);
        var transposedReflectionIndex = FindReflectionIndex(transposed);
        if (transposedReflectionIndex == -1) {
            throw new Exception("Expected reflection in horizonal or vertical axis");
        }

        return transposedReflectionIndex;
    }


    private static int FindReflectionIndex(string[] pattern)
    {
        var equalLines = new List<(int First, int Second)>();

        for (var i = 0; i < pattern.Length; i++)
        {
            for (var j = i + 1; j < pattern.Length; j++)
            {
                if (pattern[i] == pattern[j])
                {
                    equalLines.Add((i, j));
                }
            }
        }

        var reflectionAxis = equalLines
            .Where(x => x.Second - x.First == 1)
            .Where(x => IsReflection(pattern, x.First, x.Second))
            .ToArray();

        if (reflectionAxis.Length != 1)
        {
            return -1;
        }

        return reflectionAxis[0].Second;
    }

    private static bool IsReflection(string[] pattern, int lower, int upper)
    {
        while (lower >= 0 && upper < pattern.Length)
        {
            if (pattern[lower] != pattern[upper])
            {
                return false;
            }

            lower--;
            upper++;
        }
        return true;
    }

    private static string[] Transpose(string[] input)
    {
        var transposed = new char[input[0].Length][];
        for (var col = 0; col < input[0].Length; col++)
        {
            transposed[col] = new char[input.Length];
            for (var row = 0; row < input.Length; row++)
            {
                transposed[col][row] = input[row][col];
            }
        }

        return transposed.Select(x => new string(x)).ToArray();
    }

    private async static IAsyncEnumerable<string[]> Parse(string input)
    {
        var lines = new List<string>();

        await foreach (var line in File.ReadLinesAsync(input))
        {
            if (line.Trim() == string.Empty)
            {
                if (lines.Count > 0)
                {
                    yield return lines.ToArray();
                    lines.Clear();
                }
                continue;
            }

            lines.Add(line);
        }

        if (lines.Count > 0)
        {
            yield return lines.ToArray();
        }
    }
}
