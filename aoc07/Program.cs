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
        var hands = await Parse(input);
        var task = part switch
        {
            1 => Part1(hands),
            2 => Part2(),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IEnumerable<Hand> hands)
    {
        var scoredHands =  hands.Order()
            .Select((h, i) => (Hand: h, Winnings: (i + 1) * h.Bid));

        var totalWinnings = scoredHands.Select(s => s.Winnings).Sum();

        Console.WriteLine(totalWinnings);

        return Task.CompletedTask;
    }

    private static Task Part2()
    {
        return Task.CompletedTask;
    }

    private static async Task<List<Hand>> Parse(string input)
    {
        return await File.ReadLinesAsync(input)
            .Select(line =>
            {
                var parts = line.Split(' ');
                return new Hand(parts[0], Convert.ToInt32(parts[1]));
            })
            .ToListAsync();
    }
}

public class Hand : IComparable<Hand>
{
    private static Dictionary<char, int> CardValues = new()
    {
        ['A'] = 12,
        ['K'] = 11,
        ['Q'] = 10,
        ['J'] = 9,
        ['T'] = 8,
        ['9'] = 7,
        ['8'] = 6,
        ['7'] = 5,
        ['6'] = 4,
        ['5'] = 3,
        ['4'] = 2,
        ['3'] = 1,
        ['2'] = 0,
    };

    public Hand (string cards, int bid)
    {
        Cards = cards.ToCharArray();
        Bid = bid;
        Strength = CalculateStrength(Cards);
    }

    public char[] Cards { get; init; }

    public int Bid;

    public Strength Strength { get; init; }

    public int CompareTo(Hand other)
    {
        var ordinal = Strength - other.Strength;
        
        if (ordinal != 0)
        {
            return ordinal;
        }

        for (var i = 0; i < Cards.Length; i++)
        {
            ordinal = CardValues[Cards[i]] - CardValues[other.Cards[i]];
            if (ordinal != 0)
            {
                return ordinal;
            }
        }

        return 0;
    }


    public static Strength CalculateStrength(char[] cards)
    {
        var counts = new Dictionary<char, int>();
        foreach (var c in cards)
        {
            var count = counts.GetValueOrDefault(c, 0);
            count++;
            counts[c] = count;
        }

        if (counts.ContainsValue(5))
        {
            return Strength.FiveOfAKind;
        }

        if (counts.ContainsValue(4))
        {
            return Strength.FourOfAKind;
        }

        if (counts.ContainsValue(3))
        {
            if (counts.ContainsValue(2))
            {
                return Strength.FullHouse;
            }
            return Strength.ThreeOfAKind;
        }

        var twoCounts = counts.Values.Where(x => x == 2).Count();

        if (twoCounts == 2)
        {
            return Strength.TwoPair;
        }

        if (twoCounts == 1)
        {
            return Strength.OnePair;
        }

        return Strength.HighCard;
    }

}

public enum Strength
{
    HighCard = 0,
    OnePair = 1,
    TwoPair = 2,
    ThreeOfAKind = 3,
    FullHouse = 4,
    FourOfAKind = 5,
    FiveOfAKind = 6
}

