using System.Collections;
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
        var initSequence = await Parse(input);
        var task = part switch
        {
            1 => Part1(initSequence),
            2 => Part2(initSequence),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IEnumerable<string> initSequence)
    {
        var hashes = initSequence.Select(Hash);
        Console.WriteLine(hashes.Sum());
        return Task.CompletedTask;
    }

    private static Task Part2(IEnumerable<string> initSequence)
    {
        return Task.CompletedTask;
    }

    private static int Hash(string input)
    {
        return input.Select(c => (int) c)
            .Aggregate(0, (acc, c) =>
            {
                var hash = acc + c;
                hash *= 17;
                return hash % 256;
            });
    }

    private static async Task<IEnumerable<string>> Parse(string input)
    {
        return (await File.ReadAllLinesAsync(input)).First().Split(',');
    }
}