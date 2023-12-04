using System.Collections.Immutable;
using System.CommandLine;
using System.Globalization;
using System.Text.RegularExpressions;

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
        var cards = File.ReadLinesAsync(input)
            .Select(x => Card.Parse(x));

        var task = part switch
        {
            1 => Part1(cards),
            2 => Part2(cards),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IAsyncEnumerable<Card> cards)
    {
        var totalScore = cards.Select(x => x.Score()).ToEnumerable().Sum();
        Console.WriteLine(totalScore);
        return Task.CompletedTask;
    }

    private static async Task Part2(IAsyncEnumerable<Card> cards)
    {
        var cardDict = await cards.ToDictionaryAsync(x => x.Id, x => x);

        var queue = new Queue<Card>(cardDict.Values.OrderBy(x => x.Id));

        int count = 0;
        while (queue.Count > 0)
        {
            var card = queue.Dequeue();
            count++;

            var newIds = Enumerable.Range(card.Id + 1, card.Matches.Count)
                .Where(x => cardDict.ContainsKey(x))
                .Select(x => cardDict[x]);

            foreach (var newCard in newIds)
            {
                queue.Enqueue(newCard);
            }
        }

        Console.WriteLine(count);
    }
}

public partial record Card(int Id, ImmutableHashSet<int> WinningNumbers, ImmutableHashSet<int> Numbers, ImmutableHashSet<int> Matches)
{
    private static StringSplitOptions splitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    [GeneratedRegex(@"^Card +(?<id>\d+): +(?<winningNumbers>(\d+ *)+) \| +(?<numbers>(\d+ *)+)$")]
    private static partial Regex CardRegex();

    public static Card Parse(string line)
    {
        var regex = CardRegex();
        var match = regex.Match(line);
        var id = int.Parse(match.Groups["id"].Value);
        var winningNumbers = match.Groups["winningNumbers"].Value.Split(' ', splitOptions).Select(int.Parse).ToImmutableHashSet();
        var numbers = match.Groups["numbers"].Value.Split(' ', splitOptions).Select(int.Parse).ToImmutableHashSet();
        var matches = winningNumbers.Intersect(numbers).ToImmutableHashSet();

        return new Card(id, winningNumbers, numbers, matches);
    }

    public int Score()
    {
        var count = Matches.Count;

        var score = count == 0
            ? 0
            : 1 << (count - 1);

        return score;
    }
}
