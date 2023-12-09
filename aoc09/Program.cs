using System.Collections.Immutable;
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
        var sequences = Parse(input);
        var task = part switch
        {
            1 => Part1(sequences),
            2 => Part2(sequences),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IAsyncEnumerable<List<int>> sequences)
    {
        var nextValues = sequences
            .Select(NextValue);

        Console.WriteLine(nextValues.ToEnumerable().Sum());

        return Task.CompletedTask;
    }

    private static Task Part2(IAsyncEnumerable<IList<int>> sequences)
    {
        var previousValues = sequences
            .Select(PreviousValue);

        Console.WriteLine(previousValues.ToEnumerable().Sum());

        return Task.CompletedTask;
    }

    private static int NextValue(IList<int> sequence)
    {
        return AppendSequence([.. sequence])[^1];
    }

    private static ImmutableArray<int> AppendSequence(ImmutableArray<int> sequence)
    {
        if (sequence.All(x => x == 0))
        {
            return sequence.Add(0);
        }
        var differences = new List<int>();
        for (int i = 1; i < sequence.Length; i++)
        {
            differences.Add(sequence[i] - sequence[i - 1]);
        }

        var lowerSequence = AppendSequence([.. differences]);
        return sequence.Add(sequence[^1] + lowerSequence[^1]);
    }

    private static int PreviousValue(IList<int> sequence)
    {
        return PrependSequence([.. sequence])[0];
    }

    private static ImmutableArray<int> PrependSequence(ImmutableArray<int> sequence)
    {
        if (sequence.All(x => x == 0))
        {
            return sequence.Add(0);
        }
        var differences = new List<int>();

        for (int i = 1; i < sequence.Length; i++)
        {
            differences.Add(sequence[i] - sequence[i - 1]);
        }

        var lowerSequence = PrependSequence([.. differences]);
        var value = sequence[0] - lowerSequence[0];
        return [value, .. sequence];
    }

    public static IAsyncEnumerable<List<int>> Parse(string input)
    {
        return File.ReadLinesAsync(input)
            .Select(line => line.Split(" ").Select(x => Convert.ToInt32(x))
            .ToList());
    }
}
