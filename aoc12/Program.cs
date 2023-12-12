﻿using System.Collections.Immutable;
using System.CommandLine;
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
        var count = springsList.Select(s => PossibleConditions(s.Condition).Where(c => Match(c, s.DamagedCounts)).Count()).Sum();

        Console.WriteLine(count);

        return Task.CompletedTask;
    }

    private static Task Part2(ImmutableArray<Springs> springsList)
    {
        return Task.CompletedTask;
    }

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
