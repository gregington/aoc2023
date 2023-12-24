using System.Collections.Immutable;
using System.CommandLine;
using System.Numerics;
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

    public static async Task Run(string input, int part)
    {
        var hailstones = Parse(input);
        var isRealInput = input.EndsWith("input.txt");
        var task = part switch
        {
            1 => Part1(hailstones, isRealInput),
            2 => Part2(hailstones),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(IReadOnlyList<Hailstone> hailstones, bool isRealInput)
    {
        var intersectionMin = isRealInput ? 200_000_000_000_000d : 7d;
        var intersectionMax = isRealInput ? 400_000_000_000_000d : 27d;

        var linePoints = hailstones.Select(TwoPointsOnXyPlane).ToArray();

        var count = 0;
        for (var i = 0; i < linePoints.Length; i++)
        {
            for (var j = i + 1; j < linePoints.Length; j++)
            {
                var lineCoefficents1 = LineCoefficients(linePoints[i].P1, linePoints[i].P2);
                var lineCoefficents2 = LineCoefficients(linePoints[j].P1, linePoints[j].P2);

                var intersection = Intersection(lineCoefficents1, lineCoefficents2);
                if (intersection == null)
                {
                    // slope values are the same, coincident if b values are the same.
                    if (lineCoefficents1.B == lineCoefficents2.B)
                    {
                        count++;
                    }

                    // Otherwise parallel, don't intersect
                    continue;
                }

                var timeOfCollision1 = TimeOfCollision2D(hailstones[i], intersection);
                var timeOfCollision2 = TimeOfCollision2D(hailstones[j], intersection);

                if (timeOfCollision1 < 0 || timeOfCollision2 < 0)
                {
                    continue;
                }

                if (intersection.X >= intersectionMin && intersection.X <= intersectionMax
                    && intersection.Y >= intersectionMin && intersection.Y <= intersectionMax)
                    {
                        count++;
                    }
            }
        }

        Console.WriteLine(count);

        return Task.CompletedTask;
    }

    private static Task Part2(IReadOnlyList<Hailstone> hailstones)
    {
        using var ctx = new Context(new Dictionary<string, string> { ["model"] = "true" });
        var solver = ctx.MkSolver();

        var x = ctx.MkIntConst("x");
        var y = ctx.MkIntConst("y");
        var z = ctx.MkIntConst("z");
        var vx = ctx.MkIntConst("vx");
        var vy = ctx.MkIntConst("vy");
        var vz = ctx.MkIntConst("vz");

        // Take the first three hailstones
        for (var i = 0; i < 3; i++)
        {
            var hailstone = hailstones[i];

            // var (hpx, hpy, hpz) = hailstone.Position;
            // var (hvx, hvy, hvz) = hailstone.Velocity;

            var t = ctx.MkIntConst($"t{i}");

            var hpx = ctx.MkInt(Convert.ToInt64(hailstone.Position.X));
            var hpy = ctx.MkInt(Convert.ToInt64(hailstone.Position.Y));
            var hpz = ctx.MkInt(Convert.ToInt64(hailstone.Position.Z));

            var hvx = ctx.MkInt(Convert.ToInt64(hailstone.Velocity.X));
            var hvy = ctx.MkInt(Convert.ToInt64(hailstone.Velocity.Y));
            var hvz = ctx.MkInt(Convert.ToInt64(hailstone.Velocity.Z));

            solver.Add(t >= 0);
            solver.Add(ctx.MkEq(ctx.MkAdd(x, ctx.MkMul(vx, t)), ctx.MkAdd(hpx, ctx.MkMul(hvx, t))));
            solver.Add(ctx.MkEq(ctx.MkAdd(y, ctx.MkMul(vy, t)), ctx.MkAdd(hpy, ctx.MkMul(hvy, t))));
            solver.Add(ctx.MkEq(ctx.MkAdd(z, ctx.MkMul(vz, t)), ctx.MkAdd(hpz, ctx.MkMul(hvz, t))));
        }


        if (solver.Check() != Status.SATISFIABLE)
        {
            throw new Exception("Not satisfiable");
        }
        var model = solver.Model;

        var ex = model.Double(model.Eval(x));
        var ey = model.Double(model.Eval(y));
        var ez = model.Double(model.Eval(z));

        Console.WriteLine($"{ex}, {ey}, {ez}");
        Console.WriteLine(ex + ey + ez);

        return Task.CompletedTask;
    }

    private static bool IsCoincident2D((Point2D P1, Point2D P2) line1, (Point2D P1, Point2D P2) line2)
    {
        var lineCoefficents1 = LineCoefficients(line1.P1, line1.P2);
        var lineCoefficents2 = LineCoefficients(line2.P1, line2.P2);

        return lineCoefficents1 == lineCoefficents2;
    }

    private static LineCoefficients LineCoefficients(Point2D p1, Point2D p2)
    {
        var m = (p2.Y - p1.Y) / (p2.X - p1.X);
        var b = p1.Y - (m * p1.X);

        return new LineCoefficients(m, b);
    }

    private static double TimeOfCollision2D(Hailstone hailstone, Point2D intersection)
    {
        // we just need one dimension, but the velocity in that dimension can't be 0
        // There are no 0 velocity components in input, so we ignore this case and use x
        
        var distance = intersection.X - hailstone.Position.X;
        var speed = hailstone.Velocity.X;

        return distance / speed;
    }

    private static Point2D? Intersection(LineCoefficients l1, LineCoefficients l2)
    {
        var (a, c) = l1;
        var (b, d) = l2;

        if (a == b)
        {
            // Coincident or paralllel
            return null;
        }

        var x = (d - c)/(a - b);
        var y = (a * ((d - c) / (a - b))) + c;
        return new Point2D(x, y);
    }

    private static (Point2D P1, Point2D P2) TwoPointsOnXyPlane(Hailstone hailstone)
    {
        // First point is just the position
        var p1 = new Point2D(hailstone.Position.X, hailstone.Position.Y);

        // Second point is 1 nanosecond later, just add the velocity vector
        var p2 = new Point2D (p1.X + hailstone.Velocity.X, p1.Y + hailstone.Velocity.Y);

        return (p1, p2);
    }

    private static ImmutableArray<Hailstone> Parse(string input)
    {
        var regex = LineRegex();
        return File.ReadLinesAsync(input)
            .Select(line =>
            {
                var g = regex.Match(line).Groups;
                return new Hailstone(
                    new Vector3D(Convert.ToInt64(g["posX"].Value), Convert.ToInt64(g["posY"].Value), Convert.ToInt64(g["posZ"].Value)),
                    new Vector3D(Convert.ToInt64(g["velX"].Value), Convert.ToInt64(g["velY"].Value), Convert.ToInt64(g["velZ"].Value))
                );
            })
            .ToEnumerable()
            .ToImmutableArray();
    }

    [GeneratedRegex(@"^(?<posX>-?\d+), +(?<posY>-?\d+), +(?<posZ>-?\d+) +@ +(?<velX>-?\d+), +(?<velY>-?\d+), +(?<velZ>-?\d+)$")]
    private static partial Regex LineRegex();
}

public record LineCoefficients(double M, double B);

public record LineEquation2D(double A, double B, double C);

public record Point2D(double X, double Y);

public record Vector3D(double X, double Y, double Z);

public record Hailstone(Vector3D Position, Vector3D Velocity);
