using System.CommandLine;
using System.Security.Cryptography.X509Certificates;

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
        var bids = await Parse(input);
        var task = part switch
        {
            1 => Part1(bids),
            2 => Part2(bids),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IEnumerable<(char[] Cards, int Bid)> bids)
    {
        var hands = bids.Select(b => new Hand(b.Cards, StrengthCalculator.Calculate(b.Cards), b.Bid));

        var scoredHands =  hands.Order(new StandardComparer())
            .Select((h, i) => (Hand: h, Winnings: (i + 1) * h.Bid));

        var totalWinnings = scoredHands.Select(s => s.Winnings).Sum();

        Console.WriteLine(totalWinnings);

        return Task.CompletedTask;
    }

    private static Task Part2(IEnumerable<(char[] Cards, int Bid)> bids)
    {
        var hands = bids.Select(b => new Hand(b.Cards, StrengthCalculator.CalculateJokerStrength(b.Cards), b.Bid));

        var scoredHands =  hands.Order(new JokerComparer())
            .Select((h, i) => (Hand: h, Winnings: (i + 1) * h.Bid));

        var totalWinnings = scoredHands.Select(s => s.Winnings).Sum();

        Console.WriteLine(totalWinnings);

        return Task.CompletedTask;
    }

    private static async Task<List<(char[] Cards, int Bid)>> Parse(string input)
    {
        return await File.ReadLinesAsync(input)
            .Select(line =>
            {
                var parts = line.Split(' ');
                return (parts[0].ToCharArray(), Convert.ToInt32(parts[1]));
            })
            .ToListAsync();
    }
}

public record Hand (char[] Cards, Strength Strength, int Bid);

public static class StrengthCalculator
{
    private static readonly char[] NonJokerCards = "23456789TQKA".ToCharArray();

    public static Strength CalculateJokerStrength(char[] cards)
    {
        var possibleHands = CalculatePossibleHands(cards);

        var bestHand = Strength.HighCard;
        foreach (var hand in possibleHands)
        {
            var strength = Calculate(hand);
            if (strength > bestHand)
            {
                bestHand = strength;
            }
            if (bestHand == Strength.FiveOfAKind)
            {
                break;
            }
        }
        return bestHand;
    }

    private static List<char[]> CalculatePossibleHands(char[] cards)
    {
        var cardsCopy = new char[cards.Length];
        Array.Copy(cards, cardsCopy, cards.Length);
        var jokerPositions = Enumerable.Range(0, cards.Length)
            .Where(i => cards[i] == 'J');

        var possibleHands = new List<char[]>([cardsCopy]);
        
        foreach (var i in jokerPositions)
        {
            var newHands = new List<char[]>();
            foreach (var c in possibleHands)
            {
                foreach (var newCard in NonJokerCards)
                {
                    var newHand = new char[c.Length];
                    Array.Copy(c, newHand, c.Length);
                    newHand[i] = newCard;
                    newHands.Add(newHand);
                }
            }
            possibleHands = newHands;
        }

        return possibleHands;
    }

    public static Strength Calculate(char[] cards)
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

public class StandardComparer : IComparer<Hand>
{
    private static readonly Dictionary<char, int> CardValues = new()
    {
        ['2'] = 0,
        ['3'] = 1,
        ['4'] = 2,
        ['5'] = 3,
        ['6'] = 4,
        ['7'] = 5,
        ['8'] = 6,
        ['9'] = 7,
        ['T'] = 8,
        ['J'] = 9,
        ['Q'] = 10,
        ['K'] = 11,
        ['A'] = 12,
    };

    public int Compare(Hand x, Hand y)
    {
        var ordinal = x.Strength - y.Strength;
        
        if (ordinal != 0)
        {
            return ordinal;
        }

        for (var i = 0; i < x.Cards.Length; i++)
        {
            ordinal = CardValues[x.Cards[i]] - CardValues[y.Cards[i]];
            if (ordinal != 0)
            {
                return ordinal;
            }
        }

        return 0;
    }
}

public class JokerComparer : IComparer<Hand>
{
    private static readonly Dictionary<char, int> CardValues = new()
    {
        ['J'] = 0,
        ['2'] = 1,
        ['3'] = 2,
        ['4'] = 3,
        ['5'] = 4,
        ['6'] = 5,
        ['7'] = 6,
        ['8'] = 7,
        ['9'] = 8,
        ['T'] = 9,
        ['Q'] = 10,
        ['K'] = 11,
        ['A'] = 12,
    };

    public int Compare(Hand x, Hand y)
    {
        var ordinal = x.Strength - y.Strength;
        
        if (ordinal != 0)
        {
            return ordinal;
        }

        for (var i = 0; i < x.Cards.Length; i++)
        {
            ordinal = CardValues[x.Cards[i]] - CardValues[y.Cards[i]];
            if (ordinal != 0)
            {
                return ordinal;
            }
        }

        return 0;
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

