using System.Collections.Concurrent;
using System.CommandLine;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
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
        var (directions, nodes) = await Parse(input);
        var task = part switch
        {
            1 => Part1(directions, nodes),
            2 => Part2(directions, nodes),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(List<Direction> directions, Dictionary<string, Node> nodes)
    {
        var infiniteDirections = Enumerable.Repeat(directions, int.MaxValue).SelectMany(x => x);
        var currentNode = "AAA";
        int count = 0;

        foreach(var direction in infiniteDirections)
        {
            if (currentNode == "ZZZ")
            {
                break;
            }

            currentNode = nodes[currentNode][direction];
            count++;
        }

        Console.WriteLine(count);

        return Task.CompletedTask;
    }

    private static Task Part2(List<Direction> directions, Dictionary<string, Node> nodes)
    {
        var infiniteDirections = Enumerable.Repeat(directions, int.MaxValue).SelectMany(x => x);
        var startNodes = nodes.Keys.Where(n => n.EndsWith('A')).ToArray();

        var pathLengthsFromStart = startNodes.ToDictionary(startNode => startNode, startNode => StepsToAnyZNode(directions, nodes, startNode));

        var steps = pathLengthsFromStart.Values
            .Select(x => x.Steps)
            .Aggregate(Lcm);

        Console.WriteLine(steps);

        return Task.CompletedTask;
    }

    private static long Lcm(long x, long y) => x * y / Gcd(x, y);

    private static long Gcd(long x, long y)
    {
        while (y != 0)
        {
            var temp = y;
            y = x % y;
            x = temp;
        }
        return x;
    }

    private static (string EndNode, long Steps) StepsToAnyZNode(List<Direction> directions, Dictionary<string, Node> nodes, string startNode)
    {
        var infiniteDirections = Enumerable.Repeat(directions, int.MaxValue).SelectMany(x => x);
        var currentNode = startNode;
        var count = 0L;

        foreach(var direction in infiniteDirections)
        {
            if (currentNode.EndsWith('Z'))
            {
                break;
            }

            currentNode = nodes[currentNode][direction];
            count++;
        }

        return (currentNode, count);
    }
    
    public static async Task<(List<Direction> Directions, Dictionary<string, Node> Nodes)> Parse(string input)
    {
        var lines = await File.ReadAllLinesAsync(input);

        var directions = lines[0].ToArray()
            .Select(c => c == 'L' ? Direction.Left : Direction.Right)
            .ToList();

        var nodesRegex = NodeRegex();
        
        var nodes = lines.Select(line => nodesRegex.Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups)
            .Select(g => (Node: g["node"].Value, Left: g["left"].Value, Right: g["right"].Value))
            .ToDictionary(x => x.Node, x => new Node(x.Node, x.Left, x.Right));

        return (directions, nodes);
    }

    [GeneratedRegex(@"^(?<node>.+) = \((?<left>.+), (?<right>.+)\)$")]
    private static partial Regex NodeRegex();
}

public record Node(string Id, string Left, string Right)
{
    public string this[Direction direction]
    {
        get => direction == Direction.Left ? Left : Right;
    }
}

public enum Direction
{
    Left,
    Right
}
