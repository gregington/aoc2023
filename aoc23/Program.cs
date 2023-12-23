using System.Collections;
using System.Collections.Immutable;
using System.CommandLine;
using System.Runtime.Versioning;

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

    private static Task Part1(char[][] map)
    {
        var maxSteps = FindMaxSteps(map);
        Console.WriteLine(maxSteps);

        return Task.CompletedTask;
    }

    private static Task Part2(char[][] map)
    {
        map = RemoveSlopes(map);
        var start = FindStart(map);
        var end = FindEnd(map);

        var branches = FindBranches(map);
        var nodeMap = CreateNodeCosts(map, branches);

        var maxSteps = FindMaxSteps(nodeMap, end, start, [], 0);

        Console.WriteLine(maxSteps);

        return Task.CompletedTask;
    }

    private static int FindMaxSteps(Dictionary<Position, Dictionary<Position, int>> costs, Position goal, Position position, ImmutableHashSet<Position> visited, int cost)
    {
        if (position == goal)
        {
            return cost;
        }

        visited = visited.Add(position);
        var maxCost = int.MinValue;
        var destinations = costs[position];
        foreach (var (destination, traversalCost) in destinations)
        {
            if (visited.Contains(destination))
            {
                continue;
            }
            var newCost = FindMaxSteps(costs, goal, destination, visited, cost + traversalCost);
            maxCost = Math.Max(maxCost, newCost);
        }
        return maxCost;
    }

    private static Dictionary<Position, Dictionary<Position, int>> CreateNodeCosts(char[][] map, IEnumerable<(Position Position, HashSet<Direction> Direction)> nodes)
    {
        var costs = nodes.Select(n => n.Position)
            .ToDictionary(p => p, _ => new Dictionary<Position, int>());

        var nodeSet = costs.Keys.ToHashSet();

        foreach (var (node, startDirection) in nodes.SelectMany(n => n.Direction.Select(d => (n.Position, d))))
        {
            var firstStep = node.Move(startDirection);
            var (nextNode, steps) = FindNextNode(map, firstStep, [node], nodeSet);
            costs[node][nextNode] = steps;
        }
        return costs;
    }

    private static (Position, int) FindNextNode(char[][] map, Position position, HashSet<Position> visited, HashSet<Position> nodeSet)
    {
        if (nodeSet.Contains(position))
        {
            return (position, visited.Count);
        }

        visited.Add(position);
        var newPositions = NewPositions(map, position, visited);
        var newPosition = newPositions.Single(); // we should only have one path
        return FindNextNode(map, newPosition, visited, nodeSet);
    }

    private static IEnumerable<(Position Position, HashSet<Direction> Directions)> FindBranches(char[][] map)
    {
        // Start and end are special
        var start = FindStart(map);
        yield return (start, new HashSet<Direction>{Direction.Down});

        var end = FindEnd(map);
        yield return (end, new HashSet<Direction>{Direction.Up});

        for (var row = 0; row < map.Length; row++)
        {
            for (var col = 0; col < map[row].Length; col++)
            {
                var position = new Position(row, col);
                if (HasThreeOrMorePathNeighbors(map, position, out var directions))
                {
                    yield return (position, directions);
                }
            }
        }
    }

    private static bool HasThreeOrMorePathNeighbors(char[][] map, Position position, out HashSet<Direction> directions)
    {
        directions = [];
        if (map[position.Row][position.Col] == '#')
        {
            return false;
        }
        var directions2 =  Enum.GetValues<Direction>()
            .Select(direction => (Direction: direction, NewPosition: position.Move(direction)))
            .Where(p => InBounds(map, p.NewPosition))
            .Where(p => map[p.NewPosition.Row][p.NewPosition.Col] is '.' or '^' or '>' or 'v' or '<')
            .Select(p => p.Direction)
            .ToHashSet();

        if (directions2.Count >= 3)
        {
            directions = directions2;
            return true;
        }
        return false;
    }

    private static int FindMaxSteps(char[][] map)
    {
        var start = FindStart(map);
        var end = FindEnd(map);

        return FindMaxSteps(map, end, start, []);
    }

    private static char[][] RemoveSlopes(char[][] map)
    {
        return map.Select(line => line.Select(c => c switch
        {
            '^' => '.',
            '>' => '.',
            'v' => '.',
            '<' => '.',
            _ => c
        }).ToArray()).ToArray();
    }

    private static int FindMaxSteps(char[][] map, Position goal, Position position, ImmutableHashSet<Position> visited)
    {
        if (position == goal)
        {
            return 0;
        }

        visited = visited.Add(position);

        var max = int.MinValue;

        foreach (var newPosition in NewPositions(map, position, visited))
        {
            var steps = FindMaxSteps(map, goal, newPosition, visited) + 1;
            max = Math.Max(max, steps);
        }
        return max;
    }

    private static IEnumerable<Position> NewPositions(char[][] map, Position position, IReadOnlySet<Position> visited)
    {
        var c = map[position.Row][position.Col];
        var candidates = new List<Position>();
        if (c is '>' or '<' or '^' or 'v')
        {
            candidates.Add(position.Move((Direction) c));
        }
        else 
        {
            candidates.AddRange(Enum.GetValues<Direction>().Select(d => position.Move(d)));
        }

        return candidates
            .Where(p => InBounds(map, p))
            .Where(p => map[p.Row][p.Col] != '#')
            .Where(p => !visited.Contains(p));
    }

    private static void PrintMap(char[][] map)
    {
        foreach (var row in map)
        {
            Console.WriteLine(new string(row));
        }
    }

    private static bool InBounds(char[][] map, Position p) =>
        p.Row >= 0 && p.Row < map.Length && p.Col >= 0 && p.Col < map[0].Length;

    private static Position FindStart(char[][] map)
    {
        var charPos = FindSinglePath(map[0]);
        return new Position(0, charPos);
    }

    private static Position FindEnd(char[][] map)
    {
        var charPos = FindSinglePath(map[^1]);
        return new Position(map.Length - 1, charPos);
    }

    private static int FindSinglePath(char[] row) {
        var pos = -1;
        for (var i = 0; i < row.Length; i++)
        {
            if (row[i] == '.')
            {
                if (pos != -1)
                {
                    throw new Exception("Found multiple '.'");
                }
                pos = i;
            }
        }
        return pos;
    }

    private static async Task<char[][]> Parse(string filename)
    {
        return await File.ReadLinesAsync(filename)
            .Select(line => line.ToArray())
            .ToArrayAsync();
    }
}

public record State(Position Position, ImmutableHashSet<Position> Visited);

public record Position(int Row, int Col)
{
    public Position Move(Direction direction)
    {
        return direction switch
        {
            Direction.Up => this with { Row = Row - 1 },
            Direction.Right => this with { Col = Col + 1 },
            Direction.Down => this with { Row = Row + 1 },
            Direction.Left => this with { Col = Col - 1 },
            _ => throw new Exception("Unknown direction")
        };
    }
}

public enum Direction
{
    Up = '^',
    Right = '>',
    Down = 'v',
    Left = '<',
}
