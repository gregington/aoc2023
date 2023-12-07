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
        var races = Parse(input);
        var task = part switch
        {
            1 => Part1(races),
            2 => Part2(races),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IEnumerable<Race> races)
    {
        var totalWaysToBreakRecord = races.Select(race =>
            Simulate(race)
                .Where(result => result.Distance > result.Race.DistanceRecord)
                .Count());
        var product = totalWaysToBreakRecord.Aggregate((a, b) => a * b);

        Console.WriteLine(product);

        return Task.CompletedTask;
    }

    private static Task Part2(IEnumerable<Race> races)
    {
        var time = Convert.ToInt64(string.Join("", races.Select(race => race.Time.ToString())));

        var distanceRecord = Convert.ToInt64(string.Join("", races.Select(race => race.DistanceRecord.ToString())));

        var race = new Race(time, distanceRecord);

        var waysToWin = Simulate(race)
            .Where(result => result.Distance > result.Race.DistanceRecord)
            .Count();

        Console.WriteLine(waysToWin);

        return Task.CompletedTask;
    }

    private static Race[] Parse(string input)
    {
        var lines = File.ReadAllLines(input);
        var nums = lines.Select(line =>
        {
            return line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Skip(1)
                .Select(int.Parse)
                .ToArray();
        }).ToArray();
        var times = nums[0];
        var distanceRecords = nums[1];
        return times.Zip(distanceRecords)
            .Select(x => new Race(x.First, x.Second))
            .ToArray();
    }

    private static IEnumerable<Result> Simulate(Race race)
    {
        var limit = race.Time;
        for (var i = 0L; i < race.Time + 1; i++)
        {
            yield return Simulate(race, i);
        }
    }

    private static Result Simulate(Race race, long holdTime)
    {
        var velocity = holdTime;
        var remainingTime = race.Time - holdTime;
        var distance = velocity * remainingTime;
        if (distance < 0)
        {
            throw new InvalidOperationException("distance < 0");
        }
        return new Result(race, holdTime, distance);
    }
}

public record Race(long Time, long DistanceRecord);

public record Result(Race Race, long HoldTime, long Distance);