using System.Collections.Immutable;
using System.CommandLine;
using System.Drawing;
using System.Security.Cryptography;

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
        var map = await Parse(input);
        var task = part switch
        {
            1 => Part1(map),
            2 => Part2(map),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(int[][] map)
    {
        var endPosition = new Position(map.Length - 1, map[0].Length - 1);
        var queue = new PriorityQueue<State, int>();
        var seen = new HashSet<SeenKey>();

        foreach (var startingState in StartingStates(map))
        {
            queue.Enqueue(startingState, startingState.HeatLoss);
        }

        var minHeatLoss = int.MaxValue;

        while (queue.Count > 0)
        {
            var state = queue.Dequeue();
            var SeenKey = state.SeenKey;

            if (seen.Contains(SeenKey))
            {
                continue;
            }

            seen.Add(SeenKey);

            if (state.HeatLoss > minHeatLoss)
            {
                continue;
            }

            if (state.Position == endPosition)
            {
                minHeatLoss = Math.Min(minHeatLoss, state.HeatLoss);
                continue;
            }

            foreach (var nextState in NextStates(map, state))
            {
                queue.Enqueue(nextState, nextState.HeatLoss);
            }
        }

        Console.WriteLine(minHeatLoss - map[endPosition.Row][endPosition.Col]);

        return Task.CompletedTask;
    }

    private static Task Part2(int[][] map)
    {
        return Task.CompletedTask;
    }

    private static IEnumerable<State> NextStates(int[][] map, State state)
    {
        return AllowableDirections(state.LastDirection, state.NumDirectionMoves)
            .SelectMany(x => ValidPositions(x, state.Position, map))
            .Select(x =>
            {
                var numDirectionMoves = x.Direction == state.LastDirection ? state.NumDirectionMoves + 1 : 0;
                var additionalHeatLoss = map[state.Position.Row][state.Position.Col];
                return state with
                {
                    Position = x.Position,
                    LastDirection = x.Direction,
                    NumDirectionMoves = numDirectionMoves,
                    HeatLoss = state.HeatLoss + additionalHeatLoss,
                    Path = state.Path.Add((x.Position, x.Direction))
                };
            });
    }

    private static IEnumerable<Direction> AllowableDirections(Direction currentDirection, int numMovesInDirection)
    {
        var (left, straight, right) = currentDirection.TurnDirections();

        if (numMovesInDirection < 2)
        {
            yield return straight;
        }

        yield return left;
        yield return right;
    }

    public static IEnumerable<(Direction Direction, Position Position)> ValidPositions(Direction direction, Position currentPosition, int[][] map)
    {
        var newPosition = currentPosition.Move(direction);
        if (newPosition.Row >= 0 && newPosition.Row < map.Length
            && newPosition.Col >= 0 && newPosition.Col < map[0].Length)
        {
            yield return (direction, newPosition);            
        }
    }

    private static IEnumerable<State> StartingStates(int[][] map)
    {
        var startStates =  new []
            {
                (Position: new Position(0, 1), Direction: Direction.Right),
                (Position: new Position(1, 0), Direction: Direction.Down)
            }
            .Select(s => new State(s.Position, s.Direction, 1, map[s.Position.Row][s.Position.Col], [(s.Position, s.Direction)]));

        foreach (var startState in startStates)
        {
            yield return startState;
        }
    }

    public static void PrintPath(int[][] map, IEnumerable<(Position Position, Direction Direction)> path)
    {
        var copy = new char[map.Length][];
        for (var row = 0; row < copy.Length; row++)
        {
            copy[row] = new char[map[row].Length];
            copy[row] = map[row].Select(x => x.ToString()[0]).ToArray();
        }

        foreach (var (position, direction) in path)
        {
            var c = direction switch
            {
                Direction.Up => '^',
                Direction.Right => '>',
                Direction.Down => 'v',
                Direction.Left => '<',
                _ => throw new Exception("Invalid direction")
            };
            copy[position.Row][position.Col] = c;
        }

        foreach (var row in copy)
        {
            Console.WriteLine(new string(row));
        }
        Console.WriteLine();
        Console.WriteLine(path.Select(x => x.Position).Select(p => map[p.Row][p.Col]).Sum());

        Console.WriteLine();
        Console.WriteLine("--------");
        Console.WriteLine();

    }

    public static void PrintMap(int[][] map)
    {
        foreach(var line in map)
        {
            Console.WriteLine(string.Join("", line.Select(x => x.ToString())));
        }
        Console.WriteLine();
        Console.WriteLine("--------");
        Console.WriteLine();
    }

    private static async Task<int[][]> Parse(string input)
    {
        return await File.ReadLinesAsync(input)
            .Select(line => line.Select(c => c - '0').ToArray())
            .ToArrayAsync();
    }
}

public enum Direction { Up, Right, Down, Left }

public static class DirectionExtensions
{
    public static (Direction Left, Direction Straight, Direction Right) TurnDirections(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => (Direction.Left, Direction.Up, Direction.Right),
            Direction.Right => (Direction.Up, Direction.Right, Direction.Down),
            Direction.Down => (Direction.Right, Direction.Down, Direction.Left),
            Direction.Left => (Direction.Down, Direction.Left, Direction.Up),
            _ => throw new Exception("Unknown direction")
        };
    }

}

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

public record State(Position Position, Direction LastDirection, int NumDirectionMoves, int HeatLoss, ImmutableArray<(Position, Direction)> Path)
{
    public SeenKey SeenKey => new SeenKey(Position, LastDirection, NumDirectionMoves);
}

public record SeenKey(Position Position, Direction LastDirection, int NumDirectionMoves);

