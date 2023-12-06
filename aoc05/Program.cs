using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Parsing;
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
        var (seeds, maps) = await Parse(input);
        var task = part switch
        {
            1 => Part1(seeds, maps),
            2 => Part2(seeds, maps),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(long[] seeds, Maps maps)
    {
        var locations = seeds.Select(maps.SeedToLocation).ToArray();
        var minLocation = locations.Min();
        Console.WriteLine($"Min location: {minLocation}");

        return Task.CompletedTask;
    }

    private static Task Part2(long[] seeds, Maps maps)
    {
        var seedRanges = SeedsToRanges(seeds);
        var totalLength = seedRanges.Select(r => r.Length).Sum();
        Console.WriteLine($"Total length: {totalLength}");
        Console.WriteLine($"Seed ranges: {seedRanges.Length}");

        var seedRangesCompleted = 0;

        var mins = new ConcurrentBag<long>();

        Parallel.ForEach(seedRanges, range =>
        {
            var min = long.MaxValue;
            var start = range.Start;
            var end = range.Start + range.Length;
            for (var i = start; i < end; i++)
            {
                var location = maps.SeedToLocation(i);
                if (location < min)
                {
                    min = location;
                }
            }
            
            mins.Add(min);
            Interlocked.Increment(ref seedRangesCompleted);
            Console.WriteLine($"Completed: {seedRangesCompleted}/{seedRanges.Length}");
        });

        var min = mins.Min();

        Console.WriteLine($"Min location: {min}");

        return Task.CompletedTask;
    }

    private static async Task<(long[] Seeds, Maps Maps)> Parse(string input)
    {
        var lines = await File.ReadAllLinesAsync(input);
        var seedsRegex = SeedsRegex();
        var mapRegex = MapRegex();
        var rangeRegex = RangeRegex();

        long[] seeds = [];
        Map? currentMap = null;
        List<Map> maps = [];

        foreach (var line in lines)
        {
            if (seedsRegex.IsMatch(line))
            {
                var seedsStr = seedsRegex.Match(line).Groups["seeds"].Value;
                seeds = seedsStr.Split(' ').Select(long.Parse).ToArray();
                continue;
            }

            if (mapRegex.IsMatch(line))
            {
                var name = mapRegex.Match(line).Groups["name"].Value;
                currentMap = new Map(name);
                maps.Add(currentMap);
                continue;
            }

            if (rangeRegex.IsMatch(line))
            {
                var match = rangeRegex.Match(line);
                var destStart = long.Parse(match.Groups["destStart"].Value);
                var sourceStart = long.Parse(match.Groups["sourceStart"].Value);
                var length = long.Parse(match.Groups["length"].Value);
                currentMap!.AddRange(destStart, sourceStart, length);
                continue;
            }
        }

        return (seeds, new Maps(maps));
    }

    private static (long Start, long Length)[] SeedsToRanges(long[] seeds)
    {
        var ranges = new List<(long Start, long Length)>();

        for (int i = 0; i < seeds.Length; i += 2)
        {
            var start = seeds[i];
            var length = seeds[i + 1];
            ranges.Add((start, length));
        }

        return [.. ranges];
    }

    [GeneratedRegex(@"^seeds: (?<seeds>[0-9 ]+)$")]
    private static partial Regex SeedsRegex();

    [GeneratedRegex(@"^(?<name>[a-z-]+) map:$")]
    private static partial Regex MapRegex();

    [GeneratedRegex(@"^(?<destStart>\d+) (?<sourceStart>\d+) (?<length>\d+)$")]
    private static partial Regex RangeRegex();
}

public class Maps
{
    private readonly Map[] maps;

    public Maps(IEnumerable<Map> maps)
    {
        var mapsByName = maps.ToDictionary(m => m.Name);

        this.maps = new Map[]
        {
            mapsByName["seed-to-soil"],
            mapsByName["soil-to-fertilizer"],
            mapsByName["fertilizer-to-water"],
            mapsByName["water-to-light"],
            mapsByName["light-to-temperature"],
            mapsByName["temperature-to-humidity"],
            mapsByName["humidity-to-location"],
        };
    }

    public long SeedToLocation(long input)
    {
        var value = input;
        for (var i = 0; i < maps.Length; i++)
        {
            value = maps[i][value];
        }
        return value;
    }
}

public class Map
{
    private readonly List<RangeMapping> ranges = [];
    private List<long> sourceStarts = [];

    public Map(string name)
    {
        Name = name;
    }

    public string Name { get; init; }

    public void AddRange(long destStart, long sourceStart, long length)
    {
        ranges.Add(new RangeMapping(destStart, sourceStart, length));
        ranges.Sort((a, b) => a.SourceStart.CompareTo(b.SourceStart));
        sourceStarts = ranges.Select(r => r.SourceStart).ToList();
    }

   public long this[long i]
   {
        get 
        {
            var index = sourceStarts.BinarySearch(i);
            if (index < 0)
            {
                index = (~index) - 1;
            }

            if (index == -1)
            {
                return i;
            }

            var range = ranges[index];
            if (range.Contains(i))
            {
                return range.Map(i);
            }
            return i;
        }
   }
}

public record RangeMapping(long DestStart, long SourceStart, long Length)
{
    public bool Contains(long value) => value >= SourceStart && value < SourceStart + Length;

    public long Map(long value) => value - SourceStart + DestStart;
}