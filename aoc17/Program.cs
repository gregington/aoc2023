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
        var rules = new Rules(
            CanStop: _ => true,
            CanTurn: _ => true,
            CanMoveForward: state => state.DirectionMoves < 3
        );
        Console.WriteLine(FindMinimum(map, rules));

        return Task.CompletedTask;
    }

    private static Task Part2(int[][] map)
    {
        var rules = new Rules(
            CanStop: state => state.DirectionMoves >= 4,
            CanTurn: state => state.DirectionMoves == 0 || state.DirectionMoves >= 4,
            CanMoveForward: state => state.DirectionMoves < 10
        );
        Console.WriteLine(FindMinimum(map, rules));
        return Task.CompletedTask;
    }


    public static int FindMinimum(int[][] map, Rules rules)
    {
        var startPos = new Position(0, 0);
        var endPos = new Position(map.Length - 1, map[0].Length - 1);

        var queue = new PriorityQueue<State, int>();
        var seen = new HashSet<State>();
        queue.Enqueue(new State(startPos, Direction.Right, 0), 0);
        queue.Enqueue(new State(startPos, Direction.Down, 0), 0);

        while (queue.TryDequeue(out var state, out var heatLoss))
        {
            if (state.Position == endPos && rules.CanStop(state))
            {
                return heatLoss;
            }
            foreach (var nextState in NextStates(state, rules))
            {
                if (InMap(map, nextState.Position) && !seen.Contains(nextState))
                {
                    seen.Add(nextState);
                    queue.Enqueue(nextState, heatLoss + map[nextState.Position.Row][nextState.Position.Col]);
                }
            }
        }

        return int.MaxValue;
    }

    public static IEnumerable<State> NextStates(State state, Rules rules)
    {
        if (rules.CanMoveForward(state))
        {
            yield return state with
            {
                Position = state.Position.Move(state.Direction),
                DirectionMoves = state.DirectionMoves + 1
            };
        }

        if (rules.CanTurn(state))
        {
            var leftDirection = state.Direction.TurnLeft();
            yield return state with
            {
                Direction = leftDirection,
                Position = state.Position.Move(leftDirection),
                DirectionMoves = 1
            };

            var rightDirection = state.Direction.TurnRight();
            yield return state with
            {
                Direction = rightDirection,
                Position = state.Position.Move(rightDirection),
                DirectionMoves = 1
            };
        }
    }

    private static bool InMap(int[][] map, Position position)
    {
        var (row, col) = position;
        return row >= 0 && row < map.Length && col >= 0 && col < map[0].Length;
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
    public static Direction TurnLeft(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => Direction.Left,
            Direction.Right => Direction.Up,
            Direction.Down => Direction.Right,
            Direction.Left => Direction.Down,
            _ => throw new Exception("Unknown direction")
        };
    }

    public static Direction TurnRight(this Direction direction)
    {
        return direction switch
        {
            Direction.Up => Direction.Right,
            Direction.Right => Direction.Down,
            Direction.Down => Direction.Left,
            Direction.Left => Direction.Up,
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

public record State(Position Position, Direction Direction, int DirectionMoves);

public record Rules(
    Func<State, bool> CanStop,
    Func<State, bool> CanMoveForward,
    Func<State, bool> CanTurn
);

