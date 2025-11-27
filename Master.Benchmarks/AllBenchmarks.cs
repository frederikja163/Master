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
        yield return new Data(10_000, 1, 0.5f).PopulateOrderedInts();
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