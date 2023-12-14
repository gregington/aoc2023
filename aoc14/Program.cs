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
        var platform = await Parse(input);
        var task = part switch
        {
            1 => Part1(platform),
            2 => Part2(platform),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(char[][] platform)
    {
        var (_, load) = TiltPlatform(platform, Direction.North);

        Console.WriteLine(load);

        return Task.CompletedTask;
    }

    private static Task Part2(char[][] platform)
    {
        return Task.CompletedTask;
    }

    public static char[][] TiltNorth(char[][] input)
    {
        var copy = Copy(input);

        int moves;

        do
        {
            moves = 0;
            for (var row = 1; row < copy.Length; row++)
            {
                for (var col = 0; col < copy[row].Length; col++)
                {
                    var c = copy[row][col];
                    if (c is '.' or '#')
                    {
                        continue;
                    }
                    if (c != 'O')
                    {
                        throw new Exception($"Unexpected character {c} at {row},{col}");
                    }

                    var above = copy[row - 1][col];
                    if (above == '.')
                    {
                        copy[row - 1][col] = 'O';
                        copy[row][col] = '.';
                        moves++;
                    }
                }
            }
        } while (moves > 0);

        return copy;
    }

    private static (char[][] Platform, int Load) TiltPlatform(char[][] input, Direction downDirection)
    {
        var numRotations = downDirection switch
        {
            Direction.North => 0,
            Direction.West => 1,
            Direction.South => 2,
            Direction.East => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(downDirection))  
        };

        var rotated = Copy(input);

        for (int i = 0; i < numRotations; i++)
        {
            rotated = RotateClockwise(input);
        }

        var tilted = TiltNorth(rotated);
        var load = CalculateLoad(tilted);

        var reverseRotations = (4 - numRotations) % 4;
        for (int i = 0; i < reverseRotations; i++)
        {
            tilted = RotateClockwise(tilted);
        }

        return (tilted, load);
    }

    private static int CalculateLoad(char[][] platform)
    {
        var score = 0;
        for (var row = 0; row < platform.Length; row++)
        {
            for (var col = 0; col < platform[row].Length; col++)
            {
                if (platform[row][col] == 'O')
                {
                    score += platform.Length - row;
                }
            }
        }
        return score;
    }

    private static char[][] RotateClockwise(char[][] input)
    {
        var result = new char[input[0].Length][];
        for (var i = 0; i < input[0].Length; i++)
        {
            result[i] = new char[input.Length];
            for (var j = 0; j < input.Length; j++)
            {
                result[i][j] = input[input.Length - j - 1][i];
            }
        }

        return result;
    }

    private static char[][] Copy(char[][] input)
    {
        var copy = new char[input.Length][];
        for (var i = 0; i < input.Length; i++)
        {
            copy[i] = new char[input[i].Length];
            Array.Copy(input[i], copy[i], input[i].Length);
        }
        return copy;
    }

    private static void PrintPlatform(char[][] input)
    {
        foreach (var line in input)
        {
            Console.WriteLine(line);
        }
    }

    private static async Task<char[][]> Parse(string input)
    {
        return await File.ReadLinesAsync(input)
            .Select(x => x.ToCharArray())
            .ToArrayAsync();
    }
}

public enum Direction
{
    North,
    East,
    South,
    West
}