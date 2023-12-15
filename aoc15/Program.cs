using System.Collections;
using System.CommandLine;
using System.Reflection.Emit;

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
        var initSequence = await Parse(input);
        var task = part switch
        {
            1 => Part1(initSequence),
            2 => Part2(initSequence),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IEnumerable<string> initSequence)
    {
        var hashes = initSequence.Select(Hash);
        Console.WriteLine(hashes.Sum());
        return Task.CompletedTask;
    }

    private static Task Part2(IEnumerable<string> initSequence)
    {
        var boxes = InitBoxes(initSequence);

        var focusingPower = FocusingPower(boxes);
        Console.WriteLine(focusingPower);

        return Task.CompletedTask;
    }

    private static int FocusingPower(Box[] boxes)
    {
        var focusingPower = 0;
        for (var i = 0; i < boxes.Length; i++)
        {
            var box = boxes[i];
            var boxMultipler = i + 1;
            for (var j = 0; j < box.Lenses.Count; j++)
            {
                var lens = box.Lenses[j];
                var lensMultiplier = j + 1;
                focusingPower += boxMultipler * lensMultiplier * lens.FocalLength;
            }
        }

        return focusingPower;   
    }

    private static Box[] InitBoxes(IEnumerable<string> initSequence)
    {
        var boxes = Enumerable.Range(0, 256).Select(_ => new Box()).ToArray();

        var operations = initSequence.Select(s => CreateOperation(s))    
            .Select(op => (Hash: Hash(op.Lens.Label), Operation: op));

        foreach (var (hash, op) in operations)
        {
            boxes[hash].Apply(op);
        }
        return boxes;
    }

    private static int Hash(string input)
    {
        return input.Select(c => (int) c)
            .Aggregate(0, (acc, c) =>
            {
                var hash = acc + c;
                hash *= 17;
                return hash % 256;
            });
    }

    private static Operation CreateOperation(string input)
    {
        if (input.EndsWith('-'))
        {
            return new Operation('-', new Lens(input[0..^1], -1));
        }

        var x = input.Split('=');
        return new Operation('=', new Lens(x[0], int.Parse(x[1])));
    }

    private static async Task<IEnumerable<string>> Parse(string input)
    {
        return (await File.ReadAllLinesAsync(input)).First().Split(',');
    }
}

public class Box
{
    public readonly List<Lens> Lenses = new List<Lens>();

    public int Count => Lenses.Count;

    public void Apply(Operation op)
    {
        switch (op.Op)
        {
            case '=':
                Add(op.Lens);
                break;
            case '-':
                Remove(op.Lens);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(op));
        }
    }

    private void Add(Lens lens) {
        var index = IndexOfLabel(lens.Label);

        if (index != -1)
        {
            Lenses[index] = lens;
            return;
        }

        Lenses.Add(lens);
    }

    private void Remove(Lens lens)
    {
        var index = IndexOfLabel(lens.Label);
        
        if (index == -1)
        {
            return;
        }
        Lenses.RemoveAt(index);
    }

    private int IndexOfLabel(string label)
    {
        for (var i = 0; i < Lenses.Count; i++)
        {
            if (Lenses[i].Label == label)
            {
                return i;
            }
        }

        return -1;
    }
}

public record Lens(string Label, int FocalLength);

public record Operation(char Op, Lens Lens);