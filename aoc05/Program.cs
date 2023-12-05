using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

    [GeneratedRegex(@"^seeds: (?<seeds>[0-9 ]+)$")]
    private static partial Regex SeedsRegex();

    [GeneratedRegex(@"^(?<name>[a-z-]+) map:$")]
    private static partial Regex MapRegex();

    [GeneratedRegex(@"^(?<destStart>\d+) (?<sourceStart>\d+) (?<length>\d+)$")]
    private static partial Regex RangeRegex();
}

public class Maps
{
    private readonly IDictionary<string, Map> maps;

    public Maps(IEnumerable<Map> maps)
    {
        this.maps = maps.ToDictionary(m => m.Name);
    }

    public Map SeedToSoil => maps["seed-to-soil"];

    public Map SoilToFertilizer => maps["soil-to-fertilizer"];

    public Map FertilizerToWater => maps["fertilizer-to-water"];

    public Map WaterToLight => maps["water-to-light"];

    public Map LightToTemperature => maps["light-to-temperature"];

    public Map TemperatureToHumidity => maps["temperature-to-humidity"];

    public Map HumidityToLocation => maps["humidity-to-location"];

    public long SeedToLocation(long input)
    {
        var value = SeedToSoil[input];
        value = SoilToFertilizer[value];
        value = FertilizerToWater[value];
        value = WaterToLight[value];
        value = LightToTemperature[value];
        value = TemperatureToHumidity[value];
        value = HumidityToLocation[value];
        return value;
    }
}

public class Map
{
    private readonly List<RangeMapping> ranges = [];

    public Map(string name)
    {
        Name = name;
    }

    public string Name { get; init; }

    public void AddRange(long destStart, long sourceStart, long length)
    {
        ranges.Add(new RangeMapping(destStart, sourceStart, length));
    }

   public long this[long i]
   {
        get 
        {
            foreach (var range in ranges)
            {
                if (range.Contains(i))
                {
                    return range.Map(i);
                }
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