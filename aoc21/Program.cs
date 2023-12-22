using System.Collections.Frozen;
using System.Collections.Immutable;
using System.CommandLine;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

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
        var (map, start) = await Parse(input);
        var task = part switch
        {
            1 => Part1(map, start),
            2 => Part2(map, start),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(char[][] map, Position start)
    {
        IEnumerable<Position> positions = new [] { start };

        for (var i = 0; i < 64; i++)
        {
            positions = StepMany(map, positions);
        }
        Console.WriteLine(positions.Count());

        return Task.CompletedTask;
    }

    private static Task Part2(char[][] map, Position start)
    {
        return Task.CompletedTask;
    }

    private static IEnumerable<Position> StepMany(char[][] map, IEnumerable<Position> positions)
    {
        return positions.SelectMany(p => Step(map, p)).Distinct();
    }

    private static IEnumerable<Position> Step(char[][] map, Position position)
    {
        return CandidateSteps(position)
            .Where(p => InBounds(map, p))
            .Where(p => map[p.Row][p.Col] == '.');
    }

    private static bool InBounds(char[][] map, Position p) =>
        p.Row >= 0 && p.Row < map.Length && p.Col >= 0 && p.Col < map[0].Length;


    private static IEnumerable<Position> CandidateSteps(Position p) {
        yield return p with { Row = p.Row - 1};
        yield return p with { Row = p.Row + 1};
        yield return p with { Col = p.Col - 1};
        yield return p with { Col = p.Col + 1};        
    }

    private static void Print(char[][] map, IEnumerable<Position> positions)
    {
        var copy = CopyMap(map);
        foreach (var (row, col) in positions)
        {
            copy[row][col] = 'O';
        }

        foreach (var row in copy)
        {
            Console.WriteLine(new string(row));
        }
    }

    private static char[][] CopyMap(char[][] map)
    {
        return map.Select(row => 
        {
            var newRow = new char[row.Length];
            Array.Copy(row, newRow, row.Length);
            return newRow;
        }).ToArray();
    }

    private static async Task<(char[][] Map, Position start)> Parse(string input)
    {
        var map = await File.ReadLinesAsync(input)
            .Select(line => line.ToArray())
            .ToArrayAsync();

        Position? start = null;
        for (var row = 0; row < map.Length; row++)
        {
            for (var col = 0; col < map[row].Length; col++)
            {
                var c = map[row][col];
                if (c == 'S')
                {
                    start = new Position(row, col);
                    map[row][col] = '.';
                    goto exit;
                }
            }
        }
        exit:
        return (map, start!);
    }
}

public record Position(int Row, int Col);

