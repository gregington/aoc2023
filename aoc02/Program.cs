
using System.Collections.Immutable;
using System.CommandLine;
using System.Text.RegularExpressions;

public class Program
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
        var limit = new Cubes(12, 13, 14);
        var lines = File.ReadLinesAsync(input);
        var games = await lines.Select(Game.Parse).ToArrayAsync();

        var task = part switch
        {
            1 => Part1(games),
            2 => Part2(games),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IReadOnlyList<Game> games)
    {
        var limit = new Cubes(12, 13, 14);

        var possibleGames = games.Where(game => game.Draws.All(draw => Possible(draw, limit))).ToArray();
        Console.WriteLine(possibleGames.Sum(game => game.Id));
        return Task.CompletedTask;
    }

    private static Task Part2(IReadOnlyList<Game> games)
    {
        var minimums = games.Select(game => Minimum(game.Draws)).ToArray();
        var powers = minimums.Select(minimum => Power(minimum)).ToArray();
        Console.WriteLine(powers.Sum());
        return Task.CompletedTask;
    }

  private static bool Possible(Cubes cubes, Cubes limit) => 
    cubes.Red <= limit.Red && cubes.Green <= limit.Green && cubes.Blue <= limit.Blue;

    private static Cubes Minimum(IReadOnlyList<Cubes> draws) =>
        draws.Aggregate((a, b) => new Cubes(Math.Max(a.Red, b.Red), Math.Max(a.Green, b.Green), Math.Max(a.Blue, b.Blue)));

    private static int Power(Cubes cubes) => cubes.Red * cubes.Green * cubes.Blue;
}

public partial record Game(int Id, IReadOnlyList<Cubes> Draws)
{
    public static Game Parse(string input)
    {
        var regex = GameRegex();
        var match = regex.Match(input);
        var id = int.Parse(match.Groups["id"].Value);
        var draws = match.Groups["draws"].Value.Split(";").Select(Cubes.Parse).ToImmutableArray();
        return new Game(id, draws);
    }

  [GeneratedRegex(@"^Game (?<id>\d+): (?<draws>.*)$")]
  private static partial Regex GameRegex();
}

public partial record Cubes(int Red, int Green, int Blue)
{
    public static Cubes Parse(string input)
    {
        int red = 0, green = 0, blue = 0;
        var regex = CubeRegex();
        var colourInputs = input.Split(",").Select(x => x.Trim()).ToArray();
        foreach (var colourInput in colourInputs)
        {
            var match = regex.Match(colourInput);
            var count = int.Parse(match.Groups["count"].Value);
            var colour = match.Groups["colour"].Value;
            switch (colour)
            {
                case "red":
                    red = count;
                    break;
                case "green":
                    green = count;
                    break;
                case "blue":
                    blue = count;
                    break;
            }
        }

        return new Cubes(red, green, blue);
    }

  [GeneratedRegex(@"^(?<count>\d+) (?<colour>red|green|blue)$")]
  private static partial Regex CubeRegex();
}

