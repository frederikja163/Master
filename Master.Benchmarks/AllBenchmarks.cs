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
        yield return new Data(10_000, 1_000).PopulateOrderedInts().PopulateRandomInts().PopulateRandomFloats().PopulateRandomNatoAlphabetStrings().PopulateRandomGuidStrings();
        yield return new Data(10_000, 100).PopulateOrderedInts().PopulateRandomInts().PopulateRandomFloats().PopulateRandomNatoAlphabetStrings().PopulateRandomGuidStrings();
        yield return new Data(10_000, 10).PopulateOrderedInts().PopulateRandomInts().PopulateRandomFloats().PopulateRandomNatoAlphabetStrings().PopulateRandomGuidStrings();
        yield return new Data(10_000, 1).PopulateOrderedInts().PopulateRandomInts().PopulateRandomFloats().PopulateRandomNatoAlphabetStrings().PopulateRandomGuidStrings();
        yield return new Data(10_000, 1_000).PopulateOrderedInts();
        yield return new Data(10_000, 100).PopulateOrderedInts();
        yield return new Data(10_000, 10).PopulateOrderedInts();
        yield return new Data(10_000, 1).PopulateOrderedInts();
        yield return new Data(10_000, 1_000).PopulateRandomInts();
        yield return new Data(10_000, 100).PopulateRandomInts();
        yield return new Data(10_000, 10).PopulateRandomInts();
        yield return new Data(10_000, 1).PopulateRandomInts();
        yield return new Data(10_000, 1_000).PopulateRandomFloats();
        yield return new Data(10_000, 100).PopulateRandomFloats();
        yield return new Data(10_000, 10).PopulateRandomFloats();
        yield return new Data(10_000, 1).PopulateRandomFloats();
        yield return new Data(10_000, 1_000).PopulateRandomGuidStrings();
        yield return new Data(10_000, 100).PopulateRandomGuidStrings();
        yield return new Data(10_000, 10).PopulateRandomGuidStrings();
        yield return new Data(10_000, 1).PopulateRandomGuidStrings();
        yield return new Data(10_000, 1_000).PopulateRandomNatoAlphabetStrings();
        yield return new Data(10_000, 100).PopulateRandomNatoAlphabetStrings();
        yield return new Data(10_000, 10).PopulateRandomNatoAlphabetStrings();
        yield return new Data(10_000, 1).PopulateRandomNatoAlphabetStrings();
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