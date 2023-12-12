using System.Collections;
using System.Collections.Immutable;
using System.CommandLine;
using System.ComponentModel;
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
        var springs = Parse(input);
        var task = part switch
        {
            1 => Part1(springs),
            2 => Part2(springs),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(ImmutableArray<Springs> springsList)
    {
        var count = springsList.Select(s => Process(s.Condition, s.DamagedCounts)).Sum();

        Console.WriteLine(count);

        return Task.CompletedTask;
    }

    private static Task Part2(ImmutableArray<Springs> springsList)
    {
        var unfolded = springsList.Select(x => Unfold(x, 5));

        var completed = 0;
        var total = 0L;

        Parallel.ForEach(unfolded, springs =>
        {
            var count = Process(springs.Condition, springs.DamagedCounts);

            var localCompleted = Interlocked.Increment(ref completed);
            var localTotal = Interlocked.Add(ref total, count);

            Console.WriteLine($"{localTotal,8} ({localCompleted,4}/{springsList.Length,4})");
        });

        return Task.CompletedTask;
    }

    private static Springs Unfold(Springs input, int count)
    {
        var condition = string.Join('?', Enumerable.Range(0, count).Select(_ => input.Condition));

        var damagedCounts =  new List<int>();

        for (var i = 0; i < count; i++)
        {
            damagedCounts.AddRange(input.DamagedCounts);
        }

        return new Springs(condition, [.. damagedCounts]);
    }

    private static long Process(string springs, ImmutableArray<int> damagedCounts)
    {
        return Process(springs, [.. damagedCounts], []);
    }

    private static long Process(string springs, List<int> damagedCounts, Dictionary<string, long> cache)
    {
        var key = $"{springs}-{string.Join(',', damagedCounts.Select(x => x.ToString()))}";
        if (cache.TryGetValue(key, out var value))
        {
            return value;
        }

        value = Count(springs, damagedCounts, cache);
        cache[key] = value;
        return value;
    }

    private static long Count(string springs, List<int> damagedCounts, Dictionary<string, long> cache)
    {
        return springs.FirstOrDefault() switch {
            '.' => HandleWorking(springs, damagedCounts, cache),
            '#' => HandleDamaged(springs, damagedCounts, cache),
            '?' => HandleUnknown(springs, damagedCounts, cache),
            _ => HandleEnd(damagedCounts)
        };
    }

    private static long HandleWorking(string springs, List<int> damagedCounts, Dictionary<string, long> cache) =>
        Process(springs[1..], damagedCounts, cache);

    private static long HandleDamaged(string springs, List<int> damagedCounts, Dictionary<string, long> cache)
    {
        if (damagedCounts.Count == 0)
        {
            return 0;
        }

        var c = damagedCounts[0];
        damagedCounts = damagedCounts[1..];

        var leadingDamagedOrUnknown = springs.TakeWhile(x => x is '#' or '?').Count();

        if (leadingDamagedOrUnknown < c)
        {
            return 0;
        }
        if (springs.Length == c)
        {
            return Process("", damagedCounts, cache);
        }
        if (springs[c] == '#')
        {
            return 0;
        }
        return Process(springs[(c+1)..], damagedCounts, cache);
    }

    private static long HandleUnknown(string springs, List<int> damagedCounts, Dictionary<string, long> cache) =>
        Process("." + springs[1..], damagedCounts, cache) + Process("#" + springs[1..], damagedCounts, cache);

    private static long HandleEnd(List<int> damagedCounts) => damagedCounts.Count == 0 ? 1 : 0;

    private static bool Match(string conditions, ImmutableArray<int> damagedCounts)
    {
        var regex = SplitRegex();

        var damaged = regex.Split(conditions).Where(s => s != string.Empty);
        return damaged.Select(x => x.Length).SequenceEqual(damagedCounts);
    }

    private static IEnumerable<string> PossibleConditions(string conditions)
    {
        var unknownPositions = conditions.Select((c , i) => (Char: c, Index: i))
            .Where(x => x.Char == '?')
            .Select(x => x.Index)
            .ToArray();

        var numCombinations = Math.Pow(2, unknownPositions.Length);
        for (var i = 0; i < numCombinations; i++)
        {
            var copy = conditions.ToArray();
            for (var j = 0; j < unknownPositions.Length; j++)
            {
                var pos = unknownPositions[j];
                copy[pos] = (i & (1 << j)) == 0 ? '#' : '.';
            }
            yield return new string(copy);
        }
    }

    private static ImmutableArray<Springs> Parse(string input)
    {
        return File.ReadLinesAsync(input)
            .Select(line => line.Split(" "))
            .Select(parts => new Springs(
                parts[0],
                parts[1].Split(',').Select(x => Convert.ToInt32(x)).ToImmutableArray()
            ))
            .ToEnumerable()
            .ToImmutableArray();
    }

    [GeneratedRegex(@"\.+")]
    private static partial Regex SplitRegex();
}

public record Springs(string Condition, ImmutableArray<int> DamagedCounts);
