using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.CommandLine;
using System.Data.Common;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

public partial class Program
{
    public const string Accept = "A";
    public const string Reject = "R";

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
        var (workflows, parts) = await Parse(input);
        var task = part switch
        {
            1 => Part1(workflows, parts),
            2 => Part2(workflows),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(FrozenDictionary<string, Workflow> workflows, ImmutableArray<Part> parts)
    {
        var totalScore = parts.Select(part => (Part: part, Destination: Route(workflows, part)))
            .Where(x => x.Destination == Accept)
            .Select(x => x.Part)
            .Select(Score)
            .Sum();

        Console.WriteLine(totalScore);

        return Task.CompletedTask;
    }

    private static Task Part2(FrozenDictionary<string, Workflow> workflows)
    {
        var simplified = Simplify(workflows);
        var node = CreateTree(simplified, "in0", new RangeSet());

        Console.WriteLine(SumCombinations(node, Accept, Reject));
        Console.WriteLine(SumCombinations(node, Accept));

        return Task.CompletedTask;
    }

    private static long SumCombinations(Node node, params string[] names)
    {
        return SumCombinations(node, 0, names);
    }

    private static long SumCombinations(Node node, int sum, params string[] names)
    {
        if (names.Contains(node.Name))
        {
            return node.RangeSet.Combinations;
        }

        return (node.Positive == null ? 0 : SumCombinations(node.Positive, sum, names)) +
            (node.Negative == null ? 0 : SumCombinations(node.Negative, sum, names));
    }


    private static Node CreateTree(FrozenDictionary<string, Workflow> workflows, string name, RangeSet rangeSet)
    {
        if (name is Accept or Reject)
        {
            return new Node(name, rangeSet);
        }

        var workflow = workflows[name];
        var rule = workflow.Rules[0];
        var (positiveRangeSet, negativeRangeSet) = rangeSet.Split(rule.Category, rule.Comparison, rule.Value);

        return new Node(
            name, 
            rangeSet, 
            rule, 
            CreateTree(workflows, workflow.Rules[0].Destination, positiveRangeSet),
            CreateTree(workflows, workflow.Rules[1].Destination, negativeRangeSet)
        );
    }

    private static FrozenDictionary<string, Workflow> Simplify(FrozenDictionary<string, Workflow> workflows)
    {
        var simplified = new Dictionary<string, Workflow>();
        var workflowCounter = new Dictionary<string, int>();

        foreach (var (name, workflow) in workflows)
        {
            var rules = workflow.Rules;

            for (var i = 0; i < rules.Length - 1; i++)
            {
                var newName = $"{name}{i}";
                var currentRule = rules[i];
                var last = i == rules.Length - 2;
                if (last)
                {
                    var newPositive = currentRule.Destination is Accept or Reject ? currentRule.Destination : $"{currentRule.Destination}0";
                    var newNegative = rules[i+1].Destination is Accept or Reject ? rules[i+1].Destination : $"{rules[i+1].Destination}0";
                    var newRules = new []
                    {
                        new Rule(currentRule.Category, currentRule.Comparison, currentRule.Value, newPositive),
                        new Rule(Category.None, Comparison.Always, 0, newNegative)
                    };
                    simplified.Add(newName, new Workflow(newName, newRules.ToImmutableArray()));
                }
                else
                {
                    var newDestination = currentRule.Destination is Accept or Reject ? currentRule.Destination : $"{currentRule.Destination}0";
                    var newRules = new []
                    {
                        new Rule(currentRule.Category, currentRule.Comparison, currentRule.Value, newDestination),
                        new Rule(Category.None, Comparison.Always, 0, $"{name}{i+1}")
                    };
                    simplified.Add(newName, new Workflow(newName, newRules.ToImmutableArray()));
                }
            }
        }

        return simplified.ToFrozenDictionary();
    }

    private static FrozenDictionary<string, RangeSet> WorkflowDestinations(Workflow workflow)
    {
        var result = new Dictionary<string, RangeSet>();
        var rangeSet = new RangeSet();
        var lastRule = null as Rule;
        foreach (var rule in workflow.Rules)
        {
            if (lastRule != null)
            {
                rangeSet = rangeSet.Invert(lastRule.Category);
            }

            if (rule.Category != Category.None)
            {
                // If it's not the catch all (final) rule, intersect the range with existing
                rangeSet = rangeSet.Intersect(rule.Category, CreateRange(rule)!);
            }

            result.Add(rule.Destination, rangeSet);
        }
        return result.ToFrozenDictionary();
    }

    private static IEnumerable<ImmutableArray<(string Destination, RangeSet Ranges)>> FindRanges(FrozenDictionary<string, FrozenDictionary<string, RangeSet>> workflowDestinations, string workflowName, ImmutableArray<(string Destination, RangeSet Ranges)> path)
    {
        if (workflowName is Accept or Reject)
        {
            yield return path.Add((workflowName, path[^1].Ranges));
        }

        var destinations = workflowDestinations[workflowName];
        foreach (var kvp in destinations)
        {
            var newRanges = kvp.Value.Intersect(path[^1].Ranges); 
            var newPath = path.Add((workflowName, newRanges));
            foreach (var result in FindRanges(workflowDestinations, kvp.Key, newPath))
            {
                yield return result;
            }
        }
    }

    private static Range? CreateRange(Rule rule)
    {
        if (rule.Category == Category.None)
        {
            return null;
        }

        if (rule.Comparison == Comparison.Always)
        {
            return Range.Initial;
        }

        if (rule.Comparison == Comparison.GreaterThan)
        {
            return new Range(rule.Value + 1, Range.Max);
        }

        return new Range(Range.Min, rule.Value - 1);
    }

    private static IEnumerable<(ImmutableDictionary<Category, Range> Ranges, ImmutableArray<(string, Rule)> Path)> FindAcceptanceRanges(FrozenDictionary<string, Workflow> workflows)
    {
        var workflow = workflows["in"];

        var initialRanges = new [] { Category.ExtremelyCool, Category.Musical, Category.Aerodyamic, Category.Shiny }
            .ToImmutableDictionary(x => x, _ => Range.Initial);

        return FindRanges(workflows, workflow, initialRanges, ImmutableArray.Create<(string, Rule)>());
    }

    private static IEnumerable<(ImmutableDictionary<Category, Range> Ranges, ImmutableArray<(string, Rule)> Path)> FindRanges(
        FrozenDictionary<string, Workflow> workflows,
        Workflow workflow,
        ImmutableDictionary<Category, Range> ranges,
        ImmutableArray<(string Workflow, Rule Rule)> path)
    {
        foreach (var rule in workflow.Rules)
        {
            var newPath = path.Add((workflow.Name, rule));
            var newRanges = ranges;

            if (rule.Category != Category.None)
            {
                var range = rule.Comparison switch
                {
                    Comparison.LessThan => new Range(Range.Min, rule.Value - 1),
                    Comparison.GreaterThan => new Range(rule.Value + 1, Range.Max),
                    _ => throw new Exception("Invalid comparison")
                };
                var newRange = range.Intersect(ranges[rule.Category]);
                newRanges = newRanges.SetItem(rule.Category, newRange);
            }

            if (rule.Destination == Accept || rule.Destination == Reject)
            {
                yield return (newRanges, newPath.Add((rule.Destination, new Rule(Category.None, Comparison.Always, 0, rule.Destination))));
                continue;
            }


            foreach (var r in FindRanges(workflows, workflows[rule.Destination], newRanges, newPath))
            {
                yield return r;
            }
        }
    }

    private static long Combinations(IReadOnlyDictionary<Category, Range> ranges) =>
        ranges.Values.Select(r => (long) r.Length).Aggregate((a, b) => a * b);

    private static int Score(Part part) =>
        part[Category.ExtremelyCool] + part[Category.Musical] + part[Category.Aerodyamic] + part[Category.Shiny];

    private static string Route(FrozenDictionary<string, Workflow> workflows, Part part)
    {
        var dest = "in";
        while (dest is not Accept and not Reject)
        {
            var workflow = workflows[dest];
            dest = workflow.Route(part);
        }
        return dest;
    }

    private async static Task<(FrozenDictionary<string, Workflow> Workflows, ImmutableArray<Part> Parts)> Parse(string input)
    {
        var workflowRegex = WorkflowRegex();
        var partRegex = PartRegex();

        var workflows = new Dictionary<string, Workflow>();
        var parts = new List<Part>();

        await foreach (var line in File.ReadLinesAsync(input))
        {
            var match = workflowRegex.Match(line);
            if (match.Success)
            {
                var workflow = ParseWorkflow(match.Groups);
                workflows[workflow.Name] = workflow;
                continue;
            }

            match = partRegex.Match(line);
            if (match.Success)
            {
                parts.Add(ParsePart(match.Groups));
            }
        }

        return (workflows.ToFrozenDictionary(), parts.ToImmutableArray());
    }

    private static Workflow ParseWorkflow(GroupCollection groups)
    {
        var name = groups["name"].Value;
        var rulesStr = groups["rules"].Value.Split(",");
        var rules = rulesStr.Select(ParseRule).ToImmutableArray();

        return new Workflow(name, rules);
    }

    private static Rule ParseRule(string ruleStr)
    {
        var ruleRegex = RuleRegex();
        var match = ruleRegex.Match(ruleStr);
        if (!match.Success)
        {
            return new Rule(Category.None, Comparison.Always, 0, ruleStr);
        }

        var groups = match.Groups;

        return new Rule(
            (Category) groups["category"].Value[0],
            (Comparison) groups["comparison"].Value[0],
            Convert.ToInt32(groups["value"].Value),
            groups["destination"].Value
        );
    }

    private static Part ParsePart(GroupCollection groups)
    {
        var dict = new Dictionary<Category, int>
        {
            [Category.ExtremelyCool] = Convert.ToInt32(groups["x"].Value),
            [Category.Musical] = Convert.ToInt32(groups["m"].Value),
            [Category.Aerodyamic] = Convert.ToInt32(groups["a"].Value),
            [Category.Shiny] = Convert.ToInt32(groups["s"].Value)
        };

        return new Part(dict.ToFrozenDictionary());
    }

    [GeneratedRegex(@"^(?<name>.+){(?<rules>.*)}$")]
    private static partial Regex WorkflowRegex();

    [GeneratedRegex(@"^{(x=(?<x>\d+)),m=(?<m>\d+),a=(?<a>\d+),s=(?<s>\d+)}$")]
    private static partial Regex PartRegex();

    [GeneratedRegex(@"^(?<category>[xmas])(?<comparison>[<>])(?<value>\d+):(?<destination>.*)$")]
    private static partial Regex RuleRegex();
}

public enum Category
{
    ExtremelyCool = 'x',
    Musical = 'm',
    Aerodyamic = 'a',
    Shiny = 's',
    None = 'n'
}

public enum Comparison
{
    GreaterThan = '>',
    LessThan = '<',
    Always = 'x',
}

public record Rule(Category Category, Comparison Comparison, int Value, string Destination)
{
    public string? Evaluate(Part part)
    {
        return Comparison switch
        {
            Comparison.LessThan => part[Category] < Value ? Destination : null,
            Comparison.GreaterThan => part[Category] > Value ? Destination : null,
            Comparison.Always => Destination,
            _ => throw new Exception("Invalid comparison")
        };
    }

    public override string ToString() => $"{(char) Category}{(char) Comparison}{Value}";
}

public record Workflow(string Name, ImmutableArray<Rule> Rules)
{
    public string Route(Part part)
    {
        foreach (var rule in Rules)
        {
            var result = rule.Evaluate(part);
            if (result != null)
            {
                return result;
            }
        }

        throw new Exception("Part failed all rules");
    }

    public override string ToString()
    {
        var rules = string.Join(",", Rules.Select(r => {
            return r.Category switch
            {
                Category.None => r.Destination,
                _ => $"{(char)r.Category}{(char)r.Comparison}{r.Value}:{r.Destination}"
            };
        }));
        return $"{Name}: [{rules}]";
    }
}

public record Part(FrozenDictionary<Category, int> Attributes)
{
    public int this[Category category]
    {
        get => Attributes[category];
    }

    public override string ToString()
    {
        return $"{{{string.Join(",", Attributes.Select(kvp => $"{(char) kvp.Key}={kvp.Value}"))}}}";
    }
}

public record Range(int FromInclusive, int ToInclusive)
{
    public const int Min = 1;
    public const int Max = 4000;

    public static readonly Range Initial = new(Min, Max);

    public long Length = FromInclusive == 0 && ToInclusive == 0 ? 0 : ToInclusive - FromInclusive + 1;

    public override string ToString() => $"[{FromInclusive}..{ToInclusive}]";

    public Range Intersect(Range other)
    {
        // This inside other
        if (FromInclusive >= other.FromInclusive && ToInclusive <= other.ToInclusive)
        {
            return this;
        }

        // Other inside this
        if (other.FromInclusive >= FromInclusive && other.ToInclusive >= ToInclusive)
        {
            return other;
        }

        // Check for disjoint
        if (ToInclusive < other.FromInclusive || FromInclusive > other.ToInclusive)
        {
            return new Range(0, 0);            
        }

        if (FromInclusive <= other.ToInclusive)
        {
            return new(FromInclusive, other.ToInclusive);
        }

        if (other.FromInclusive <= ToInclusive)
        {
            return new(other.FromInclusive, ToInclusive);
        }

        throw new Exception("Unexpected condition");
    }

    public (Range Positive, Range Negative) Split(Comparison comparison, int value)
    {
        return comparison switch
        {
            Comparison.GreaterThan => (new(value + 1, ToInclusive), new(FromInclusive, value)),
            Comparison.LessThan => (new(FromInclusive, value - 1), new(value, ToInclusive)),
            _ => throw new Exception("Invalid comparison")
        };
    }

    public Range Invert()
    {
        if (FromInclusive == Min)
        {
            return new Range(ToInclusive + 1, Max);
        }

        if (ToInclusive == Max)
        {
            return new Range(Min, FromInclusive - 1);
        }

        throw new Exception("Unable to invert");
    }
}

public class RangeSet
{
    public RangeSet()
    {
        Ranges = new [] { Category.ExtremelyCool, Category.Musical, Category.Aerodyamic, Category.Shiny }
            .ToImmutableDictionary(c => c, _ => Range.Initial);
    }

    private RangeSet(ImmutableDictionary<Category, Range> ranges)
    {
        Ranges = ranges;
    }

    public ImmutableDictionary<Category, Range> Ranges { get; init; }

    public Range this[Category category] => Ranges[category];

    public RangeSet Intersect(Category c, Range range) =>
        new RangeSet(Ranges.SetItem(c, Ranges[c].Intersect(range)));

    public RangeSet Intersect(RangeSet other)
    {
        var rangeSet = this;
        foreach (var kvp in other.Ranges)
        {
            rangeSet = rangeSet.Intersect(kvp.Key, kvp.Value);
        }
        return rangeSet;
    }

    public RangeSet Invert(Category c) => new RangeSet(Ranges.SetItem(c, Ranges[c].Invert()));

    public (RangeSet Positive, RangeSet Negative) Split(Category category, Comparison comparison, int value)
    {
        var existingRange = Ranges[category];
        var (positiveRange, negativeRange) = existingRange.Split(comparison, value);
        return (
            new RangeSet(Ranges.SetItem(category, positiveRange)),
            new RangeSet(Ranges.SetItem(category, negativeRange))
        );
    }

    public long Combinations => Ranges.Values.Select(x => x.Length).Aggregate((a, b) => a * b);

    public override string ToString()
    {
        var components = Ranges.Where(kvp => kvp.Value != Range.Initial)
            .Select(kvp => $"{(char) kvp.Key}{kvp.Value}");

        return $"{{{string.Join(", ", components)}}}";
    }
}

public class Node
{
    public Node(string name, RangeSet rangeSet)
    {
        Name = name;
        RangeSet = rangeSet;
        Rule = null;
        Positive = null;
        Negative = null;
    }

    public Node(string name, RangeSet rangeSet, Rule rule, Node positive, Node negative) : this(name, rangeSet)
    {
        Rule = rule;
        Positive = positive;
        Negative = negative;
    }
    
    public RangeSet RangeSet { get; init; }

    public string Name { get; init; }

    public Rule? Rule { get; init; }

    public Node? Positive { get; init; }

    public Node? Negative { get; init; }

    public bool IsLeaf => Positive == null && Negative == null;
}