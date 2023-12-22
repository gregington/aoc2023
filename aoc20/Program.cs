using System.Collections.Frozen;
using System.Collections.Immutable;
using System.CommandLine;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

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
        var modules = await Parse(input);
        var task = part switch
        {
            1 => Part1(modules),
            2 => Part2(),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(FrozenDictionary<string, Module> modules)
    {
        var (lowCount, highCount) = Enumerable.Range(0, 1000)
            .SelectMany(_ => PushButton(modules))
            .Aggregate((LowCount: 0, HighCount: 0), (acc, signal) => signal.Pulse == Pulse.Low ? (acc.LowCount + 1, acc.HighCount) : (acc.LowCount, acc.HighCount + 1));

        Console.WriteLine(lowCount * highCount);

        return Task.CompletedTask;
    }

    private static Task Part2()
    {
        return Task.CompletedTask;
    }

    private static IEnumerable<Signal> PushButton(FrozenDictionary<string, Module> modules)
    {
        var queue = new Queue<Signal>();
        queue.Enqueue(new Signal(Pulse.Low, "button", "broadcaster"));

        while (queue.TryDequeue(out var signal))
        {
            yield return signal;
            var module = modules.GetValueOrDefault(signal.Receiver);
            if (module == null)
            {
                continue;
            }
            foreach (var newSignal in module.Process(signal))
            {
                queue.Enqueue(newSignal);
            }
        }
    }

    private static async Task<FrozenDictionary<string, Module>> Parse(string input)
    {        
        var regex = Regex();

        var types = new Dictionary<string, Type>();
        var inputs = new Dictionary<string, List<string>>();
        var outputs = new Dictionary<string, List<string>>();

        await foreach (var line in File.ReadLinesAsync(input))
        {
            var groups = regex.Match(line).Groups;
            var name = groups["name"].Value;

            var typeStr = "";
            if (groups["type"].Success)
            {
                typeStr = groups["type"].Value;
            }
            var type = typeStr switch
                {
                    "%" => typeof(FlipFlop),
                    "&" => typeof(Conjunction),
                    "" => typeof(Broadcast),
                    _ => throw new Exception($"Invalid type {typeStr}")
                };

            types[name] = type;

            var outputStr = groups["receivers"].Value;
            var outputList = outputStr.Replace(" ", "").Trim().Split(",").ToList();
            outputs[name] = outputList;

            foreach (var output in outputList)
            {
                if (!inputs.TryGetValue(output, out var inputList))
                {
                    inputList = new List<string>();
                    inputs[output] = inputList;
                }
                inputList.Add(name);
            }
        }

        var modules = new Dictionary<string, Module>();

        foreach (var (name, type) in types)
        {
            var inputList = inputs.GetValueOrDefault(name, []);
            var outputList = outputs[name];

            if (type == typeof(Broadcast))
            {
                modules[name] = new Broadcast(name, inputList, outputList);
            } 
            else if (type == typeof(FlipFlop))
            {
                modules[name] = new FlipFlop(name, inputList, outputList);
            }
            else if (type == typeof(Conjunction))
            {
                modules[name] = new Conjunction(name, inputList, outputList);
            }
            else
            {
                throw new Exception("Unexpecrted type");
            }
        }

        return modules.ToFrozenDictionary();
    }

    [GeneratedRegex(@"^(?<type>[%&])?(?<name>.+) -> (?<receivers>.*)$")]
    private static partial Regex Regex();
}

public enum Pulse
{
    Low,
    High
}

public record Signal(Pulse Pulse, string Sender, string Receiver)
{
    public override string ToString()
    {
        return $"{Sender} -{Pulse.ToString().ToLowerInvariant()}-> {Receiver}";
    }
}

public abstract class Module
{
    public Module(string name, IEnumerable<string> inputs, IEnumerable<string> outputs)
    {
        Name = name;
        Inputs = inputs.ToImmutableArray();
        Outputs = outputs.ToImmutableArray();
    }

    public string Name { get; init; }

    public ImmutableArray<string> Inputs { get; init; }

    public ImmutableArray<string> Outputs { get; init; }

    public abstract IEnumerable<Signal> Process(Signal signal);

    protected IEnumerable<Signal> SendPulse(Pulse pulse)
    {
        foreach (var output in Outputs)
        {
            yield return new Signal(pulse, Name, output);
        }
    }
}

public class FlipFlop : Module
{
    enum State
    {
        Off,
        On
    }

    private State state = State.Off;

    public FlipFlop(string name, IEnumerable<string> inputs, IEnumerable<string> outputs) : base(name, inputs, outputs) {}

    public override IEnumerable<Signal> Process(Signal signal)
    {
        var (pulse, _, _) = signal;

        if (pulse == Pulse.High)
        {
            // No-op for high pulse
            yield break;
        }

        Pulse outputPulse;
        if (state == State.Off)
        {
            state = State.On;
            outputPulse = Pulse.High;
        }
        else {
            state = State.Off;
            outputPulse = Pulse.Low;
        }

        foreach (var outputSignal in SendPulse(outputPulse))
        {
            yield return outputSignal;
        }
    }
}

public class Conjunction : Module
{
    private readonly Dictionary<string, Pulse> inputValues;

    public Conjunction(string name, IEnumerable<string> inputs, IEnumerable<string> outputs) : base(name, inputs, outputs) {
        inputValues = inputs.ToDictionary(input => input, _ => Pulse.Low);
    }

    public override IEnumerable<Signal> Process(Signal signal)
    {
        var (pulse, source, _) = signal;
        if (!inputValues.ContainsKey(source))
        {
            throw new Exception($"Unexpected source {source} for module {Name}");
        }
        inputValues[source] = pulse;

        var outputPulse = inputValues.Values.All(x => x == Pulse.High) ? Pulse.Low : Pulse.High;

        return SendPulse(outputPulse);
    }
}

public class Broadcast : Module
{
    public Broadcast(string name, IEnumerable<string> inputs, IEnumerable<string> outputs) : base(name, inputs, outputs) {}

    public override IEnumerable<Signal> Process(Signal signal)
    {
        var (pulse, _, _) = signal;

        return SendPulse(pulse);
    }
}