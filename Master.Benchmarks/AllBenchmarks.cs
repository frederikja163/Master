using BenchmarkDotNet.Attributes;
using Master.Benchmarks.BenchmarkDotnetConfig;

namespace Master.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public abstract class AllBenchmarks
{
    protected TimeSpan Timeout = TimeSpan.FromMinutes(2);
    
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
    
    [ParamsSource(nameof(GetData))] public required Data Data { get; set; }

    public IEnumerable<Data> GetData()
    {
        bool isRows = true;
        bool isColumns = false;
        int totalCount = 5;
        for (int i = 0; i < totalCount; i++)
        {
            int rows = isRows ? (int)Math.Pow(10, i) : 10;
            int columns = isColumns ? (int)Math.Pow(10, totalCount - i + 1) : 100;
            yield return new Data(1_000, rows, 1).PopulateRandomInts(columns / 10).PopulateRandomDoubles(columns / 5).PopulateRandomNatoAlphabetStrings(columns / 5).PopulateOrderedInts(columns / 10).PopulateRandomGuidStrings(columns / 5).PopulateRandomFloats(columns / 5);
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