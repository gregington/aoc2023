using System;
using System.Collections.Concurrent;
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
            2 => Part2(),
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

    private static Task Part2()
    {
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

    private static Result[] Simulate(Race race)
    {
        return Enumerable.Range(0, race.Time + 1)
            .Select(holdTime => Simulate(race, holdTime))
            .ToArray();
    }

    private static Result Simulate(Race race, int holdTime)
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

public record Race(int Time, int DistanceRecord);

public record Result(Race Race, int HoldTime, int Distance);