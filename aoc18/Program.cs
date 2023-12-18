using System.Collections;
using System.Collections.Frozen;
using System.CommandLine;
using System.Drawing;
using System.Text;
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
        var instructions = await Parse(input);
        var task = part switch
        {
            1 => Part1(instructions),
            2 => Part2(instructions),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IEnumerable<Instruction> instructions)
    {
        var map = DigOutline(instructions);
        map = DigInterior(map);
        Console.WriteLine(map.Count);
        return Task.CompletedTask;
    }

    private static Task Part2(IEnumerable<Instruction> instructions)
    {
        return Task.CompletedTask;
    }

    private static FrozenDictionary<Position, string> DigOutline(IEnumerable<Instruction> instructions)
    {
        var dictionary = new Dictionary<Position, string>();

        var current = new Position(0, 0);
        dictionary[current] = "000000";

        foreach (var instruction in instructions)
        {
            for (var i = 0; i < instruction.Distance; i++)
            {
                current = current.Move(instruction.Direction);
                dictionary[current] = instruction.Color;
            }
        }

        return dictionary.ToFrozenDictionary();
    }

    private static FrozenDictionary<Position, string> DigInterior(IReadOnlyDictionary<Position, string> outline)
    {
        var startPosition = FindStartPosition(outline);
        var map = outline.ToDictionary();

        var stack = new Stack<Position>();
        stack.Push(startPosition);

        while (stack.TryPop(out var current))
        {
            if (map.ContainsKey(current))
            {
                continue;
            }
            map[current] = "000000";
            stack.Push(current.Move(Direction.Up));
            stack.Push(current.Move(Direction.Right));
            stack.Push(current.Move(Direction.Down));
            stack.Push(current.Move(Direction.Left));
        }

        return map.ToFrozenDictionary();
    }

    private static Position FindStartPosition(IReadOnlyDictionary<Position, string> map)
    {
        var minRow = map.Keys.Min(p => p.Row);
        var topPixels = map.Keys.Where(p => p.Row == minRow).OrderBy(p => p.Col);

        // Find the first position that does not have a position downwards
        return topPixels.First(p => !map.ContainsKey(p.Move(Direction.Down)))
            .Move(Direction.Down);
    }

    private static void Print(IReadOnlyDictionary<Position, string> map)
    {
        var minRow = map.Keys.Min(p => p.Row);
        var maxRow = map.Keys.Max(p => p.Row);
        var minCol = map.Keys.Min(p => p.Col);
        var maxCol = map.Keys.Max(p => p.Col);

        var height = maxRow - minRow + 1;
        var width = maxCol - minCol + 1;

        for (var row = minRow; row <= maxRow; row++)
        {
            var sb = new StringBuilder();
            for (var col = minCol; col <= maxCol; col++)
            {
                var position = new Position(row, col);
                var c = map.ContainsKey(position) ? '#' : '.';
                sb.Append(c);
            }
            Console.WriteLine(sb.ToString());
        }

    }

    private static async Task<IEnumerable<Instruction>> Parse(string input)
    {
        var regex = InstructionRegex();

        var lines = await File.ReadAllLinesAsync(input);
        return lines.Select(line =>
        {
            var match = regex.Match(line);
            if (!match.Success)
            {
                throw new Exception($"Failed to parse line: {line}");
            }

            return new Instruction(
                Direction: match.Groups["direction"].Value switch
                {
                    "U" => Direction.Up,
                    "R" => Direction.Right,
                    "D" => Direction.Down,
                    "L" => Direction.Left,
                    _ => throw new Exception("Unknown direction")
                },
                Distance: int.Parse(match.Groups["distance"].Value),
                Color: match.Groups["color"].Value
            );
        });
    }

    [GeneratedRegex(@"^(?<direction>[URDL]) (?<distance>\d+) \(#(?<color>.{6})\)$")]
    private static partial Regex InstructionRegex();
}

public enum Direction { Up, Right, Down, Left }

public record Position(int Row, int Col)
{
    public Position Move(Direction direction)
    {
        return direction switch
        {
            Direction.Up => this with { Row = Row - 1 },
            Direction.Right => this with { Col = Col + 1 },
            Direction.Down => this with { Row = Row + 1 },
            Direction.Left => this with { Col = Col - 1},
            _ => throw new Exception("Unknown direction")
        };
    }
}

public record Instruction(Direction Direction, int Distance, string Color);