using System.Collections.Frozen;
using System.CommandLine;
using System.Text.RegularExpressions;

partial class Program
{
    private const string NumbersRegex = @"\d";

    private const string NumbersAndWordsRegex = @"\d|one|two|three|four|five|six|seven|eight|nine";

    private static FrozenDictionary<string, string> NUMBER_MAP = new Dictionary<string, string>
    {
        ["one"] = "1",
        ["two"] = "2",
        ["three"] = "3",
        ["four"] = "4",
        ["five"] = "5",
        ["six"] = "6",
        ["seven"] = "7",
        ["eight"] = "8",
        ["nine"] = "9"
    }.ToFrozenDictionary();

    public static async Task Main(string[] args)
    {
        var inputOption = new Option<string>(
            name: "--input",
            description: "Input file path",
            getDefaultValue: () => "input.txt");

        var includeWordsOption = new Option<bool>(
            name: "--include-words",
            description: "include digits spelled as numbers",
            getDefaultValue: () => false);

        var rootCommand = new RootCommand();
        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(includeWordsOption);

        rootCommand.SetHandler(Run, inputOption, includeWordsOption);

        await rootCommand.InvokeAsync(args);
    }

    private static async Task Run(string input, bool includeWords)
    {
        var baseRegex = includeWords ? NumbersAndWordsRegex : NumbersRegex;

        var firstRegex = new Regex($@"^.*?(?<number>{baseRegex}).*$");
        var lastRegex = new Regex($@"^.*(?<number>{baseRegex}).*$");

        var lines = File.ReadLinesAsync(input);

        var sum = await lines
            .Select(line =>
            {
                var first = firstRegex.Match(line).Groups["number"].Value;
                first = NUMBER_MAP.TryGetValue(first, out string? value) ? value : first;
                var last = lastRegex.Match(line).Groups["number"].Value;
                last = NUMBER_MAP.TryGetValue(last, out value) ? value : last;
                return int.Parse($"{first}{last}");
            })
            .AggregateAsync((a, b) => a + b);

        Console.WriteLine(sum);
    }
}


