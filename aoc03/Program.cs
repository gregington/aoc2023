using System.CommandLine;
using System.Text.Json;

public class Program
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
        var board = await CreateBoard(input);
        var task = part switch
        {
            1 => Part1(board),
            2 => Part2(board),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(char[][] board)
    {
        var height = board.Length;
        var width = board[0].Length;

        var potentialPartNumbers = FindPartNumbers(board);

        var partNumbers = potentialPartNumbers
            .Select(pn => (PartNumber: pn, Neighbors: pn.Positions.SelectMany(p => GetNeighbors(p, board))))
            .Where(x => x.Neighbors.Any(c => IsSymbol(c)));

        var sum = partNumbers.Select(pn => pn.PartNumber.Value).Sum();
        Console.WriteLine(sum);

        return Task.CompletedTask;
    }

    private static Task Part2(char[][] board)
    {
        return Task.CompletedTask;
    }

    private static async Task<char[][]> CreateBoard(string input)
    {
        var lines = await File.ReadAllLinesAsync(input);

        char[][] board = new char[lines.Length][];

        for (int row = 0; row < lines.Length; row++)
        {
            var rowChars = lines[row].ToCharArray();
            board[row] = rowChars;
        }
        return board;
    }

    private static PartNumber[] FindPartNumbers(char[][] board)
    {
        var height = board.Length;
        var width = board[0].Length;

        List<PartNumber> partNumbers = [];

        List<char> partNumBuilder = [];
        List<Point> positionsBuilder = [];
        for (var row = 0; row < height; row++)
        {
            for (var col = 0; col < width; col++)
            {
                var c = board[row][col];
                if (char.IsDigit(c))
                {
                    partNumBuilder.Add(c);
                    positionsBuilder.Add(new Point(row, col));
                }
                else if (partNumBuilder.Count > 0)
                {
                    var value = int.Parse(new string(partNumBuilder.ToArray()));
                    var partNumber = new PartNumber(value, [.. positionsBuilder]);
                    partNumbers.Add(partNumber);

                    partNumBuilder.Clear();
                    positionsBuilder.Clear();
                }
            }

            if (partNumBuilder.Count > 0)
            {
                var value = int.Parse(new string(partNumBuilder.ToArray()));
                var partNumber = new PartNumber(value, [.. positionsBuilder]);
                partNumbers.Add(partNumber);

                partNumBuilder.Clear();
                positionsBuilder.Clear();
            }
        }
        return [.. partNumbers];
    }

    private static char[] GetNeighbors(Point point, char[][] board)
    {
        var possiblePositions = new [] {   
            new Point(point.Row - 1, point.Col - 1),
            new Point(point.Row - 1, point.Col),
            new Point(point.Row - 1, point.Col + 1),
            new Point(point.Row, point.Col - 1),
            new Point(point.Row, point.Col + 1),
            new Point(point.Row + 1, point.Col - 1),
            new Point(point.Row + 1, point.Col),
            new Point(point.Row + 1, point.Col + 1)
        };

        var positions = possiblePositions
            .Where(x => x.Row >= 0 && x.Row < board.Length)
            .Where(x => x.Col >= 0 && x.Col < board[0].Length)
            .ToArray();

        return positions
            .Select(x => board[x.Row][x.Col])
            .ToArray();
    }

    private static bool IsSymbol(char c) => c != '.' && !char.IsDigit(c);

    private record Point(int Row, int Col);

    private record PartNumber(int Value, Point[] Positions);
}