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
        var result = patterns
            .Select(x => FindReflectionAxis(x, false))
            .Select(Score)
            .ToEnumerable()
            .Sum();
        Console.WriteLine(result);

        return Task.CompletedTask;
    }

    private static Task Part2(IAsyncEnumerable<string[]> patterns)
    {
        var result = patterns
            .Select(x => FindReflectionAxis(x, true))
            .Select(Score)
            .ToEnumerable()
            .Sum();
        Console.WriteLine(result);

        return Task.CompletedTask;
    }

    private static int Score(Axis axis)
    {
        return axis.Direction switch
        {
            'H' => axis.Index * 100,
            'V' => axis.Index,
            _ => throw new Exception("Unknown direction")
        };
    }

    public static Axis FindReflectionAxis(string[] pattern, bool smudge)
    {
        // Horizontal reflection
        var reflectionIndex = FindReflectionIndex(pattern, smudge);
        if (reflectionIndex != -1) {
            return new Axis('H', reflectionIndex);
        }

        // Vertical reflection
        var transposed = Transpose(pattern);
        var transposedReflectionIndex = FindReflectionIndex(transposed, smudge);
        if (transposedReflectionIndex == -1) {
            return null;
        }

        return new Axis('V', transposedReflectionIndex);
    }

    private static int FindReflectionIndex(string[] pattern, bool smudge)
    {
        var allowedDifferences = smudge ? 1 : 0;

        for (var i = 1; i < pattern.Length; i++)
        {
            var differences = 0;
            var lower = i - 1;
            var upper = i;

            while (lower >= 0 && upper < pattern.Length)
            {
                differences += CountDifferences(pattern[lower], pattern[upper]);

                if (differences > allowedDifferences)
                {
                    break;
                }
                lower--;
                upper++;
            }
            if (differences == allowedDifferences)
            {
                return i;
            }
        }
        return -1;
    }

    private static int CountDifferences(string a, string b)
    {
        var differences = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                differences++;
            }
        }
        return differences;
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

public record Axis(char Direction, int Index);