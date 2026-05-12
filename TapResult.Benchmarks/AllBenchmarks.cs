using BenchmarkDotNet.Attributes;
using TapResult.Benchmarks.BenchmarkDotnetConfig;
using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public abstract class AllBenchmarks
{
    protected TimeSpan Timeout = TimeSpan.FromMinutes(60);
    
    [IterationSetup]
    public void Setup()
    {
        if (File.Exists(Config.FilePath))
        {
            File.Delete(Config.FilePath);
        }

        if (Directory.Exists(Config.FilePath))
        {
            Directory.Delete(Config.FilePath, true);
        }
    }
    
    [ParamsSource(nameof(GetData))] public required ICustomData Data { get; set; }

    public static IEnumerable<ICustomData> GetData()
    {
        yield return new RawData(1_000, 1_000, 1).PopulateRandomNatoAlphabetStrings();
        yield return new RawData(1_000, 1_000, 1).PopulateRandomGuidStrings();
        yield return new RawData(1_000, 1_000, 1).PopulateOrderedInts();
        yield return new RawData(1_000, 1_000, 1).PopulateRandomInts();
        yield return new RawData(1_000, 1_000, 1).PopulateRandomDoubles();
        yield break;
        bool isRows = true;
        bool isColumns = false;
        bool isSparsity = false;
        int totalCount = 4;
        for (int i = 0; i < totalCount; i++)
        {
            int rows = isRows ? (int)Math.Pow(10, i) : 10;
            int columns = isColumns ? (int)Math.Pow(10, totalCount - i) : 100;
            float sparsity = isSparsity ? MathF.Max((float)i / totalCount, 0.1f) : 1f;
            yield return new RawData(1_000, rows, sparsity)
                .PopulateRandomNatoAlphabetStrings(columns / 5)
                .PopulateRandomInts(columns / 10)
                .PopulateRandomDoubles(columns / 5)
                .PopulateOrderedInts(columns / 10)
                .PopulateRandomGuidStrings(columns / 5)
                .PopulateRandomFloats(columns / 5)
                ;
        }
    }
    
    protected static void RunWithTimeout(Action action, TimeSpan timeout)
    {
        Task task = Task.Run(action);
        if (!task.Wait(timeout))
        {
            throw new TimeoutException();
        }
    }
}