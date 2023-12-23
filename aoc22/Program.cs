using System.Collections.Frozen;
using System.CommandLine;
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
        var blocks = Parse(input);
        var task = part switch
        {
            1 => Part1(blocks),
            2 => Part2(blocks),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };

        await task;
    }

    private static Task Part1(List<Block> blocks)
    {
        var droppedBlocks = DropAll(blocks);
        Print(droppedBlocks);

        var disintegratableBlocks = FindDisintegratableBlocks(droppedBlocks);
        Console.WriteLine(disintegratableBlocks.Count);

        return Task.CompletedTask;
    }

    private static Task Part2(List<Block> blocks)
    {
        var droppedBlocks = DropAll(blocks).OrderBy(x => x.Id).ToList();
        var (blocksAbove, blocksBelow) = CreateAdjacencies(droppedBlocks);

        var results = new List<int>();

        foreach (var block in droppedBlocks)
        {
            var queue = new Queue<int>();
            queue.Enqueue(block.Id);

            var falling = new HashSet<int>();
            while (queue.TryDequeue(out var blockId))
            {
                falling.Add(blockId);

                var additionalFalling =
                    from blockT in blocksAbove[blockId]
                    where blocksBelow[blockT].IsSubsetOf(falling)
                    select blockT;

                foreach (var b in additionalFalling)
                {
                    queue.Enqueue(b);
                }
            }

            results.Add(falling.Count - 1); // exclude original block
        }

        Console.WriteLine(results.Sum());

        return Task.CompletedTask;
    }

    private static void Print(IEnumerable<Block> blocks)
    {
        foreach (var block in blocks)
        {
            Console.WriteLine(block);
        }
    }

    private static (Dictionary<int, HashSet<int>> BlocksAbove, Dictionary<int, HashSet<int>> BlocksBelow) CreateAdjacencies(List<Block> blocks)
    {
        blocks = blocks.OrderBy(b => b.MinZ).ToList();
        var blocksAbove = blocks.ToDictionary(b => b.Id, _ => new HashSet<int>());
        var blocksBelow = blocks.ToDictionary(b => b.Id, _ => new HashSet<int>());

        for (var i = 0; i < blocks.Count; i++)
        {
            for (var j = i + 1; j < blocks.Count; j++)
            {
                if (blocks[j].Drop().Voxels.Intersect(blocks[i].Voxels).Any())
                {
                    blocksBelow[blocks[j].Id].Add(blocks[i].Id);
                    blocksAbove[blocks[i].Id].Add(blocks[j].Id);
                }
            }
        }
        return (blocksAbove, blocksBelow);
    }

    private static HashSet<int>[] SupportMap(IEnumerable<Block> blocks, IReadOnlyDictionary<Voxel, Block> voxelToBlock)
    {
        var result = blocks.Select(_ => new HashSet<int>()).ToArray();
        foreach (var block in blocks)
        {
            var blockUp = block.Drop();
            result[block.Id] = blockUp.Voxels
                .Select(v => voxelToBlock.TryGetValue(v, out var block) ? block.Id as int? : null)
                .Where(x => x != null)
                .Select(x => (int) x!)
                .Where(x => x != block.Id)
                .ToHashSet();
        }

        return result;        
    }

    private static List<Block> FindDisintegratableBlocks(List<Block> blocks)
    {
        return blocks.Where(block =>
        {
            var disintegrated = blocks.Where(b => b.Id != block.Id).ToList();
            return FindDroppableBlock(disintegrated) == null;
        })
        .ToList();
    }

    private static List<Block> DropAll(List<Block> blocks)
    {
        var copy = new List<Block>(blocks);
        
        var blocksByZ = copy.OrderBy(block => block.MinZ).ToList();

        foreach (var block in blocksByZ)
        {
            var blockToDrop = block;
            while (TryDrop(blockToDrop!, copy, out var droppedBlock))
            {
                blockToDrop = droppedBlock;
                copy[block.Id] = droppedBlock!;
            }
        }
        return copy;
    }

    private static bool TryDrop(Block block, List<Block> blocks, out Block? droppedBlock)
    {
        if (block.MinZ == 1)
        {
            droppedBlock = null;
            return false;
        }

        var candidateDrop = block.Drop();
        var blocksToCheck = blocks
            .Where(b => b.Id != candidateDrop.Id)
            .Where(b => b.MaxZ >= candidateDrop.MinZ && b.MinZ <= candidateDrop.MaxZ);
        foreach (var b in blocksToCheck)
        {
            if (candidateDrop.Voxels.Intersect(b.Voxels).Any())
            {
                droppedBlock = null;
                return false;
            }
        }
        droppedBlock = candidateDrop;
        return true;
    }

    private static Block? FindDroppableBlock(List<Block> blocks)
    {
        return blocks
            .Where(block => block.MinZ >= 2)
            .Where(block => 
            {
                var droppedBlock = block.Drop();

                var blocksToCheck = blocks
                    .Where(b => b.Id != droppedBlock.Id)
                    .Where(b => b.MaxZ >= droppedBlock.MinZ && b.MinZ <= droppedBlock.MaxZ);
                foreach (var b in blocksToCheck)
                {
                    if (droppedBlock.Voxels.Intersect(b.Voxels).Any())
                    {
                        droppedBlock = null;
                        return false;
                    }
                }
                return true;
            })
            .FirstOrDefault();
    }

    private static List<Block> Parse(string filename)
    {
        var regex = LineRegex();
        return Counter().Zip(File.ReadLines(filename), (a, b) => (Id: a, Line: b))
            .Select(x =>
            {
                var g = regex.Match(x.Line).Groups;
                var a = new Voxel(Convert.ToInt32(g["ax"].Value), Convert.ToInt32(g["ay"].Value), Convert.ToInt32(g["az"].Value));
                var b = new Voxel(Convert.ToInt32(g["bx"].Value), Convert.ToInt32(g["by"].Value), Convert.ToInt32(g["bz"].Value));
                return new Block(x.Id, a, b);
            })
            .ToList();
    }

    private static IEnumerable<int> Counter()
    {
        var counter = 0;
        while (true)
        {
            yield return counter++;
        }
    }

    [GeneratedRegex(@"^(?<ax>\d+),(?<ay>\d+),(?<az>\d+)~(?<bx>\d+),(?<by>\d+),(?<bz>\d+)$")]
    private static partial Regex LineRegex();
}

public record Voxel(int X, int Y, int Z);

public sealed class Block
{
    public Block(int id, Voxel a, Voxel b)
    {
        (Id, A, B) = (id, a, b);
        MinZ = Math.Min(A.Z, B.Z);
        MaxZ = Math.Max(A.Z, B.Z);
        Voxels = CalculateVoxels(a, b);
    }

    public int Id { get; init; }
    public Voxel A { get; init; }
    public Voxel B { get; init; }

    public IReadOnlySet<Voxel> Voxels { get; init; }

    public Block Drop()
    {
        return new Block(Id, A with { Z = A.Z - 1 }, B with { Z = B.Z - 1 });
    }

    public int MinZ { get; init; }

    public int MaxZ { get; init; }

    public override string ToString()
    {
        return $"{Id,4}: ({A.X,1}, {A.Y,1}, {A.Z,3})~({B.X,1}, {B.Y,1}, {B.Z,3})";
    }

    private static HashSet<Voxel> CalculateVoxels(Voxel a, Voxel b)
    {
        var set = new HashSet<Voxel>();
        // Bricks can extend in X, Y or Z, but not 2 or 3
        if (a.X == b.X && a.Y == b.Y)
        {
            // Extends in z direction
            var minZ = Math.Min(a.Z, b.Z);
            var maxZ = Math.Max(a.Z, b.Z);

            for (var z = minZ; z <= maxZ; z++)
            {
                set.Add(new Voxel(a.X, a.Y, z));
            }
        }
        else if (a.X == b.X && a.Z == b.Z)
        {
            // Extends in Y direction
            var minY = Math.Min(a.Y, b.Y);
            var maxY = Math.Max(a.Y, b.Y);

            for (var y = minY; y <= maxY; y++)
            {
                set.Add(new Voxel(a.X, y, a.Z));
            }
        }
        else if (a.Y == b.Y && a.Z == b.Z)
        {
            // Extends in X direction
            var minX = Math.Min(a.X, b.X);
            var maxX = Math.Max(a.X, b.X);

            for (var x = minX; x <= maxX; x++)
            {
                set.Add(new Voxel(x, a.Y, a.Z));
            }
        }
        else
        {
            throw new Exception("Unexpected block direction");
        }

        return set;
    }
}