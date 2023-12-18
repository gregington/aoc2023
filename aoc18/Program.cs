using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
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
        var vertices = Vertices(instructions);
        Console.WriteLine(Area(vertices));
        return Task.CompletedTask;
    }

    private static Task Part2(IEnumerable<Instruction> instructions)
    {
        instructions = FixInstructions(instructions);
        var vertices = Vertices(instructions);
        Console.WriteLine(Area(vertices));
        return Task.CompletedTask;
    }

    private static long Perimiter(IReadOnlyList<Position> vertices)
    {
        var verts = vertices.ToImmutableArray().Add(vertices[0]);
        return Enumerable.Range(0, verts.Length - 1)
            .Select(i => Math.Abs(verts[i].Row - verts[i + 1].Row) + Math.Abs(verts[i].Col - verts[i + 1].Col))
            .Sum();
    }

    private static long Area(IReadOnlyList<Position> vertices)
    {
        // Shoelace formula
        var verts = vertices.ToImmutableArray().Add(vertices[0]);
        
        var sum1 = Enumerable.Range(0, verts.Length - 1)
            .Select(i => verts[i].Row * verts[i + 1].Col)
            .Sum();

        var sum2 = Enumerable.Range(0, verts.Length - 1)
            .Select(i => verts[i].Col * verts[i + 1].Row)
            .Sum();

        var perimiter = Perimiter(vertices);

        return (Math.Abs(sum1 - sum2) + perimiter) / 2 + 1;
    }

    private static ImmutableArray<Position> Vertices(IEnumerable<Instruction> instructions)
    {
        var vertices = new List<Position>();
        var current = new Position(0, 0);
        vertices.Add(current);

        foreach (var instruction in instructions)
        {
            current = current.Move(instruction.Direction, instruction.Distance);
            vertices.Add(current);
        }

        return [.. vertices];
    }

    private static IEnumerable<Instruction> FixInstructions(IEnumerable<Instruction> original)
    {
        return original.Select(orig =>
        {
            var distance = Convert.ToInt64(orig.Color[..5], 16);
            var direction = orig.Color[^1] switch
            {
                '0' => Direction.Right,
                '1' => Direction.Down,
                '2' => Direction.Left,
                '3' => Direction.Up,
                _ => throw new Exception("Unknown direction")
            };

            return new Instruction(direction, distance, string.Empty);
        });
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
                Distance: long.Parse(match.Groups["distance"].Value),
                Color: match.Groups["color"].Value
            );
        });
    }

    [GeneratedRegex(@"^(?<direction>[URDL]) (?<distance>\d+) \(#(?<color>.{6})\)$")]
    private static partial Regex InstructionRegex();
}

public enum Direction { Up, Right, Down, Left }

public record Position(long Row, long Col)
{
    public Position Move(Direction direction) => Move(direction, 1);

    public Position Move(Direction direction, long distance)
    {
        return direction switch
        {
            Direction.Up => this with { Row = Row - distance },
            Direction.Right => this with { Col = Col + distance },
            Direction.Down => this with { Row = Row + distance },
            Direction.Left => this with { Col = Col - distance },
            _ => throw new Exception("Unknown direction")
        };
    }
}

public record Instruction(Direction Direction, long Distance, string Color);