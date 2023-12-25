using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Z3;

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

    public static async Task Run(string filename, int part)
    {
        var graph = await Parse(filename);
        var task = part switch
        {
            1 => Part1(filename, graph),
            2 => Part2(),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static async Task Part1(string filename, ImmutableDictionary<string, ImmutableHashSet<string>> graph)
    {
        var graphVizFilename = Path.GetFileNameWithoutExtension(filename) + ".dot";
        await WriteGraphVizFile(graphVizFilename, graph);

        // From graphVis with neato layout is visually obvious which three edges need to be cut.
        var edgesToCut = graphVizFilename.StartsWith("input")
            ? new List<(string, string)>() {("lmj","xgs"), ("pgz", "hgk"), ("gzr", "qnz")}
            : [("pzl", "hfx"), ("bvb", "cmg"), ("jqt", "nvd")];
        
        var cutGraph = Cut(graph, edgesToCut);
        var cutGraphVizFilename = Path.GetFileNameWithoutExtension(filename) + ".cut.dot";
        await WriteGraphVizFile(cutGraphVizFilename, cutGraph);

        var count1 = CountReachableNodes(cutGraph, edgesToCut[0].Item1);
        var count2 = CountReachableNodes(cutGraph, edgesToCut[0].Item2);

        Console.WriteLine($"{count1} * {count2} = {count1 * count2}");

        return;
    }

    private static int CountReachableNodes(ImmutableDictionary<string, ImmutableHashSet<string>> graph, string startNode)
    {
        // var visitedNodes = new HashSet<string>();
        var traversedEdges = new HashSet<(string, string)>();

        var queue = new Queue<(string, string)>();
        foreach (var dest in graph[startNode])
        {
            queue.Enqueue((startNode, dest));
        }

        while (queue.TryDequeue(out var edge))
        {
            traversedEdges.Add(edge);
            foreach (var dest in graph[edge.Item2])
            {
                var newEdge = (edge.Item2, dest);
                if (!traversedEdges.Contains(newEdge))
                {
                    queue.Enqueue(newEdge);
                }
            }
        }

        return traversedEdges.Select(x => x.Item1)
            .Concat(traversedEdges.Select(x => x.Item2))
            .Distinct()
            .Count();
    }

    private static ImmutableDictionary<string, ImmutableHashSet<string>> Cut(ImmutableDictionary<string, ImmutableHashSet<string>> graph, IEnumerable<(string, string)> cuts)
    {
        return cuts.Aggregate(graph, Cut);
    }

    private static ImmutableDictionary<string, ImmutableHashSet<string>> Cut(ImmutableDictionary<string, ImmutableHashSet<string>> graph, (string, string) cut)
    {
        // First forward
        var set1 = graph[cut.Item1];
        set1 = set1.Remove(cut.Item2);
        graph = graph.SetItem(cut.Item1, set1);

        // Then backward
        var set2 = graph[cut.Item2];
        set2 = set2.Remove(cut.Item1);
        graph = graph.SetItem(cut.Item2, set2);

        return graph;
    }

    private static Task Part2()
    {
        return Task.CompletedTask;
    }

    private static async Task WriteGraphVizFile(string filename, ImmutableDictionary<string, ImmutableHashSet<string>> graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("strict graph {");

        foreach (var node in graph)
        {
            foreach (var dest in node.Value)
            {
                sb.AppendLine($"    {node.Key} -- {dest}");
            }
        }

        sb.AppendLine("}");

        await File.WriteAllTextAsync(filename, sb.ToString());
    }

    private static async Task<ImmutableDictionary<string, ImmutableHashSet<string>>> Parse(string input)
    {
        Dictionary<string, HashSet<string>> graph = [];
        
        await foreach (var line in File.ReadLinesAsync(input))
        {
            var split = line.Split(": ");
            var from = split[0];
            var to = split[1].Split(" ");

            if (!graph.TryGetValue(from, out var set))
            {
                set = [];
            }
            foreach (var dest in to)
            {
                set.Add(dest);
            }
            graph[from] = set;

            foreach (var dest in to)
            {
                if (!graph.TryGetValue(dest, out set))
                {
                    set = [];
                }
                set.Add(from);
                graph[dest] = set;
            }
        }
        return graph.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableHashSet());
    }

}
