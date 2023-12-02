using System.CommandLine;
using System.Diagnostics;
using System.Text.RegularExpressions;

partial class Program
{
    public static async Task Main(string[] args)
    {
        var inputOption = new Option<string>(
            name: "--input",
            description: "Input file path",
            getDefaultValue: () => "input.txt");

        var rootCommand = new RootCommand();
        rootCommand.AddOption(inputOption);

        rootCommand.SetHandler(Run, inputOption);

        await rootCommand.InvokeAsync(args);
    }

    private static async Task Run(string input)
    {
        var lines = File.ReadLinesAsync(input);
        var numbers = lines
            .Select(line =>
            {
                var nums = line.Where(c => char.IsDigit(c)).ToArray();
                return int.Parse($"{nums[0]}{nums[^1]}");
            });

        var sum = numbers.ToEnumerable().Sum();
        Console.WriteLine(sum);
    }
}


