using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.CommandLine;
using System.Drawing;
using System.Runtime.CompilerServices;
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
            2 => Part2(workflows, parts),
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

    private static Task Part2(FrozenDictionary<string, Workflow> workflows, ImmutableArray<Part> parts)
    {
        return Task.CompletedTask;
    }

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