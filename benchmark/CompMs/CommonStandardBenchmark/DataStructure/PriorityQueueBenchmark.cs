using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CompMs.Common.DataStructure;

namespace CommonStandardBenchmark.DataStructure;

[HtmlExporter]
[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class PriorityQueueBenchmark
{
    [Params(32, 256, 2048)]
    public int Size { get; set; }

    [Params(1, 8, 64)]
    public int Operations { get; set; }

    private int[] pushpopvalues = null!;
    private int[] poppushvalues = null!;
    private PriorityQueue<int> customQueue = null!;
    private System.Collections.Generic.PriorityQueue<int, int> builtInQueue = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        poppushvalues = new int[Size + Operations];
        pushpopvalues = new int[Operations];
        for (var i = 0; i < poppushvalues.Length; i++) {
            poppushvalues[i] = rng.Next();
        }
        for (var i = 0; i < pushpopvalues.Length; i++) {
            pushpopvalues[i] = rng.Next();
        }
    }

    [IterationSetup(Targets = [nameof(CustomPopPush), nameof(CustomPopAndPush), nameof(BuiltInDequeueEnqueue)])]
    public void PopPushSetup() {
        customQueue = new PriorityQueue<int>(CreateInitialValues(), Comparer<int>.Default);
        builtInQueue = new System.Collections.Generic.PriorityQueue<int, int>();
        for (var i = 0; i < Size; i++) {
            builtInQueue.Enqueue(poppushvalues[Operations + i], poppushvalues[Operations + i]);
        }
    }

    [IterationSetup(Targets = [nameof(CustomPushPop), nameof(CustomPushAndPop), nameof(BuiltInEnqueueDequeue)])]
    public void PushPopSetup() {
        customQueue = new PriorityQueue<int>(Comparer<int>.Default);
        builtInQueue = new System.Collections.Generic.PriorityQueue<int, int>();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("PopPush")]
    public int CustomPopAndPush()
    {
        var checksum = 0;
        for (var i = 0; i < Operations; i++) {
            checksum += customQueue.Pop();
            customQueue.Push(poppushvalues[i]);
        }

        return checksum + customQueue.Length;
    }

    [Benchmark]
    [BenchmarkCategory("PopPush")]
    public int CustomPopPush()
    {
        var checksum = 0;
        for (var i = 0; i < Operations; i++) {
            checksum += customQueue.PopPush(poppushvalues[i]);
        }

        return checksum + customQueue.Length;
    }

    [Benchmark]
    [BenchmarkCategory("PopPush")]
    public int BuiltInDequeueEnqueue()
    {
        var checksum = 0;
        for (var i = 0; i < Operations; i++) {
            checksum += builtInQueue.Dequeue();
            builtInQueue.Enqueue(poppushvalues[i], poppushvalues[i]);
        }

        return checksum + builtInQueue.Count;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("PushPop")]
    public int CustomPushAndPop()
    {
        var checksum = 0;
        for (var i = 0; i < Operations; i++) {
            customQueue.Push(pushpopvalues[i]);
            checksum += customQueue.Pop();
        }

        return checksum + customQueue.Length;
    }

    [Benchmark]
    [BenchmarkCategory("PushPop")]
    public int CustomPushPop()
    {
        var checksum = 0;
        for (var i = 0; i < Operations; i++) {
            checksum += customQueue.PushPop(pushpopvalues[i]);
        }

        return checksum + customQueue.Length;
    }

    [Benchmark]
    [BenchmarkCategory("PushPop")]
    public int BuiltInEnqueueDequeue()
    {
        var checksum = 0;
        for (var i = 0; i < Operations; i++) {
            builtInQueue.Enqueue(pushpopvalues[i], pushpopvalues[i]);
            checksum += builtInQueue.Dequeue();
        }

        return checksum + builtInQueue.Count;
    }

    private IReadOnlyList<int> CreateInitialValues()
    {
        var initial = new int[Size];
        Array.Copy(poppushvalues, Operations, initial, 0, Size);
        return initial;
    }
}